namespace NHS.CohortManager.CohortDistributionDataServices;

using System.Net;
using System.Text.Json;
using Common;
using Common.Interfaces;
using Data.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

public class RetrieveCohortDistributionData
{
    private readonly ILogger<RetrieveCohortDistributionData> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ICreateCohortDistributionData _createCohortDistributionData;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly HttpRequestHelper _httpRequestHelper;
    public RetrieveCohortDistributionData(ILogger<RetrieveCohortDistributionData> logger, ICreateCohortDistributionData createCohortDistributionData, ICreateResponse createResponse, IExceptionHandler exceptionHandler, HttpRequestHelper httpRequestHelper)
    {
        _logger = logger;
        _createCohortDistributionData = createCohortDistributionData;
        _createResponse = createResponse;
        _exceptionHandler = exceptionHandler;
        _httpRequestHelper = httpRequestHelper;
    }

    [Function("RetrieveCohortDistributionData")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        int serviceProviderId = HttpRequestHelper.GetServiceProviderId(req);
        int rowCount = HttpRequestHelper.GetRowCount(req);

        if (rowCount == 0) return _httpRequestHelper.LogErrorResponse(req, "User has requested 0 rows, which is not possible.");
        if (rowCount >= 1000) return _httpRequestHelper.LogErrorResponse(req, "User cannot request more than 1000 rows at a time.");
        if (serviceProviderId == 0) return _httpRequestHelper.LogErrorResponse(req, "No ServiceProviderId has been provided.");

        try
        {
            var cohortDistributionParticipants = _createCohortDistributionData.ExtractCohortDistributionParticipants(serviceProviderId, rowCount);
            if (cohortDistributionParticipants.Count == 0) _createResponse.CreateHttpResponse(HttpStatusCode.NoContent, req);

            var cohortDistributionParticipantsJson = JsonSerializer.Serialize(cohortDistributionParticipants);
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, cohortDistributionParticipantsJson);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, "", "");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }
}
