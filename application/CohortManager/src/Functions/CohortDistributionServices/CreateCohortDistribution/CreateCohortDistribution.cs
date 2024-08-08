namespace NHS.CohortManager.CohortDistributionService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Common;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NHS.CohortManager.CohortDistribution;
using System.Text;
using Model;

public class CreateCohortDistribution
{
    private readonly ICreateResponse _createResponse;
    private readonly ILogger<CreateCohortDistribution> _logger;
    private readonly ICallFunction _callFunction;
    private readonly ICohortDistributionHelper _CohortDistributionHelper;


    private readonly IExceptionHandler _exceptionHandler;

    public CreateCohortDistribution(ICreateResponse createResponse, ILogger<CreateCohortDistribution> logger, ICallFunction callFunction, ICohortDistributionHelper CohortDistributionHelper, IExceptionHandler exceptionHandler)
    {
        _createResponse = createResponse;
        _logger = logger;
        _callFunction = callFunction;
        _CohortDistributionHelper = CohortDistributionHelper;
        _exceptionHandler = exceptionHandler;
    }

    [Function("CreateCohortDistribution")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        var requestBody = new CreateCohortDistributionRequestBody();

        try
        {
            string requestBodyJson;
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBodyJson = reader.ReadToEnd();
            }

            requestBody = JsonSerializer.Deserialize<CreateCohortDistributionRequestBody>(requestBodyJson);
        }
        catch (Exception ex)
        {
            if (requestBody.NhsNumber != null && requestBody.FileName != null)
            {
                await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, requestBody.NhsNumber, requestBody.FileName);
            }
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, "", "");

            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        if (string.IsNullOrEmpty(requestBody.ScreeningService) || string.IsNullOrEmpty(requestBody.NhsNumber))
        {
            string logMessage = $"One or more of the required parameters is missing. NhsNumber: {requestBody.NhsNumber} ScreeningService: {requestBody.ScreeningService}";
            _logger.LogError(logMessage);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, logMessage);
        }

        try
        {
            var participantData = await _CohortDistributionHelper.RetrieveParticipantDataAsync(requestBody);
            var response = await HandleErrorResponseIfNull(participantData, req);
            if (response != null) return response;

            var serviceProvider = await _CohortDistributionHelper.AllocateServiceProviderAsync(requestBody.NhsNumber, participantData.ScreeningAcronym, participantData.Postcode);
            response = await HandleErrorResponseIfNull(serviceProvider, req);
            if (response != null) return response;

            var mostRecentCohortDistributionParticipant = await _CohortDistributionHelper.GetLastCohortDistributionRecord(requestBody);
            response = await HandleErrorResponseIfNull(mostRecentCohortDistributionParticipant, req);
            if (response != null) return response;

            var transformedParticipant = await _CohortDistributionHelper.TransformParticipantAsync(serviceProvider, participantData);
            response = await HandleErrorResponseIfNull(transformedParticipant, req);
            if (response != null) return response;

            var cohortAddResponse = await AddCohortDistribution(transformedParticipant);
            if (cohortAddResponse.StatusCode != HttpStatusCode.OK)
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
            }

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
        }
        catch (Exception ex)
        {
            _logger.LogError("One of the functions failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, requestBody.NhsNumber, requestBody.FileName);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
    }

    private async Task<HttpResponseData> HandleErrorResponseIfNull(object obj, HttpRequestData req)
    {
        if (obj == null)
        {
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
        return null;
    }

    private async Task<HttpWebResponse> AddCohortDistribution(CohortDistributionParticipant transformedParticipant)
    {
        var json = JsonSerializer.Serialize(transformedParticipant);
        var response = await _callFunction.SendPost(Environment.GetEnvironmentVariable("AddCohortDistributionURL"), json);

        _logger.LogInformation("Called add cohort distribution function");
        return response;
    }
}
