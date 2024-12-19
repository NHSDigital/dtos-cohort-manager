/// <summary>
/// Takes a participant from the queue, gets data from the demographic service,
/// validates the participant, then calls create participant, mark as eligible, and create cohort distribution
/// </summary>

namespace NHS.CohortManager.ParticipantManagementService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using DataServices.Client;
using System.Text.Json;
using System.Text;
using System.Net;
using Model;
using Common;

public class CheckParticipantExists
{
    private readonly IDataServiceClient<ParticipantManagement> _participantManagementClient;
    private readonly ILogger<CheckParticipantExists> _logger;
    private readonly ICreateResponse _createResponse;
    private BasicParticipantData _participant;
    public CheckParticipantExists(IDataServiceClient<ParticipantManagement> participantManagementClient,
                                ICreateResponse createResponse, ILogger<CheckParticipantExists> logger)
    {
        _participantManagementClient = participantManagementClient;
        _createResponse = createResponse;
        _logger = logger;
    }

    [Function(nameof(CheckParticipantExists))]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        try
        {
            string requestBodyJson;
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBodyJson = await reader.ReadToEndAsync();
            }

            _participant = JsonSerializer.Deserialize<BasicParticipantData>(requestBodyJson);

            if (_participant.NhsNumber == null || _participant.ScreeningId == null) throw new ArgumentException();
        }
        catch
        {
            _logger.LogError("{date}: Request could not be desiralised", DateTime.UtcNow, ex);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
        try 
        {
            var dbParticipant = await _participantManagementClient.GetByFilter(i => i.NHSNumber.ToString() == _participant.NhsNumber && i.ScreeningId.ToString() == _participant.ScreeningId);
            if (dbParticipant == null) return _createResponse.CreateHttpResponse(HttpStatusCode.NotFound, req, "Participant not found");

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
        }
        catch
        {
            // _logger.
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }
}