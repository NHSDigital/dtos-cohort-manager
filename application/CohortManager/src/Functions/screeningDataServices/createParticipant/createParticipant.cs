namespace NHS.CohortManager.ScreeningDataServices;

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Data.Database;
using Common;
using Model;

public class CreateParticipant
{
    private readonly ILogger<CreateParticipant> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ICreateParticipantData _createParticipantData;
    private readonly IExceptionHandler _handleException;

    public CreateParticipant(ILogger<CreateParticipant> logger, ICreateResponse createResponse, ICreateParticipantData createParticipantData, IExceptionHandler handleException)
    {
        _logger = logger;
        _createResponse = createResponse;
        _createParticipantData = createParticipantData;
        _handleException = handleException;
    }

    [Function("CreateParticipant")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        _logger.LogInformation("CreateParticipant is called...");
        ParticipantCsvRecord participantCsvRecord = null;
        try
        {


            using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                var requestBody = await reader.ReadToEndAsync();
                participantCsvRecord = JsonSerializer.Deserialize<ParticipantCsvRecord>(requestBody);
            }

            var participantCreated = await _createParticipantData.CreateParticipantEntry(participantCsvRecord);
            if (participantCreated)
            {
                _logger.LogInformation("Successfully created the participant");
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
            }
            _logger.LogError("Failed to create the participant");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to make the CreateParticipant request\nMessage: {Message}", ex.Message);
            await _handleException.CreateSystemExceptionLog(ex, participantCsvRecord.Participant);
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }
}
