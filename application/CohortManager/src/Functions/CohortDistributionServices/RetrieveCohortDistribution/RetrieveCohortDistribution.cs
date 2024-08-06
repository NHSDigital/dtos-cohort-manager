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
        try
        {
            var cohortDistributionParticipants = _createCohortDistributionData.ExtractCohortDistributionParticipants();
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
}
