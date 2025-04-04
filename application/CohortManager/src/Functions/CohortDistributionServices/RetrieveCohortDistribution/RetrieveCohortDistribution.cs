namespace NHS.CohortManager.CohortDistributionDataServices;

using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Threading.Tasks;
using Common;
using Common.Interfaces;
using Data.Database;
using DataServices.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;
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

    private readonly IDataServiceClient<CohortDistribution> _cohortDistributionDataServiceClient;


    public RetrieveCohortDistributionData(ILogger<RetrieveCohortDistributionData> logger, ICreateCohortDistributionData createCohortDistributionData, ICreateResponse createResponse, IExceptionHandler exceptionHandler, IHttpParserHelper httpParserHelper, IDataServiceClient<CohortDistribution> cohortDistributionDataServiceClient)
    {
        _logger = logger;
        _createCohortDistributionData = createCohortDistributionData;
        _createResponse = createResponse;
        _exceptionHandler = exceptionHandler;
        _httpParserHelper = httpParserHelper;
        _cohortDistributionDataServiceClient = cohortDistributionDataServiceClient;
    }

    [Function(nameof(RetrieveCohortDistributionData))]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        var requestId = req.Query["requestId"];
        int rowCount = _httpParserHelper.GetQueryParameterAsInt(req, "rowCount");
        var cohortDistributionParticipants = new List<CohortDistributionParticipantDto>();

        try
        {
            if (!string.IsNullOrEmpty(requestId))
            {
                var audit = await _createCohortDistributionData.GetNextCohortRequestAudit(requestId);
                Guid requestIdFromDatabase = audit.RequestId;

                if (requestId != null)
                {
                    cohortDistributionParticipants = await _createCohortDistributionData.GetCohortDistributionParticipantsByRequestId(requestIdFromDatabase);
                }
            }

            if (cohortDistributionParticipants.Count == 0)
            {
                cohortDistributionParticipants = await _createCohortDistributionData
                .GetUnextractedCohortDistributionParticipants(rowCount);
            }

            return cohortDistributionParticipants.Count == 0
                ? _createResponse.CreateHttpResponse(HttpStatusCode.NoContent, req)
                : _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, JsonSerializer.Serialize(cohortDistributionParticipants));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, "", "", "", "N/A");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }

    private async Task<List<CohortDistributionParticipantDto>> GetCohortDistributionParticipantsByRequestId(Guid requestId)
    {
        var recordToReturn = new CohortDistributionParticipant();
        var CohortDistributionList = await _cohortDistributionDataServiceClient.GetByFilter(x => x.RequestId == requestId);

        return CohortDistributionParticipantMapper.MapDto(CohortDistributionList.ToList());
    }
}
