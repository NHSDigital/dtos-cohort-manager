namespace NHS.CohortManager.ParticipantManagementServices;

using System.Net;
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

    public ManageServiceNowParticipantFunction(ILogger<ManageServiceNowParticipantFunction> logger, IOptions<ManageServiceNowParticipantConfig> config,
        IHttpClientFunction httpClientFunction, IExceptionHandler handleException, IDataServiceClient<ParticipantManagement> participantManagementClient)
    {
        _logger = logger;
        _config = config.Value;
        _httpClientFunction = httpClientFunction;
        _exceptionHandler = handleException;
        _participantManagementClient = participantManagementClient;
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
            var participantDemographic = await ValidateAndRetrieveParticipantFromPds(serviceNowParticipant);
            if (participantDemographic is null) return;

            var participantManagement = await _participantManagementClient.GetSingleByFilter(
                x => x.NHSNumber == serviceNowParticipant.NhsNumber && x.ScreeningId == serviceNowParticipant.ScreeningId);

            var success = await ProcessParticipantRecord(serviceNowParticipant, participantManagement);
            if (!success)
            {
                await HandleException(new Exception("Participant Management Data Service request failed"), serviceNowParticipant, ServiceNowMessageType.AddRequestInProgress);
            }
        }
        catch (Exception ex)
        {
            await HandleException(ex, serviceNowParticipant, ServiceNowMessageType.AddRequestInProgress);
        }
    }

    private async Task<ParticipantDemographic?> ValidateAndRetrieveParticipantFromPds(ServiceNowParticipant serviceNowParticipant)
    {
        var pdsResponse = await _httpClientFunction.SendGetResponse($"{_config.RetrievePdsDemographicURL}?nhsNumber={serviceNowParticipant.NhsNumber}");

        if (pdsResponse.StatusCode == HttpStatusCode.NotFound)
        {
            await HandleException(new Exception("Request to PDS for ServiceNow Participant returned a 404 NotFound response."), serviceNowParticipant, ServiceNowMessageType.UnableToAddParticipant);
            return null;
        }

        if (pdsResponse.StatusCode != HttpStatusCode.OK)
        {
            await HandleException(new Exception($"Request to PDS for ServiceNow Participant returned an unexpected response. Status code: {pdsResponse.StatusCode}"), serviceNowParticipant, ServiceNowMessageType.AddRequestInProgress);
            return null;
        }

        var participantDemographic = await DeserializeParticipantDemographic(pdsResponse, serviceNowParticipant);
        if (participantDemographic is null) return null;

        return await ValidateParticipantData(serviceNowParticipant, participantDemographic)
            ? participantDemographic
            : null;
    }

    private async Task<ParticipantDemographic?> DeserializeParticipantDemographic(HttpResponseMessage pdsResponse, ServiceNowParticipant serviceNowParticipant)
    {
        var jsonString = await pdsResponse.Content.ReadAsStringAsync();
        var participantDemographic = JsonSerializer.Deserialize<ParticipantDemographic>(jsonString);

        if (participantDemographic is null)
        {
            await HandleException(new Exception($"Deserialisation of PDS for ServiceNow Participant response to {typeof(ParticipantDemographic)} returned null"), serviceNowParticipant, ServiceNowMessageType.AddRequestInProgress);
            return null;
        }

        return participantDemographic;
    }

    private async Task<bool> ValidateParticipantData(ServiceNowParticipant serviceNowParticipant, ParticipantDemographic participantDemographic)
    {
        if (participantDemographic.NhsNumber != serviceNowParticipant.NhsNumber)
        {
            await HandleException(new Exception("NHS Numbers don't match for ServiceNow Participant and PDS, NHS Number must have been superseded"), serviceNowParticipant, ServiceNowMessageType.UnableToAddParticipant);
            return false;
        }

        if (!CheckParticipantDataMatches(serviceNowParticipant, participantDemographic))
        {
            await HandleException(new Exception("Participant data from ServiceNow does not match participant data from PDS"), serviceNowParticipant, ServiceNowMessageType.UnableToAddParticipant);
            return false;
        }

        return true;
    }

    private async Task<bool> ProcessParticipantRecord(ServiceNowParticipant serviceNowParticipant, ParticipantManagement? participantManagement)
    {
        if (participantManagement is null)
        {
            return await AddNewParticipant(serviceNowParticipant);
        }

        if (participantManagement.BlockedFlag == 1)
        {
            await HandleException(new Exception("Participant data from ServiceNow is blocked"), serviceNowParticipant, ServiceNowMessageType.UnableToAddParticipant);
            return true;
        }

        return await UpdateExistingParticipant(serviceNowParticipant, participantManagement);
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
            _logger.LogInformation("Participant with NHS Number: {NhsNumber} set as High Risk", serviceNowParticipant.NhsNumber);
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

    private static bool CheckParticipantDataMatches(ServiceNowParticipant serviceNowParticipant, ParticipantDemographic participantDemographic)
    {
        return serviceNowParticipant.FirstName == participantDemographic.GivenName &&
               serviceNowParticipant.FamilyName == participantDemographic.FamilyName &&
               serviceNowParticipant.DateOfBirth.ToString("yyyy-MM-dd") == participantDemographic.DateOfBirth;
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
}
