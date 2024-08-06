namespace NHS.CohortManager.CohortDistributionServices;

using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Common;
using Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;

public class GetCohortDistributionParticipants
{
    private readonly ILogger<GetCohortDistributionParticipants> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ICreateCohortDistributionData _cohortDistributionData;
    public GetCohortDistributionParticipants(ILogger<GetCohortDistributionParticipants> logger, ICreateResponse createResponse, ICreateCohortDistributionData cohortDistributionData)
    {
        _logger = logger;
        _createResponse = createResponse;
        _cohortDistributionData = cohortDistributionData;
    }

    [Function(nameof(GetCohortDistributionParticipants))]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        var serviceProviderId = (int)ServiceProvider.BsSelect;
        var rowCount = GetRowCount(req);

        if (rowCount == 0) return LogErrorResponse(req, "User has requested 0 rows, which is not possible.");

        try
        {
            var testDataJson = CohortDistributionHelper.GetCohortMockJsonFile(MockTestFiles.CohortMockData1000Participants);
            var cohortDistributionParticipants = _cohortDistributionData.GetCohortDistributionParticipantsMock(serviceProviderId, rowCount, testDataJson);
            if (cohortDistributionParticipants != null)
            {
                var cohortDistributionParticipantsJson = JsonSerializer.Serialize(cohortDistributionParticipants);
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, cohortDistributionParticipantsJson);
            }

            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching cohort distribution participants.");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }

    private static int GetRowCount(HttpRequestData req)
    {
        var rowCountString = req.Query["rowCount"];
        return int.TryParse(rowCountString, out int rowCount) ? rowCount : 0;
    }

    private HttpResponseData LogErrorResponse(HttpRequestData req, string errorMessage)
    {
        _logger.LogError(errorMessage);
        return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
    }
}
