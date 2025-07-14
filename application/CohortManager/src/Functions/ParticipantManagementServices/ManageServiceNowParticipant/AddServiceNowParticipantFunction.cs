namespace NHS.CohortManager.ParticipantManagementServices;

using System.Net;
using System.Text.Json;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using Model.Enums;

public class AddServiceNowParticipantFunction
{
    private readonly ILogger<AddServiceNowParticipantFunction> _logger;
    private readonly ManageServiceNowParticipantConfig _config;
    private readonly IHttpClientFunction _httpClientFunction;
    private readonly IExceptionHandler _handleException;

    public AddServiceNowParticipantFunction(ILogger<AddServiceNowParticipantFunction> logger, IOptions<ManageServiceNowParticipantConfig> addParticipantConfig,
        IHttpClientFunction httpClientFunction, IExceptionHandler handleException)
    {
        _logger = logger;
        _config = addParticipantConfig.Value;
        _httpClientFunction = httpClientFunction;
        _handleException = handleException;
    }

    [Function(nameof(AddServiceNowParticipantFunction))]
    public async Task Run([QueueTrigger("%AddServiceNowParticipantQueueName%", Connection = "AzureWebJobsStorage")] string jsonFromQueue)
    {
        var participant = JsonSerializer.Deserialize<ServiceNowParticipant>(jsonFromQueue);

        if (participant == null)
        {
            _logger.LogError("Deserialisation of message response to {type} returned null", typeof(ServiceNowParticipant));
            return;
        }

        try
        {
            var pdsResponse = await _httpClientFunction.SendGetResponse($"{_config.RetrievePdsDemographicURL}?nhsNumber={participant.NhsNumber}");

            if (pdsResponse.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogError("NHS Number not found in PDS, unable to verify participant");
                await SendSeviceNowMessage(participant.ServiceNowRecordNumber, ServiceNowMessageType.MessageType1);
                return;
            }

            if (pdsResponse.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError("Request to PDS returned an unexpected response. Status code: {StatusCode}", pdsResponse.StatusCode);
                await _handleException.CreateSystemExceptionLog(new Exception($"Request to PDS returned an unexpected response. Status code: {pdsResponse.StatusCode}"), participant);
                await SendSeviceNowMessage(participant.ServiceNowRecordNumber, ServiceNowMessageType.MessageType2);
                return;
            }

            var jsonString = await pdsResponse.Content.ReadAsStringAsync();
            var pdsDemographic = JsonSerializer.Deserialize<PdsDemographic>(jsonString);

            if (pdsDemographic == null)
            {
                _logger.LogError("Deserialisation of PDS response to {type} returned null", typeof(PdsDemographic));
                await _handleException.CreateSystemExceptionLog(new Exception($"Deserialisation of PDS response to {typeof(PdsDemographic)} returned null"), participant);
                await SendSeviceNowMessage(participant.ServiceNowRecordNumber, ServiceNowMessageType.MessageType2);
                return;
            }

            if (pdsDemographic.NhsNumber != participant.NhsNumber)
            {
                _logger.LogError("NHS Numbers don't match, NHS Number must have been superseded");
                await SendSeviceNowMessage(participant.ServiceNowRecordNumber, ServiceNowMessageType.MessageType1);
                return;
            }

            // TODO: DTOSS-8375
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected exception occured");
            await _handleException.CreateSystemExceptionLog(ex, participant);
            await SendSeviceNowMessage(participant.ServiceNowRecordNumber, ServiceNowMessageType.MessageType2);
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
