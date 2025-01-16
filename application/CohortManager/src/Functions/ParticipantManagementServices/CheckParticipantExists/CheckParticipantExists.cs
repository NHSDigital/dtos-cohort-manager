/// <summary>
/// Checks if a participant exists in the participant management table.
/// </summary>
/// <param name="participant">BasicParticipantData containing an NHS number & screening ID.</param>
/// <returns>HttpResponseData: 200 if the participant exists, or 404 with an error response if it doesn't.</returns>

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
    public CheckParticipantExists(IDataServiceClient<ParticipantManagement> participantManagementClient,
                                ICreateResponse createResponse, ILogger<CheckParticipantExists> logger)
    {
        _participantManagementClient = participantManagementClient;
        _createResponse = createResponse;
        _logger = logger;
    }

    [Function(nameof(CheckParticipantExists))]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        long nhsNumber, screeningId;
        try
        {
            nhsNumber = long.Parse(req.Query["NhsNumber"]);
            screeningId = long.Parse(req.Query["ScreeningId"]);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError("{DateTime}: Request is missing required parameters: {Ex}", DateTime.UtcNow, ex);
            return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.BadRequest, req, "Request is missing required parameters");
        }
        catch (Exception ex)
        {
            _logger.LogError("{DateTime}: Invalid Request: {Ex}", DateTime.UtcNow, ex);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
        try 
        {
            var dbParticipants = await _participantManagementClient.GetByFilter(i => i.NHSNumber == nhsNumber && i.ScreeningId == screeningId);
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