namespace NHS.CohortManager.CohortDistributionDataServices;

using System.Net;
using System.Text.Json;
using Common;
using Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
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
    private readonly IHttpParserHelper _httpParserHelper;
    public RetrieveCohortDistributionData(ILogger<RetrieveCohortDistributionData> logger, ICreateCohortDistributionData createCohortDistributionData, ICreateResponse createResponse, IExceptionHandler exceptionHandler, IHttpParserHelper httpParserHelper)
    {
        _logger = logger;
        _createCohortDistributionData = createCohortDistributionData;
        _createResponse = createResponse;
        _exceptionHandler = exceptionHandler;
        _httpParserHelper = httpParserHelper;
    }

    [Function(nameof(RetrieveCohortDistributionData))]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        var requestId = req.Query["requestId"];
        int screeningServiceId = _httpParserHelper.GetScreeningServiceId(req);
        int rowCount = _httpParserHelper.GetRowCount(req);
        List<CohortDistributionParticipantDto> cohortDistributionParticipants;

        try
        {
            if (string.IsNullOrEmpty(requestId))
            {
                cohortDistributionParticipants = _createCohortDistributionData
                    .GetUnextractedCohortDistributionParticipantsByScreeningServiceId(screeningServiceId, rowCount);
            }
            else
            {
                cohortDistributionParticipants = _createCohortDistributionData.GetCohortDistributionParticipantsByRequestId(requestId);
            }

            if (cohortDistributionParticipants.Count == 0) return _createResponse.CreateHttpResponse(HttpStatusCode.NoContent, req);

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
