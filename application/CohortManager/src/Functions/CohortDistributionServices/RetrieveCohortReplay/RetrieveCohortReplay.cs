namespace NHS.CohortManager.CohortDistributionDataServices;

using System.Net;
using System.Text.Json;
using Common;
using Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// Azure Function for retrieving previously retrieved cohort distribution data based on requestId.
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
public class RetrieveCohortReplay
{
    private readonly ILogger<RetrieveCohortReplay> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ICreateCohortDistributionData _createCohortDistributionData;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IHttpParserHelper _httpParserHelper;

    public RetrieveCohortReplay(ILogger<RetrieveCohortReplay> logger, ICreateCohortDistributionData createCohortDistributionData, ICreateResponse createResponse, IExceptionHandler exceptionHandler, IHttpParserHelper httpParserHelper)
    {
        _logger = logger;
        _createCohortDistributionData = createCohortDistributionData;
        _createResponse = createResponse;
        _exceptionHandler = exceptionHandler;
        _httpParserHelper = httpParserHelper;
    }

    [Function("RetrieveCohortReplay")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        var requestId = req.Query["requestId"];
        if (string.IsNullOrEmpty(requestId)) return _httpParserHelper.LogErrorResponse(req, "No RequestId has been provided.");

        try
        {
            var cohortDistributionParticipants = _createCohortDistributionData.GetCohortDistributionParticipantsByRequestId(requestId);
            if (cohortDistributionParticipants.Count == 0) return _createResponse.CreateHttpResponse(HttpStatusCode.NoContent, req);

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
