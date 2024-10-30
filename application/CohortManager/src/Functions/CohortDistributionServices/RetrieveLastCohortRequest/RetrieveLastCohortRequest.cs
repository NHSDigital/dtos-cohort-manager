namespace NHS.CohortManager.CohortDistributionDataServices;

using System.Net;
using System.Text.Json;
using Common;
using Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

/// /// <summary>
/// Azure Function for retrieving cohort audit history data based on LastRequestId.
/// </summary>
/// <param name="req">The HTTP request data containing query parameters and request details.</param>
/// Takes a requestId and checks if there are any new requests by date that have a 500 status
/// if there is all cohort participants for that request are returned.
/// if there is not a 204 no content is returned.
/// <param name="lastRequestId">Required.</param>
/// <returns>
/// HTTP response with:
/// - 400 Bad Request if lastRequestId is missing.
/// - 204 No Content if no data is found.
/// - 200 OK - List<CohortDistributionParticipant> in JSON format.
/// - 500 Internal Server Error if an exception occurs.
/// </returns>
public class RetrieveLastCohortRequest
{
    private readonly ILogger<RetrieveLastCohortRequest> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ICreateCohortDistributionData _createCohortDistributionData;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IHttpParserHelper _httpParserHelper;

    public RetrieveLastCohortRequest(ILogger<RetrieveLastCohortRequest> logger, ICreateCohortDistributionData createCohortDistributionData, ICreateResponse createResponse, IExceptionHandler exceptionHandler, IHttpParserHelper httpParserHelper)
    {
        _logger = logger;
        _createCohortDistributionData = createCohortDistributionData;
        _createResponse = createResponse;
        _exceptionHandler = exceptionHandler;
        _httpParserHelper = httpParserHelper;
    }
        [Function("RetrieveLastCohortRequest")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            var lastRequestId = req.Query["lastRequestId"];
            if (string.IsNullOrEmpty(lastRequestId)) return _httpParserHelper.LogErrorResponse(req, "No RequestId has been provided.");

            try
            {
                var requestIdsList = _createCohortDistributionData.GetOutstandingCohortRequestAudits(lastRequestId).Select(s => s.RequestId).ToList();
                if (requestIdsList.Count == 0) return _createResponse.CreateHttpResponse(HttpStatusCode.NoContent, req);

                var cohortDistributionParticipants = _createCohortDistributionData.GetParticipantsByRequestIds(requestIdsList);

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
