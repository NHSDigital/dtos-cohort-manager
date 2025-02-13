namespace NHS.CohortManager.CohortDistribution.ValidateCohortDistributionRecord;
using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Common.Interfaces;
using DataServices.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;

public class ValidateCohortDistributionRecord
{
    private readonly ILogger<ValidateCohortDistributionRecord> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly ICallFunction _callFunction;

    private readonly IDataServiceClient<CohortDistribution> _cohortDistributionDataService;


    public ValidateCohortDistributionRecord(ILogger<ValidateCohortDistributionRecord> logger, ICreateResponse createResponse, IExceptionHandler exceptionHandler, ICallFunction callFunction, IDataServiceClient<CohortDistribution> cohortDistributionDataService)
    {
        _createResponse = createResponse;
        _exceptionHandler = exceptionHandler;
        _callFunction = callFunction;
        _logger = logger;
        _cohortDistributionDataService = cohortDistributionDataService;
    }
    /// <summary>
    /// Deserializes a ValidateCohortDistributionRecordBody object.
    /// compares existing and new participant data, returns an appropriate HTTP response
    /// based on validation success or failure.
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    [Function("ValidateCohortDistributionRecord")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        var requestBody = new ValidateCohortDistributionRecordBody();
        try
        {
            string requestBodyJson;
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBodyJson = await reader.ReadToEndAsync();
            }

            requestBody = JsonSerializer.Deserialize<ValidateCohortDistributionRecordBody>(requestBodyJson);
        }
        catch (Exception ex)
        {
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, requestBody.NhsNumber, requestBody.FileName, "", JsonSerializer.Serialize(requestBody.CohortDistributionParticipant));
            _logger.LogError(ex, "There was an error while deserializing records");
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }

        try
        {
            var existingParticipant = await GetLastCohortDistributionParticipantAsync(requestBody.NhsNumber);
            var newParticipant = requestBody.CohortDistributionParticipant;

            var validationResult = await ValidateDataAsync(existingParticipant, newParticipant, requestBody.FileName);
            if (validationResult.CreatedException)
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.Created, req, JsonSerializer.Serialize(validationResult));
            }
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);

        }
        catch (Exception ex)
        {
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, requestBody.NhsNumber, requestBody.FileName, "", JsonSerializer.Serialize(requestBody.CohortDistributionParticipant));
            _logger.LogError(ex, "There was an error validating the cohort distribution records {Message}", ex.Message);
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }


    private async Task<CohortDistributionParticipant> GetLastCohortDistributionParticipantAsync(string existingNhsNumber)
    {
        long nhsNumber;
        nhsNumber = long.TryParse(existingNhsNumber, out long tempNhsNumber) ? tempNhsNumber : throw new FormatException("Unable to parse NHS Number");

        var cohortDistributionRecord = await _cohortDistributionDataService.GetSingleByFilter(x => x.NHSNumber == nhsNumber);
        if (cohortDistributionRecord == null)
        {
            return new CohortDistributionParticipant();
        }
        return new CohortDistributionParticipant(cohortDistributionRecord);
    }

    private async Task<ValidationExceptionLog> ValidateDataAsync(CohortDistributionParticipant existingParticipant, CohortDistributionParticipant newParticipant, string fileName)
    {
        if (existingParticipant == null)
        {
            existingParticipant = new CohortDistributionParticipant();
        }

        var json = JsonSerializer.Serialize(new LookupValidationRequestBody(
            new Participant(existingParticipant),
            new Participant(newParticipant),
            fileName,
            RulesType.CohortDistribution
        ));


        var response = await _callFunction.SendPost(Environment.GetEnvironmentVariable("LookupValidationURL"), json);
        var responseBodyJson = await _callFunction.GetResponseText(response);
        var responseBody = JsonSerializer.Deserialize<ValidationExceptionLog>(responseBodyJson);

        return responseBody;
    }
}
