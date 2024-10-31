namespace NHS.CohortManager.CohortDistributionDataServices;

using System.Net;
using System.Text.Json;
using Common;
using Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;

/// <summary>
/// Azure Function for retrieving cohort distribution data based on ScreeningServiceId.
/// </summary>
/// <param name="req">The HTTP request data containing query parameters and request details.</param>
/// <param name="requestId">query parameter.</param>
/// <returns>
/// HTTP response with:
/// - 400 Bad Request if parameters are invalid or missing.
/// - 204 No Content if no data is found.
/// - 200 OK - List<CohortDistributionParticipant> in JSON format.
/// - 500 Internal Server Error if an exception occurs.
/// </returns>
public class RetrieveCohortDistributionData
{
    private readonly ILogger<RetrieveCohortDistributionData> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ICreateCohortDistributionData _createCohortDistributionData;
    private readonly IExceptionHandler _exceptionHandler;
    private const int rowCount = 100;
    public RetrieveCohortDistributionData(ILogger<RetrieveCohortDistributionData> logger, ICreateCohortDistributionData createCohortDistributionData, ICreateResponse createResponse, IExceptionHandler exceptionHandler)
    {
        _logger = logger;
        _createCohortDistributionData = createCohortDistributionData;
        _createResponse = createResponse;
        _exceptionHandler = exceptionHandler;
    }

[Function("RetrieveCohortDistributionData")]
public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
{
    var requestId = req.Query["requestId"];
    var screeningServiceId = (int)ServiceProvider.BSS;
    List<CohortDistributionParticipant> cohortDistributionParticipants;

    try
    {
        if (string.IsNullOrEmpty(requestId))
        {
            cohortDistributionParticipants = _createCohortDistributionData
                .GetUnextractedCohortDistributionParticipantsByScreeningServiceId(screeningServiceId, rowCount);
        }
        else
        {
            var requestIdsList = _createCohortDistributionData
                .GetOutstandingCohortRequestAudits(requestId)
                .Select(s => s.RequestId)
                .ToList();

            cohortDistributionParticipants = _createCohortDistributionData.GetParticipantsByRequestIds(requestIdsList);
        }

        if (cohortDistributionParticipants.Count == 0) return _createResponse.CreateHttpResponse(HttpStatusCode.NoContent, req);

        var cohortDistributionParticipantsJson = JsonSerializer.Serialize(cohortDistributionParticipants);
        return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, cohortDistributionParticipantsJson);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, ex.Message);
        await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, "", "", "", "N/A");
        return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
    }
}

}
