namespace NHS.CohortManager.CohortDistributionDataServices;

using System.Net;
using System.Text.Json;
using Common;
using Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// Azure Function for retrieving the last cohort request after a given RequestId.
/// todo: Add more details.
/// </summary>
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
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            var lastRequestId = req.Query["lastRequestId"];
            if (string.IsNullOrEmpty(lastRequestId)) return _httpParserHelper.LogErrorResponse(req, "No RequestId has been provided.");

            try
            {
                var nextRecords = _createCohortDistributionData.GetLastCohortRequest(lastRequestId);
                if (nextRecords.Result.Count == 0) return _createResponse.CreateHttpResponse(HttpStatusCode.NoContent, req);

                var nextRecordsJson = JsonSerializer.Serialize(nextRecords);
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, nextRecordsJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, "", "", "", "N/A");
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
            }
        }
    }
