namespace NHS.CohortManager.CohortDistribution.ValidateCohortDistributionRecord;
using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;

public class ValidateCohortDistributionRecord
{
    private readonly ILogger<ValidateCohortDistributionRecord> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ICreateCohortDistributionData _createCohortDistributionData;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly ICallFunction _callFunction;


    public ValidateCohortDistributionRecord(ILogger<ValidateCohortDistributionRecord> logger, ICreateResponse createResponse, ICreateCohortDistributionData createCohortDistributionData, IExceptionHandler exceptionHandler, ICallFunction callFunction)
    {
        _createResponse = createResponse;
        _createCohortDistributionData = createCohortDistributionData;
        _exceptionHandler = exceptionHandler;
        _callFunction = callFunction;
        _logger = logger;
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
                requestBodyJson = reader.ReadToEnd();
            }

            requestBody = JsonSerializer.Deserialize<ValidateCohortDistributionRecordBody>(requestBodyJson);
        }
        catch (Exception ex)
        {
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, requestBody.NhsNumber, requestBody.FileName, "", JsonSerializer.Serialize(requestBody.CohortDistributionParticipant));
            _logger.LogError($"there was an error while deserializing records");
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }

        try
        {
            var existingParticipant = _createCohortDistributionData.GetLastCohortDistributionParticipant(requestBody.NhsNumber);
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
            _logger.LogError($"there was an error validating the cohort distribution records {ex.Message}");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
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
