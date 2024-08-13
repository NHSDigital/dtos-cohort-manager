using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;
using NHS.CohortManager.CohortDistribution;

namespace NHS.CohortManager.CohortDistribution.ValidateCohortDistributionRecord;

public class ValidateCohortDistributionRecord
{
    private readonly ILogger<ValidateCohortDistributionRecord> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ICreateCohortDistributionData _createCohortDistributionData;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly ICallFunction _callFunction;

    private readonly ICreateParticipant _createParticipant;


    public ValidateCohortDistributionRecord(ILogger<ValidateCohortDistributionRecord> logger, ICreateResponse createResponse, ICreateCohortDistributionData createCohortDistributionData, IExceptionHandler exceptionHandler, ICallFunction callFunction, ICreateParticipant createParticipant)
    {
        _createResponse = createResponse;
        _createCohortDistributionData = createCohortDistributionData;
        _exceptionHandler = exceptionHandler;
        _callFunction = callFunction;
        _createParticipant = createParticipant;
        _logger = logger;
    }

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
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, requestBody.NhsNumber, requestBody.FileName);
            _logger.LogError($"there was an error while deserializing records");
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }

        try
        {
            var lastParticipant = _createCohortDistributionData.GetLastCohortDistributionParticipant(requestBody.NhsNumber);
            var validationRes = await ValidateDataAsync(lastParticipant, requestBody.CohortDistributionParticipant, requestBody.FileName);

            if (validationRes)
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.Created, req);
            }
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);

        }
        catch (Exception ex)
        {
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, requestBody.NhsNumber, requestBody.FileName);
            _logger.LogError($"there was an error validating the cohort distribution records {ex.Message}");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }

    private async Task<bool> ValidateDataAsync(CohortDistributionParticipant existingParticipant, CohortDistributionParticipant newParticipant, string fileName)
    {
        var json = JsonSerializer.Serialize(new LookupValidationRequestBody(
            _createParticipant.ConvertCohortDistributionRecordToParticipant(existingParticipant),
            _createParticipant.ConvertCohortDistributionRecordToParticipant(newParticipant),
            fileName
        ));


        var response = await _callFunction.SendPost(Environment.GetEnvironmentVariable("LookupValidationURL"), json);
        if (response.StatusCode == HttpStatusCode.Created)
        {
            return true;
        }


        return false;
    }
}

