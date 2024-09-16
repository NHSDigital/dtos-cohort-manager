namespace NHS.CohortManager.CohortDistributionDataServices;

using System.Net;
using System.Text.Json;
using Common;
using Common.Interfaces;
using Data.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// Azure Function for retrieving previously retrieved cohort distribution data based on requestId.
/// </summary>
/// <param name="req">The HTTP request data containing query parameters and request details.</param>
/// <param name="requestId">query parameter.</param>
/// <param name="serviceProviderId">query parameter.</param>
/// <param name="rowCount">query parameter.</param>
/// <returns>
/// HTTP response with:
/// - 400 Bad Request if parameters are invalid or missing.
/// - 204 No Content if no data is found.
/// - 200 OK - List<CohortDistributionParticipant> in JSON format.
/// - 500 Internal Server Error if an exception occurs.
/// </returns>
public class RetrieveCohortReplay
{
    private readonly ILogger<RetrieveCohortReplay> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ICreateCohortDistributionData _createCohortDistributionData;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly HttpRequestHelper _httpRequestHelper;
    public RetrieveCohortReplay(ILogger<RetrieveCohortReplay> logger, ICreateCohortDistributionData createCohortDistributionData, ICreateResponse createResponse, IExceptionHandler exceptionHandler, HttpRequestHelper httpRequestHelper)
    {
        _logger = logger;
        _createCohortDistributionData = createCohortDistributionData;
        _createResponse = createResponse;
        _exceptionHandler = exceptionHandler;
        _httpRequestHelper = httpRequestHelper;
    }

    [Function("RetrieveCohortReplay")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        int serviceProviderId = HttpRequestHelper.GetServiceProviderId(req);
        int rowCount = HttpRequestHelper.GetRowCount(req);
        var requestId = req.Query["requestId"];

        if (rowCount == 0) return _httpRequestHelper.LogErrorResponse(req, "User has requested 0 rows, which is not possible.");
        if (rowCount >= 1000) return _httpRequestHelper.LogErrorResponse(req, "User cannot request more than 1000 rows at a time.");
        if (serviceProviderId == 0) return _httpRequestHelper.LogErrorResponse(req, "No ServiceProviderId has been provided.");
        if (string.IsNullOrEmpty(requestId)) return _httpRequestHelper.LogErrorResponse(req, "No RequestId has been provided.");

        try
        {
            var cohortDistributionParticipants = _createCohortDistributionData.GetCohortDistributionParticipantsByRequestId(serviceProviderId, rowCount, requestId);
            if (cohortDistributionParticipants.Count == 0) _createResponse.CreateHttpResponse(HttpStatusCode.NoContent, req);

            var cohortDistributionParticipantsJson = JsonSerializer.Serialize(cohortDistributionParticipants);
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, cohortDistributionParticipantsJson);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, "", "");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }
}
