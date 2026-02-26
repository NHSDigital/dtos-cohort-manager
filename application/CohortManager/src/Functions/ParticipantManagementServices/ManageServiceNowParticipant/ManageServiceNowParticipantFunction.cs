namespace NHS.CohortManager.ParticipantManagementServices;

using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Common;
using DataServices.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using Model.Constants;
using Model.Enums;

public class ManageServiceNowParticipantFunction
{
    private readonly ILogger<ManageServiceNowParticipantFunction> _logger;
    private readonly ManageServiceNowParticipantConfig _config;
    private readonly IHttpClientFunction _httpClientFunction;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IDataServiceClient<ParticipantManagement> _participantManagementClient;
    private readonly IQueueClient _queueClient;

    private static readonly Regex NonLetterRegex = new(@"[^\p{Lu}\p{Ll}\p{Lt}]", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    public ManageServiceNowParticipantFunction(ILogger<ManageServiceNowParticipantFunction> logger, IOptions<ManageServiceNowParticipantConfig> config,
        IHttpClientFunction httpClientFunction, IExceptionHandler handleException, IDataServiceClient<ParticipantManagement> participantManagementClient,
        IQueueClient queueClient)
    {
        _logger = logger;
        _config = config.Value;
        _httpClientFunction = httpClientFunction;
        _exceptionHandler = handleException;
        _participantManagementClient = participantManagementClient;
        _queueClient = queueClient;
    }

    /// <summary>
    /// Reads messages from the Manage ServiceNow Participant Service Bus topic, checks PDS, adds/updates the record in participant management,
    /// and sends the record to cohort distribution
    /// </summary>
    /// <param name="serviceNowParticipant">The participant from ServiceNow</param>
    [Function(nameof(ManageServiceNowParticipantFunction))]
    public async Task Run([ServiceBusTrigger(topicName: "%ServiceNowParticipantManagementTopic%", subscriptionName: "%ManageServiceNowParticipantSubscription%", Connection = "ServiceBusConnectionString_internal")] ServiceNowParticipant serviceNowParticipant)
    {
        try
        {
            var pdsDemographic = await ValidateAndRetrieveParticipantFromPds(serviceNowParticipant);
            if (pdsDemographic is null)
            {
                return;
            }

            var participantManagement = await _participantManagementClient.GetSingleByFilter(
                x => x.NHSNumber == serviceNowParticipant.NhsNumber && x.ScreeningId == serviceNowParticipant.ScreeningId);

            var success = await ProcessParticipantRecord(serviceNowParticipant, participantManagement, pdsDemographic);
            if (!success)
            {
                return;
            }

            var subscribeToNemsSuccess = await SubscribeParticipantToNEMS(serviceNowParticipant.NhsNumber);

            if (!subscribeToNemsSuccess)
            {
                _logger.LogError("Failed to subscribe participant for updates. Case Number: {CaseNumber}", serviceNowParticipant.ServiceNowCaseNumber);
            }

            if(!string.IsNullOrEmpty(serviceNowParticipant.RequiredGpCode))
            {
                await _exceptionHandler.CreateTransformExecutedExceptions(new CohortDistributionParticipant{NhsNumber = serviceNowParticipant.NhsNumber.ToString()},"98.UpdateServiceNowData.ReferralWithPrimaryCareProvider",98);
            }

            var participantForDistribution = new BasicParticipantCsvRecord(serviceNowParticipant, participantManagement);

            var sendToQueueSuccess = await _queueClient.AddAsync(participantForDistribution, _config.CohortDistributionTopic);

            if (!sendToQueueSuccess)
            {
                await HandleException(new Exception($"Failed to send participant from ServiceNow to topic: {_config.CohortDistributionTopic}"), serviceNowParticipant, ServiceNowMessageType.AddRequestInProgress);
            }
        }
        catch (Exception ex)
        {
            await HandleException(ex, serviceNowParticipant, ServiceNowMessageType.AddRequestInProgress);
        }
    }

    private async Task<PdsDemographic?> ValidateAndRetrieveParticipantFromPds(ServiceNowParticipant serviceNowParticipant)
    {
        var pdsResponse = await _httpClientFunction.SendGetResponse($"{_config.RetrievePdsDemographicURL}?nhsNumber={serviceNowParticipant.NhsNumber}");

        if (pdsResponse.StatusCode == HttpStatusCode.NotFound)
        {
            await HandleException(new Exception("Request to PDS for ServiceNow Participant returned a NotFound response."), serviceNowParticipant, ServiceNowMessageType.UnableToAddParticipant);
            return null;
        }

        if (pdsResponse.StatusCode != HttpStatusCode.OK)
        {
            await HandleException(new Exception($"Request to PDS for ServiceNow Participant returned an unexpected response. Status code: {pdsResponse.StatusCode}"), serviceNowParticipant, ServiceNowMessageType.AddRequestInProgress);
            return null;
        }

        var pdsDemographic = await DeserializePdsDemographic(pdsResponse, serviceNowParticipant);
        if (pdsDemographic is null) return null;

        return await ValidateParticipantData(serviceNowParticipant, pdsDemographic)
            ? pdsDemographic
            : null;
    }

    private async Task<PdsDemographic?> DeserializePdsDemographic(HttpResponseMessage pdsResponse, ServiceNowParticipant serviceNowParticipant)
    {
        var pdsDemographic = await pdsResponse.Content.ReadFromJsonAsync<PdsDemographic>();

        if (pdsDemographic is null)
        {
            await HandleException(new Exception($"Deserialisation of PDS for ServiceNow Participant response to {typeof(PdsDemographic)} returned null"), serviceNowParticipant, ServiceNowMessageType.AddRequestInProgress);
            return null;
        }

        return pdsDemographic;
    }

    private async Task<bool> ValidateParticipantData(ServiceNowParticipant serviceNowParticipant, PdsDemographic pdsDemographic)
    {
        if (pdsDemographic.NhsNumber != serviceNowParticipant.NhsNumber.ToString())
        {
            await HandleException(new Exception("NHS Numbers don't match for ServiceNow Participant and PDS, NHS Number must have been superseded"), serviceNowParticipant, ServiceNowMessageType.UnableToAddParticipant);
            return false;
        }

        if (!CheckParticipantDataMatches(serviceNowParticipant, pdsDemographic))
        {
            await HandleException(new Exception("Participant data from ServiceNow does not match participant data from PDS"), serviceNowParticipant, ServiceNowMessageType.UnableToAddParticipant);
            return false;
        }

        return true;
    }

    private async Task<bool> ProcessParticipantRecord(ServiceNowParticipant serviceNowParticipant, ParticipantManagement? participantManagement, PdsDemographic pdsDemographic)
    {
        var success = false;
        string? failureDescription;

        if (participantManagement is null)
        {
            success = await AddNewParticipant(serviceNowParticipant, pdsDemographic);
            failureDescription = "Participant Management Data Service add request failed";
        }
        else if (participantManagement.BlockedFlag == 1)
        {
            failureDescription = "Participant data from ServiceNow is blocked";
        }
        else
        {
            success = await UpdateExistingParticipant(serviceNowParticipant, participantManagement, pdsDemographic);
            failureDescription = "Participant Management Data Service update request failed";
        }

        if (!success)
        {
            await HandleException(new Exception(failureDescription), serviceNowParticipant, ServiceNowMessageType.UnableToAddParticipant);
        }

        return success;
    }

    private async Task<bool> AddNewParticipant(ServiceNowParticipant serviceNowParticipant, PdsDemographic pdsDemographic)
    {
        _logger.LogInformation("Participant not in participant management table, adding new record");

        var isVhrParticipant = CheckIfVhrParticipant(serviceNowParticipant);

        var participantToAdd = new ParticipantManagement
        {
            ScreeningId = serviceNowParticipant.ScreeningId,
            NHSNumber = serviceNowParticipant.NhsNumber,
            RecordType = Actions.New,
            EligibilityFlag = 1,
            ReferralFlag = 1,
            RecordInsertDateTime = DateTime.UtcNow,
            IsHigherRisk = isVhrParticipant ? 1 : null,
            ReasonForRemoval = pdsDemographic.ReasonForRemoval,
            ReasonForRemovalDate = ParseRemovalEffectiveFromDateStringToDateTime(pdsDemographic.RemovalEffectiveFromDate)
        };

        if (isVhrParticipant)
        {
            _logger.LogInformation("Participant set as High Risk");
        }

        return await _participantManagementClient.Add(participantToAdd);
    }

    private async Task<bool> UpdateExistingParticipant(ServiceNowParticipant serviceNowParticipant, ParticipantManagement participantManagement, PdsDemographic pdsDemographic)
    {
        _logger.LogInformation("Existing participant management record found, updating record {ParticipantId}", participantManagement.ParticipantId);

        participantManagement.RecordType = Actions.New;
        participantManagement.EligibilityFlag = 1;
        participantManagement.ReferralFlag = 1;
        participantManagement.ExceptionFlag = 0;
        participantManagement.RecordUpdateDateTime = DateTime.UtcNow;
        participantManagement.ReasonForRemoval = pdsDemographic.ReasonForRemoval;
        participantManagement.ReasonForRemovalDate = ParseRemovalEffectiveFromDateStringToDateTime(pdsDemographic.RemovalEffectiveFromDate);

        HandleVhrFlagForExistingParticipant(serviceNowParticipant, participantManagement);

        return await _participantManagementClient.Update(participantManagement);
    }

    private static DateTime? ParseRemovalEffectiveFromDateStringToDateTime(string? removalEffectiveFromDate)
    {
        if (removalEffectiveFromDate == null)
        {
            return null;
        }

        return DateTime.ParseExact(removalEffectiveFromDate, "yyyy'-'MM'-'dd'T'HH':'mm':'ssK", CultureInfo.InvariantCulture);
    }

    private void HandleVhrFlagForExistingParticipant(ServiceNowParticipant serviceNowParticipant, ParticipantManagement participantManagement)
    {
        var isVhrParticipant = CheckIfVhrParticipant(serviceNowParticipant);

        if (!participantManagement.IsHigherRisk.HasValue && isVhrParticipant)
        {
            participantManagement.IsHigherRisk = 1;
            _logger.LogInformation("Participant {ParticipantId} set as High Risk based on ServiceNow attributes", participantManagement.ParticipantId);
        }

        if (participantManagement.IsHigherRisk == 1)
        {
            _logger.LogInformation("Participant {ParticipantId} still maintained as High Risk", participantManagement.ParticipantId);
        }
    }

    private async Task HandleException(Exception exception, ServiceNowParticipant serviceNowParticipant, ServiceNowMessageType serviceNowMessageType)
    {
        _logger.LogError(exception, "Exception occurred whilst attempting to add participant from ServiceNow");
        await _exceptionHandler.CreateSystemExceptionLog(exception, serviceNowParticipant);
        await SendServiceNowMessage(serviceNowParticipant.ServiceNowCaseNumber, serviceNowMessageType);
    }

    private static bool CheckParticipantDataMatches(ServiceNowParticipant serviceNowParticipant, PdsDemographic pdsDemographic)
    {
        return NormalizedNamesMatch(serviceNowParticipant.FirstName, pdsDemographic.FirstName) &&
               NormalizedNamesMatch(serviceNowParticipant.FamilyName, pdsDemographic.FamilyName) &&
               serviceNowParticipant.DateOfBirth.ToString("yyyy-MM-dd") == pdsDemographic.DateOfBirth;
    }

    /// <summary>
    /// Normalizes and compares two name strings by removing accents, spaces, hyphens, and special characters.
    /// Converts accented characters to their base forms (É→E, Ñ→N, Ö→O) to match database storage behavior.
    /// </summary>
    /// <param name="name1">First name to compare</param>
    /// <param name="name2">Second name to compare</param>
    /// <returns>True if the normalized names match (case-insensitive), false otherwise</returns>
    private static bool NormalizedNamesMatch(string? name1, string? name2)
    {
        if (string.IsNullOrWhiteSpace(name1) && string.IsNullOrWhiteSpace(name2)) return true;
        if (string.IsNullOrWhiteSpace(name1) || string.IsNullOrWhiteSpace(name2)) return false;

        var normalized1 = NormalizeName(name1);
        var normalized2 = NormalizeName(name2);

        if (string.IsNullOrEmpty(normalized1) || string.IsNullOrEmpty(normalized2)) return false;

        return string.Equals(normalized1, normalized2, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Normalizes a name by removing accents and all non-letter characters.
    /// This handles spaces, hyphens, apostrophes, and other punctuation.
    /// Accented characters like É, Ñ, Ö are converted to their base forms (E, N, O).
    /// Uses Unicode NFD normalization to decompose accents, then removes diacritical marks.
    /// </summary>
    /// <param name="name">The name to normalize</param>
    /// <returns>Normalized name containing only unaccented ASCII letters</returns>
    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var trimmedName = name.Trim();
        var normalizedString = trimmedName.Normalize(NormalizationForm.FormD);
        var lettersOnlyString = NonLetterRegex.Replace(normalizedString, string.Empty);

        return lettersOnlyString.Normalize(NormalizationForm.FormC);
    }

    private async Task SendServiceNowMessage(string serviceNowCaseNumber, ServiceNowMessageType servicenowMessageType)
    {
        var url = $"{_config.SendServiceNowMessageURL}/{serviceNowCaseNumber}";
        var requestBody = new SendServiceNowMessageRequestBody
        {
            MessageType = servicenowMessageType
        };
        var json = JsonSerializer.Serialize(requestBody);

        _logger.LogInformation("Sending ServiceNow message type {MessageType}", servicenowMessageType);

        await _httpClientFunction.SendPut(url, json);
    }

    private static bool CheckIfVhrParticipant(ServiceNowParticipant serviceNowParticipant)
    {
        return serviceNowParticipant.ReasonForAdding == ServiceNowReasonsForAdding.VeryHighRisk;
    }

    private async Task<bool> SubscribeParticipantToNEMS(long nhsNumber)
    {
        var queryParams = new Dictionary<string, string>
        {
            {"nhsNumber", nhsNumber.ToString()}
        };

        var nemsSubscribeResponse = await _httpClientFunction.SendPost(_config.ManageNemsSubscriptionSubscribeURL, queryParams);

        return nemsSubscribeResponse.IsSuccessStatusCode;
    }
}
