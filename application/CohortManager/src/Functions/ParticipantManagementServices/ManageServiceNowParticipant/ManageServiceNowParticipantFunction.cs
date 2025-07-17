namespace NHS.CohortManager.ParticipantManagementServices;

using System.Net;
using System.Text.Json;
using Common;
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
    private readonly IExceptionHandler _handleException;

    public ManageServiceNowParticipantFunction(ILogger<ManageServiceNowParticipantFunction> logger, IOptions<ManageServiceNowParticipantConfig> addParticipantConfig,
        IHttpClientFunction httpClientFunction, IExceptionHandler handleException)
    {
        _logger = logger;
        _config = addParticipantConfig.Value;
        _httpClientFunction = httpClientFunction;
        _handleException = handleException;
    }

    /// <summary>
    /// Reads messages from the Manage ServiceNow Participant Service Bus topic, checks PDS, adds/updates the record in participant management,
    /// and sends the record to cohort distribution
    /// </summary>
    /// <param name="participant">The participant from ServiceNow</param>
    [Function(nameof(ManageServiceNowParticipantFunction))]
    public async Task Run([ServiceBusTrigger(topicName: "%ServiceNowParticipantManagementTopic%", subscriptionName: "%ManageServiceNowParticipantSubscription%", Connection = "ServiceBusConnectionString_internal")] ServiceNowParticipant participant)
    {
        try
        {
            var pdsResponse = await _httpClientFunction.SendGetResponse($"{_config.RetrievePdsDemographicURL}?nhsNumber={participant.NhsNumber}");

            if (pdsResponse.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogError("NHS Number not found in PDS, unable to verify participant");
                await SendSeviceNowMessage(participant.ServiceNowRecordNumber, ServiceNowMessageType.UnableToVerifyParticipant);
                return;
            }

            if (pdsResponse.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError("Request to PDS returned an unexpected response. Status code: {StatusCode}", pdsResponse.StatusCode);
                await _handleException.CreateSystemExceptionLog(new Exception($"Request to PDS returned an unexpected response. Status code: {pdsResponse.StatusCode}"), participant);
                await SendSeviceNowMessage(participant.ServiceNowRecordNumber, ServiceNowMessageType.AddRequestInProgress);
                return;
            }

            var jsonString = await pdsResponse.Content.ReadAsStringAsync();
            var participantDemographic = JsonSerializer.Deserialize<ParticipantDemographic>(jsonString);

            if (participantDemographic == null)
            {
                _logger.LogError("Deserialisation of PDS response to {Type} returned null", typeof(ParticipantDemographic));
                await _handleException.CreateSystemExceptionLog(new Exception($"Deserialisation of PDS response to {typeof(ParticipantDemographic)} returned null"), participant);
                await SendSeviceNowMessage(participant.ServiceNowRecordNumber, ServiceNowMessageType.AddRequestInProgress);
                return;
            }

            if (participantDemographic.NhsNumber.ToString() != participant.NhsNumber)
            {
                _logger.LogError("NHS Numbers don't match, NHS Number must have been superseded");
                await SendSeviceNowMessage(participant.ServiceNowRecordNumber, ServiceNowMessageType.UnableToVerifyParticipant);
                return;
            }

            // TODO: DTOSS-8375
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected exception occured");
            await _handleException.CreateSystemExceptionLog(ex, participant);
            await SendSeviceNowMessage(participant.ServiceNowRecordNumber, ServiceNowMessageType.AddRequestInProgress);
        }
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
