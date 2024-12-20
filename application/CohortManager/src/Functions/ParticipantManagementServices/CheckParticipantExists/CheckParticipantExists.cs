/// <summary>
/// Checks if a participant exists in the participant management table.
/// </summary>
/// <param name="participant">BasicParticipantData containing an NHS number & screening ID.</param>
/// <returns>HttpResponseData: 200 if the participant exists, or 404 with an error response if it doesn't.

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
using System.Net.Sockets;

public class CheckParticipantExists
{
    private readonly IDataServiceClient<ParticipantManagement> _participantManagementClient;
    private readonly ILogger<CheckParticipantExists> _logger;
    private readonly ICreateResponse _createResponse;
    private long _nhsNumber;
    private long _screeningId;
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

            var participant = JsonSerializer.Deserialize<BasicParticipantData>(requestBodyJson);

            if (participant.NhsNumber == null || participant.ScreeningId == null)
                throw new ArgumentException("Request is missing required paramaters");

            _nhsNumber = long.Parse(participant.NhsNumber);
            _screeningId = long.Parse(participant.ScreeningId);
        }
        catch (Exception ex)
        {
            _logger.LogError("{DateTime}: Request could not be desiralised: {Ex}", DateTime.UtcNow, ex);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
        try 
        {
            var dbParticipants = await _participantManagementClient.GetByFilter(i => i.NHSNumber == _nhsNumber && i.ScreeningId == _screeningId);
            if (dbParticipants == null || !dbParticipants.Any())
                return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.NotFound, req, "Participant not found");

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
        }
        catch (Exception ex)
        {
            _logger.LogError("{DateTime}: Request could not be processed: {Ex}", DateTime.UtcNow, ex);
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }
}