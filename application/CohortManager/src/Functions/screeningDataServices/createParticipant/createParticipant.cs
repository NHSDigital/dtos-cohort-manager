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
using System.Runtime.CompilerServices;

public class CreateParticipant
{
    private readonly ILogger<CreateParticipant> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ICreateParticipantData _createParticipantData;
    private readonly string connectionString;
    private readonly ICheckDemographic _checkDemographic;

    public CreateParticipant(ILogger<CreateParticipant> logger, ICreateResponse createResponse, ICreateParticipantData createParticipantData, ICheckDemographic checkDemographic)
    {
        _logger = logger;
        _createResponse = createResponse;
        _createParticipantData = createParticipantData;
        _checkDemographic = checkDemographic;
        connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
        _logger.LogInformation("Connection String: " + connectionString);
    }

    [Function("CreateParticipant")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        _logger.LogInformation("CreateParticipant is called...");

        try
        {
            // parse through the HTTP request
            string requestBody = "";
            Participant participantData = new Participant();

            using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBody = await reader.ReadToEndAsync();
                participantData = JsonSerializer.Deserialize<Participant>(requestBody);
            }

            var participantCreated = _createParticipantData.CreateParticipantEntryAsync(participantData, connectionString);
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
