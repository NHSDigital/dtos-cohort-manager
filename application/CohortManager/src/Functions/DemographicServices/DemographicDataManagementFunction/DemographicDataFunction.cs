namespace NHS.CohortManager.DemographicServices;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Data.Database;
using DataServices.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.Extensions.Logging;
using Model;

public class DemographicDataFunction
{
    private readonly ILogger<DemographicDataFunction> _logger;
    private readonly ICreateResponse _createResponse;

    private readonly IDataServiceClient<ParticipantDemographic> _participantDemographic;

    public DemographicDataFunction(ILogger<DemographicDataFunction> logger, ICreateResponse createResponse, IDataServiceClient<ParticipantDemographic> participantDemographic)
    {
        _logger = logger;
        _createResponse = createResponse;
        _participantDemographic = participantDemographic;
    }

    [Function("DemographicDataFunction")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        return await Main(req, false);
    }

    /// <summary>
    /// Gets filtered demographic data from the demographic data service,
    /// this endpoint is used by the external BI product
    /// </summary>
    /// <param name="Id">The NHS number to get the demographic data for.</param>
    /// <returns>JSON response containing the Primary Care Provider & Preferred Language</returns>
    [Function("DemographicDataFunctionExternal")]
    public async Task<HttpResponseData> RunExternal([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        return await Main(req, true);
    }

    private async Task<HttpResponseData> Main(HttpRequestData req, bool externalRequest)
    {
        try
        {
            string NHSNumber = req.Query["Id"];

            var demographicData = await GetDemographicData(NHSNumber);
            var data = JsonSerializer.Serialize(demographicData);

            if (data == null)
            {
                _logger.LogInformation("demographic function failed");
                return _createResponse.CreateHttpResponse(HttpStatusCode.NotFound, req, "Participant not found");
            }

            // Filters out unnecessary data for use in the BI product
            if (externalRequest)
            {
                var filteredData = JsonSerializer.Deserialize<FilteredDemographicData>(data);
                data = JsonSerializer.Serialize(filteredData);
            }

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "There has been an error saving demographic data: {Message}", ex.Message);
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }

    private async Task<Demographic> GetDemographicData(string nhsNumber)
    {
        long nhsNumberLong;
        if (!long.TryParse(nhsNumber, out nhsNumberLong))
        {
            throw new FormatException("Could not parse NhsNumber");
        }
        var result = await _participantDemographic.GetSingleByFilter(x => x.NhsNumber == nhsNumberLong);
        return result.ToDemographic();
    }
}
