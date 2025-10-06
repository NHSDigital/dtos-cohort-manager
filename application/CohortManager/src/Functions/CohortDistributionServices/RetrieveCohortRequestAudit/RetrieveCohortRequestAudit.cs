namespace NHS.CohortManager.CohortDistributionDataServices;

using System.Globalization;
using System.Net;
using System.Text.Json;
using Common;
using Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NHS.CohortManager.Shared.Utilities;

/// <summary>
/// Azure Function for retrieving cohort audit history data based on RequestId, Status Code and Date.
/// </summary>
/// <param name="req">The HTTP request data containing query parameters and request details.</param>
/// <param name="requestId">Optional. If unknown will use other query params. The HTTP request data containing query parameters and request details.</param>
/// <param name="statusCode">Optional. Http Status Code. 200, 500 or null. If null will be ignored as a query param.</param>
/// <param name="dateFrom"> Optional. If empty will return all records for all dates.</param>
/// <returns>
/// HTTP response with:
/// - 400 Bad Request if parameters are invalid or missing.
/// - 204 No Content if no data is found.
/// - 200 OK - List<RequestAudit> in JSON format.  // BS_SELECT_REQUEST_AUDIT
/// - 500 Internal Server Error if an exception occurs.
/// </returns>
public class RetrieveCohortRequestAudit
{
    private readonly ILogger<RetrieveCohortRequestAudit> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ICreateCohortDistributionData _createCohortDistributionData;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IHttpParserHelper _httpParserHelper;
    public const string Iso8601 = "yyyyMMdd";

    public RetrieveCohortRequestAudit(ILogger<RetrieveCohortRequestAudit> logger, ICreateCohortDistributionData createCohortDistributionData, ICreateResponse createResponse, IExceptionHandler exceptionHandler, IHttpParserHelper httpParserHelper)
    {
        _logger = logger;
        _createCohortDistributionData = createCohortDistributionData;
        _createResponse = createResponse;
        _exceptionHandler = exceptionHandler;
        _httpParserHelper = httpParserHelper;
    }

    [Function("RetrieveCohortRequestAudit")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        var requestId = req.Query["requestId"];
        var statusCode = req.Query["statusCode"];
        var dateFromQuery = req.Query["dateFrom"];
        var acceptedStatusCodes = new string[] { ((int)HttpStatusCode.OK).ToString(), ((int)HttpStatusCode.InternalServerError).ToString(), ((int)HttpStatusCode.NoContent).ToString() };
        DateTime? dateFrom = null;

        if (!string.IsNullOrEmpty(dateFromQuery))
        {
            bool isValidDateFormat = DateTime.TryParseExact(dateFromQuery, Iso8601, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date);
            if (!isValidDateFormat)
            {
                return _httpParserHelper.LogErrorResponse(req, "Invalid date format. Please use yyyyMMdd.");
            }
            dateFrom = date;
        }

        try
        {
            if (!string.IsNullOrEmpty(statusCode) && !acceptedStatusCodes.Contains(statusCode)) return _httpParserHelper.LogErrorResponse(req, "Invalid status code. Only status codes 200, 204 and 500 are accepted.");
            var cohortAuditHistoryList = await _createCohortDistributionData.GetCohortRequestAudit(requestId, statusCode, dateFrom);

            if (cohortAuditHistoryList.Count == 0)
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.NoContent, req);
            }

            var cohortAuditHistoryJson = JsonSerializer.Serialize(cohortAuditHistoryList);
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, cohortAuditHistoryJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExceptionMessage}", ex.Message);
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, "", "", "", "N/A");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }
}
