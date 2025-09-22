namespace NHS.CohortManager.ParticipantManagementServices;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Common;
using DataServices.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using Model.Enums;

public class ManageServiceNowParticipantFunction
{
    private readonly ILogger<ManageServiceNowParticipantFunction> _logger;
    private readonly ManageServiceNowParticipantConfig _config;
    private readonly IHttpClientFunction _httpClientFunction;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IDataServiceClient<ParticipantManagement> _participantManagementClient;
    private readonly IQueueClient _queueClient;

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

            var success = await ProcessParticipantRecord(serviceNowParticipant, participantManagement);
            if (!success)
            {
                return;
            }

            var subscribeToNemsSuccess = await SubscribeParticipantToNEMS(serviceNowParticipant.NhsNumber);

            if (!subscribeToNemsSuccess)
            {
                _logger.LogError("Failed to subscribe participant to NEMS. Case Number: {CaseNumber}", serviceNowParticipant.ServiceNowCaseNumber);
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

    private async Task<bool> ProcessParticipantRecord(ServiceNowParticipant serviceNowParticipant, ParticipantManagement? participantManagement)
    {
        var success = false;
        string? failureDescription;

        if (participantManagement is null)
        {
            success = await AddNewParticipant(serviceNowParticipant);
            failureDescription = "Participant Management Data Service add request failed";
        }
        else if (participantManagement.BlockedFlag == 1)
        {
            failureDescription = "Participant data from ServiceNow is blocked";
        }
        else
        {
            success = await UpdateExistingParticipant(serviceNowParticipant, participantManagement);
            failureDescription = "Participant Management Data Service update request failed";
        }

        if (!success)
        {
            await HandleException(new Exception(failureDescription), serviceNowParticipant, ServiceNowMessageType.UnableToAddParticipant);
        }

        return success;
    }

    private async Task<bool> AddNewParticipant(ServiceNowParticipant serviceNowParticipant)
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
            IsHigherRisk = isVhrParticipant ? 1 : null
        };

        if (isVhrParticipant)
        {
            _logger.LogInformation("Participant set as High Risk");
        }

        return await _participantManagementClient.Add(participantToAdd);
    }

    private async Task<bool> UpdateExistingParticipant(ServiceNowParticipant serviceNowParticipant, ParticipantManagement participantManagement)
    {
        _logger.LogInformation("Existing participant management record found, updating record {ParticipantId}", participantManagement.ParticipantId);

        participantManagement.RecordType = Actions.Amended;
        participantManagement.EligibilityFlag = 1;
        participantManagement.ReferralFlag = 1;
        participantManagement.RecordUpdateDateTime = DateTime.UtcNow;

        HandleVhrFlagForExistingParticipant(serviceNowParticipant, participantManagement);

        return await _participantManagementClient.Update(participantManagement);
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
        return serviceNowParticipant.FirstName == pdsDemographic.FirstName &&
               serviceNowParticipant.FamilyName == pdsDemographic.FamilyName &&
               serviceNowParticipant.DateOfBirth.ToString("yyyy-MM-dd") == pdsDemographic.DateOfBirth;
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
