namespace NHS.CohortManager.CohortDistributionDataServices;

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Common;
using Common.Interfaces;
using DataServices.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;

public class AddCohortDistributionDataFunction
{
    private readonly ILogger<AddCohortDistributionDataFunction> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ICreateCohortDistributionData _createCohortDistributionData;
    private readonly IExceptionHandler _exceptionHandler;

    private readonly IDataServiceClient<CohortDistribution> _cohortDistributionDataService;

    public AddCohortDistributionDataFunction(ILogger<AddCohortDistributionDataFunction> logger, ICreateCohortDistributionData createCohortDistributionData, ICreateResponse createResponse, IExceptionHandler exceptionHandler, IDataServiceClient<CohortDistribution> cohortDistributionDataService)
    {
        _logger = logger;
        _createCohortDistributionData = createCohortDistributionData;
        _createResponse = createResponse;
        _exceptionHandler = exceptionHandler;
        _cohortDistributionDataService = cohortDistributionDataService;
    }

    [Function("AddCohortDistributionData")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        var participantCsvRecord = new CohortDistributionParticipant();
        try
        {
            string requestBody = "";

            using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBody = await reader.ReadToEndAsync();
                participantCsvRecord = JsonSerializer.Deserialize<CohortDistributionParticipant>(requestBody);
            }

            var isAdded = await InsertCohortDistributionData(participantCsvRecord);
            if (isAdded)
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
            }
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
        catch (Exception ex)
        {
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, participantCsvRecord.NhsNumber, "", "", JsonSerializer.Serialize(participantCsvRecord));
            _logger.LogError(ex, ex.Message);
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }

    private async Task<bool> InsertCohortDistributionData(CohortDistributionParticipant cohortDistributionParticipant)
    {
        var cohortDistributionParticipantToAdd = cohortDistributionParticipant.ToCohortDistributionParticipant();
        var isAdded = await _cohortDistributionDataService.Add(cohortDistributionParticipantToAdd);

        return isAdded;
    }
}
