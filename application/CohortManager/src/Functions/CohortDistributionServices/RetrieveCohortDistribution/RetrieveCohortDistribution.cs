namespace NHS.CohortManager.CohortDistributionDataServices;

using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Common;
using Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model.DTO;

/// <summary>
/// Azure Function for retrieving cohort distribution data based on ScreeningServiceId.
/// </summary>
/// <param name="req">The HTTP request data containing query parameters and request details.</param>
/// <param name="requestId">query parameter.</param>
/// <param name="rowCount">query parameter.</param>
/// <param name="screeningServiceId">query parameter.</param>
/// <returns>
/// HTTP response with:
/// - 400 Bad Request if parameters are invalid or missing.
/// - 204 No Content if no data is found.
/// - 200 OK - List<CohortDistributionParticipant> in JSON format.
/// - 500 Internal Server Error if an exception occurs.
/// </returns>
public class RetrieveCohortDistributionData
{
    private readonly ILogger<RetrieveCohortDistributionData> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ICreateCohortDistributionData _createCohortDistributionData;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly RetrieveCohortDistributionConfig _config;



    public RetrieveCohortDistributionData(ILogger<RetrieveCohortDistributionData> logger,
        ICreateCohortDistributionData createCohortDistributionData,
        ICreateResponse createResponse,
        IExceptionHandler exceptionHandler,
        IOptions<RetrieveCohortDistributionConfig> config
        )
    {
        _logger = logger;
        _createCohortDistributionData = createCohortDistributionData;
        _createResponse = createResponse;
        _exceptionHandler = exceptionHandler;
        _config = config.Value;
    }

    [Function(nameof(RetrieveCohortDistributionData))]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        List<CohortDistributionParticipantDto> cohortDistributionParticipants;
        try
        {
            int rowCount = _config.MaxRowCount;
            bool retrieveSupersededRecordsLast = _config.RetrieveSupersededRecordsLast;
            if (!string.IsNullOrEmpty(req.Query["rowCount"]) && int.TryParse(req.Query["rowCount"], out int rowCountParam))
            {
                rowCount = Math.Min(rowCount, rowCountParam);
            }

            //If no requestID is provided we send back a batch of unextracted participants
            if (string.IsNullOrEmpty(req.Query["requestId"]))
            {
                cohortDistributionParticipants = await _createCohortDistributionData
                    .GetUnextractedCohortDistributionParticipants(rowCount, retrieveSupersededRecordsLast);

                return CreateResponse(cohortDistributionParticipants, req);
            }

            if (!Guid.TryParse(req.Query["requestId"], out Guid requestId))
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
            }


            var nextBatch = await _createCohortDistributionData.GetNextCohortRequestAudit(requestId);

            if (nextBatch != null)
            {
                Guid requestIdFromDatabase = nextBatch.RequestId;
                cohortDistributionParticipants = await _createCohortDistributionData.GetCohortDistributionParticipantsByRequestId(requestIdFromDatabase);
                return CreateResponse(cohortDistributionParticipants, req);
            }

            cohortDistributionParticipants = await _createCohortDistributionData.GetUnextractedCohortDistributionParticipants(rowCount, retrieveSupersededRecordsLast);

            return CreateResponse(cohortDistributionParticipants, req);

        }
        catch (KeyNotFoundException keyNotFoundException)
        {
            _logger.LogWarning(keyNotFoundException, "RequestId not found in the database");
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, "", "", "", "N/A");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }

    private HttpResponseData CreateResponse(List<CohortDistributionParticipantDto> participantDtos, HttpRequestData req)
    {
        return participantDtos.Count == 0
            ? _createResponse.CreateHttpResponse(HttpStatusCode.NoContent, req)
            : _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, JsonSerializer.Serialize(participantDtos));
    }

}
