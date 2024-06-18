namespace screeningDataServices;

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

    public CreateParticipant(ILogger<CreateParticipant> logger, ICreateResponse createResponse, ICreateParticipantData createParticipantData)
    {
        _logger = logger;
        _createResponse = createResponse;
        _createParticipantData = createParticipantData;
    }

    [Function("CreateParticipant")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        _logger.LogInformation("CreateParticipant is called...");

        try
        {
            string requestBody = "";
            ParticipantCsvRecord participantCsvRecord;

            using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBody = await reader.ReadToEndAsync();
                participantCsvRecord = JsonSerializer.Deserialize<ParticipantCsvRecord>(requestBody);
            }

            var participantCreated = _createParticipantData.CreateParticipantEntry(participantCsvRecord);
            if (participantCreated)
            {
                _logger.LogInformation("Successfully created the participant(s)");
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
            }
            _logger.LogError("Failed to create the participant(s)");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, "Failed to make the CreateParticipant request");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }
}
