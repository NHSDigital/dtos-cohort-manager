namespace NHS.CohortManager.CohortDistributionDataServices;

using System.Net;
using System.Text.Json;
using Common;
using Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// Azure Function for retrieving cohort audit history data based on RequestId, Status Code and Date.
/// </summary>
/// <param name="req">The HTTP request data containing query parameters and request details.</param>
/// <param name="requestId">The HTTP request data containing query parameters and request details.</param>
/// <param name="statusCode">Http Status Code. Most likely 200 or 500</param>
/// <param name="date"> Optional? If empty will return all records.</param>
/// <returns>
/// HTTP response with:
/// - 400 Bad Request if parameters are invalid or missing.
/// - 204 No Content if no data is found.
/// - 200 OK - List<RequestAudit> in JSON format.  // BS_SELECT_REQUEST_AUDIT
/// - 500 Internal Server Error if an exception occurs.
/// </returns>
public class RetrieveCohortAuditHistory
{
    private readonly ILogger<RetrieveCohortAuditHistory> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ICreateCohortDistributionData _createCohortDistributionData;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IHttpParserHelper _httpParserHelper;

    public RetrieveCohortAuditHistory(ILogger<RetrieveCohortAuditHistory> logger, ICreateCohortDistributionData createCohortDistributionData, ICreateResponse createResponse, IExceptionHandler exceptionHandler, IHttpParserHelper httpParserHelper)
    {
        _logger = logger;
        _createCohortDistributionData = createCohortDistributionData;
        _createResponse = createResponse;
        _exceptionHandler = exceptionHandler;
        _httpParserHelper = httpParserHelper;
    }

    [Function("RetrieveCohortAuditHistory")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        var requestId = req.Query["requestId"];
        var statusCode = req.Query["statusCode"];
        var dateFrom = req.Query["dateFrom"];
        var acceptedStatusCodes = new string[] { ((int)HttpStatusCode.OK).ToString(), ((int)HttpStatusCode.InternalServerError).ToString() };

        if (string.IsNullOrEmpty(requestId)) return _httpParserHelper.LogErrorResponse(req, "No request Id provided.");
        if (!acceptedStatusCodes.Contains(statusCode)) return _httpParserHelper.LogErrorResponse(req, "Invalid status code. Only status codes 200 and 500 are accepted.");

        try
        {
            var cohortAuditHistoryList = _createCohortDistributionData.GetCohortAuditHistory(requestId, statusCode, dateFrom);
            if (cohortAuditHistoryList.Count == 0) return _createResponse.CreateHttpResponse(HttpStatusCode.NoContent, req);

            var cohortAuditHistoryJson = JsonSerializer.Serialize(cohortAuditHistoryList);
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, cohortAuditHistoryJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, "", "", "", "N/A");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }
}
