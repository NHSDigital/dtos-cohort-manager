namespace NHS.CohortManager.CohortDistribution.ValidateCohortDistributionRecord;
using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using DataServices.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using Model.Enums;
using NHS.Screening.ValidateCohortDistributionRecord;

public class ValidateCohortDistributionRecord
{
    private readonly ILogger<ValidateCohortDistributionRecord> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IHttpClientFunction _httpClientFunction;
    private readonly IDataServiceClient<CohortDistribution> _cohortDistributionDataService;
    private readonly ValidateCohortDistributionRecordConfig _config;


    public ValidateCohortDistributionRecord(
        ILogger<ValidateCohortDistributionRecord> logger,
        ICreateResponse createResponse,
        IExceptionHandler exceptionHandler,
        IHttpClientFunction httpClientFunction,
        IDataServiceClient<CohortDistribution> cohortDistributionDataService,
        IOptions<ValidateCohortDistributionRecordConfig> validateCohortDistributionRecordConfig
    )
    {
        _createResponse = createResponse;
        _exceptionHandler = exceptionHandler;
        _httpClientFunction = httpClientFunction;
        _logger = logger;
        _cohortDistributionDataService = cohortDistributionDataService;
        _config = validateCohortDistributionRecordConfig.Value;
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
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, JsonSerializer.Serialize(validationResult));

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

        _logger.LogInformation("Getting last cohort distribution record in ValidateCohortDistributionRecord");

        if (!long.TryParse(existingNhsNumber, out nhsNumber))
        {
            throw new FormatException("Unable to parse NHS Number");
        }
        // using get by filter here because we can get more than one record from the database
        var cohortDistributionRecords = await _cohortDistributionDataService.GetByFilter(x => x.NHSNumber == nhsNumber);

        // we do this because get by filter will return an empty array
        if (cohortDistributionRecords.Any())
        {
            CohortDistribution latestParticipant = cohortDistributionRecords
                                                    .OrderByDescending(x => x.CohortDistributionId)
                                                    .FirstOrDefault();

            _logger.LogInformation("last cohort distribution record in ValidateCohortDistributionRecord was got with result {record}", latestParticipant);
            return new CohortDistributionParticipant(latestParticipant);
        }
        return new CohortDistributionParticipant();
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

        _logger.LogInformation("Sending record to validation in ValidateCohortDistributionRecord");

        var response = await _httpClientFunction.SendPost(_config.LookupValidationURL, json);
        var responseBodyJson = await _httpClientFunction.GetResponseText(response);

        _logger.LogInformation("validation response in ValidateCohortDistributionRecord was {Response}", responseBodyJson);
        var responseBody = JsonSerializer.Deserialize<ValidationExceptionLog>(responseBodyJson);

        return responseBody;


    }
}
