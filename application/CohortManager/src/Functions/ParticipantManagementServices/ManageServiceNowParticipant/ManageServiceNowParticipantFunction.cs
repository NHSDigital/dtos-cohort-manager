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

    public ManageServiceNowParticipantFunction(ILogger<ManageServiceNowParticipantFunction> logger, IOptions<ManageServiceNowParticipantConfig> addParticipantConfig,
        IHttpClientFunction httpClientFunction, IExceptionHandler handleException, IDataServiceClient<ParticipantManagement> participantManagementClient)
    {
        _logger = logger;
        _config = addParticipantConfig.Value;
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
            var pdsResponse = await _httpClientFunction.SendGetResponse($"{_config.RetrievePdsDemographicURL}?nhsNumber={serviceNowParticipant.NhsNumber}");

            if (pdsResponse.StatusCode == HttpStatusCode.NotFound)
            {
                await HandleException(new Exception("Request to PDS for ServiceNow Participant returned a 404 NotFound response."), serviceNowParticipant, ServiceNowMessageType.UnableToAddParticipant);
                return;
            }

            if (pdsResponse.StatusCode != HttpStatusCode.OK)
            {
                await HandleException(new Exception($"Request to PDS for ServiceNow Participant returned an unexpected response. Status code: {pdsResponse.StatusCode}"), serviceNowParticipant, ServiceNowMessageType.AddRequestInProgress);
                return;
            }

            var jsonString = await pdsResponse.Content.ReadAsStringAsync();
            var participantDemographic = JsonSerializer.Deserialize<ParticipantDemographic>(jsonString);

            if (participantDemographic == null)
            {
                await HandleException(new Exception($"Deserialisation of PDS for ServiceNow Participant response to {typeof(ParticipantDemographic)} returned null"), serviceNowParticipant, ServiceNowMessageType.AddRequestInProgress);
                return;
            }

            if (participantDemographic.NhsNumber != serviceNowParticipant.NhsNumber)
            {
                await HandleException(new Exception("NHS Numbers don't match for ServiceNow Participant and PDS, NHS Number must have been superseded"), serviceNowParticipant, ServiceNowMessageType.UnableToAddParticipant);
                return;
            }

            var dataMatch = CheckParticipantDataMatches(serviceNowParticipant, participantDemographic);

            if (!dataMatch)
            {
                await HandleException(new Exception("Participant data from ServiceNow does not match participant data from PDS"), serviceNowParticipant, ServiceNowMessageType.UnableToAddParticipant);
                return;
            }

            var participantManagement = await _participantManagementClient.GetSingleByFilter(
                x => x.NHSNumber == serviceNowParticipant.NhsNumber && x.ScreeningId == serviceNowParticipant.ScreeningId);

            bool dataServiceResponse;
            if (participantManagement is null)
            {
                _logger.LogInformation("Participant not in participant management table, adding new record");

                var participantToAdd = new ParticipantManagement
                {
                    ScreeningId = serviceNowParticipant.ScreeningId,
                    NHSNumber = serviceNowParticipant.NhsNumber,
                    RecordType = Actions.New,
                    EligibilityFlag = 1,
                    ReferralFlag = 1,
                    RecordInsertDateTime = DateTime.UtcNow
                };

                dataServiceResponse = await _participantManagementClient.Add(participantToAdd);
            }
            else if (participantManagement.BlockedFlag == 1)
            {
                await HandleException(new Exception("Participant data from ServiceNow is blocked"), serviceNowParticipant, ServiceNowMessageType.UnableToAddParticipant);
                return;
            }
            else
            {
                _logger.LogInformation("Existing participant managment record found, updating record {ParticipantId}", participantManagement.ParticipantId);
                participantManagement.RecordType = Actions.Amended;
                participantManagement.EligibilityFlag = 1;
                participantManagement.ReferralFlag = 1;
                participantManagement.RecordUpdateDateTime = DateTime.UtcNow;

                dataServiceResponse = await _participantManagementClient.Update(participantManagement);
            }

            if (!dataServiceResponse)
            {
                await HandleException(new Exception("Participant Management Data Service request failed"), serviceNowParticipant, ServiceNowMessageType.AddRequestInProgress);
                return;
            }

        }
        catch (Exception ex)
        {
            await HandleException(ex, serviceNowParticipant, ServiceNowMessageType.AddRequestInProgress);
        }
    }

    private async Task HandleException(Exception exception, ServiceNowParticipant serviceNowParticipant, ServiceNowMessageType serviceNowMessageType)
    {
        _logger.LogError(exception, "Exception occured whilst attempting to add participant from ServiceNow");
        await _exceptionHandler.CreateSystemExceptionLog(exception, serviceNowParticipant);
        await SendSeviceNowMessage(serviceNowParticipant.ServiceNowRecordNumber, serviceNowMessageType);
    }

    private static bool CheckParticipantDataMatches(ServiceNowParticipant serviceNowParticipant, ParticipantDemographic participantDemographic)
    {
        if (serviceNowParticipant.FirstName == participantDemographic.GivenName &&
            serviceNowParticipant.FamilyName == participantDemographic.FamilyName &&
            serviceNowParticipant.DateOfBirth.ToString("yyyy-MM-dd") == participantDemographic.DateOfBirth)
        {
            return true;
        }

        return false;
    }

    private async Task SendSeviceNowMessage(string serviceNowRecordNumber, ServiceNowMessageType servicenowMessageType)
    {
        var url = $"{_config.SendServiceNowMessageURL}/{serviceNowRecordNumber}";
        var requestBody = new SendServiceNowMessageRequestBody
        {
            MessageType = servicenowMessageType
        };
        var json = JsonSerializer.Serialize(requestBody);

        _logger.LogInformation("Sending ServiceNow message type {MessageType}", servicenowMessageType);

        await _httpClientFunction.SendPut(url, json);
    }
}
