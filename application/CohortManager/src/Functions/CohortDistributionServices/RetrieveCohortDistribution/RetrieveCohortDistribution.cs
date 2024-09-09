namespace NHS.CohortManager.CohortDistributionDataServices;

using System.Net;
using System.Text.Json;
using Common;
using Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

public class RetrieveCohortDistributionDataFunction
{
    private readonly ILogger<RetrieveCohortDistributionDataFunction> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ICreateCohortDistributionData _createCohortDistributionData;

    private readonly IExceptionHandler _exceptionHandler;
    public RetrieveCohortDistributionDataFunction(ILogger<RetrieveCohortDistributionDataFunction> logger, ICreateCohortDistributionData createCohortDistributionData, ICreateResponse createResponse, IExceptionHandler exceptionHandler)
    {
        _logger = logger;
        _createCohortDistributionData = createCohortDistributionData;
        _createResponse = createResponse;
        _exceptionHandler = exceptionHandler;
    }

    [Function("RetrieveCohortDistributionData")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        int serviceProviderId = GetServiceProviderId(req);
        int rowCount = GetRowCount(req);

        if (rowCount == 0) return LogErrorResponse(req, "User has requested 0 rows, which is not possible.");
        if (serviceProviderId == 0) return LogErrorResponse(req, "No ServiceProviderId has been provided.");

        try
        {
            var cohortDistributionParticipants = _createCohortDistributionData.ExtractCohortDistributionParticipants(serviceProviderId, rowCount);
            if (cohortDistributionParticipants.Any())
            {
                var cohortDistributionParticipantsJson = JsonSerializer.Serialize(cohortDistributionParticipants);
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, cohortDistributionParticipantsJson);
            }

            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, "", "");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }

    private static int GetQueryParameterAsInt(HttpRequestData req, string key, int defaultValue = 0)
    {
        var queryString = req.Query[key];
        return int.TryParse(queryString, out int value) ? value : defaultValue;
    }

    private static int GetRowCount(HttpRequestData req)
    {
        return GetQueryParameterAsInt(req, "rowCount");
    }

    private static int GetServiceProviderId(HttpRequestData req)
    {
        return GetQueryParameterAsInt(req, "serviceProviderId");
    }

    private HttpResponseData LogErrorResponse(HttpRequestData req, string errorMessage)
    {
        _logger.LogError(errorMessage);
        return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
    }
}
