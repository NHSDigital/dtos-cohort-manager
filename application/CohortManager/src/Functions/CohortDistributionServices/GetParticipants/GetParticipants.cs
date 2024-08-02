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
using Model.Enums;

public class GetParticipants
{
    private readonly ILogger<GetParticipants> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ICreateCohortDistributionData _cohortDistributionData;
    public GetParticipants(ILogger<GetParticipants> logger, ICreateResponse createResponse, ICreateCohortDistributionData cohortDistributionData)
    {
        _logger = logger;
        _createResponse = createResponse;
        _cohortDistributionData = cohortDistributionData;
    }

    [Function(nameof(GetParticipants))]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        var serviceProviderId = (int)ServiceProvider.BsSelect;
        var rowCount = GetRowCount(req);

        if (rowCount == 0) return LogAndCreateErrorResponse(req, "User has requested 0 rows, which is not possible.");

        try
        {
            var cohortDistributionParticipants = _cohortDistributionData.GetCohortDistributionParticipantsMock(serviceProviderId, rowCount);
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

    private HttpResponseData LogAndCreateErrorResponse(HttpRequestData req, string errorMessage)
    {
        _logger.LogError(errorMessage);
        return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
    }

}
