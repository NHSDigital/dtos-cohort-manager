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

    public CreateCohortDistribution(ICreateResponse createResponse, ILogger<CreateCohortDistribution> logger, ICallFunction callFunction, ICohortDistributionHelper CohortDistributionHelper)
    {
        _createResponse = createResponse;
        _logger = logger;
        _callFunction = callFunction;
        _CohortDistributionHelper = CohortDistributionHelper;
    }

    [Function("CreateCohortDistribution")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        CreateCohortDistributionRequestBody requestBody;
        try
        {
            string requestBodyJson;
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBodyJson = reader.ReadToEnd();
            }

            requestBody = JsonSerializer.Deserialize<CreateCohortDistributionRequestBody>(requestBodyJson);
        }
        catch
        {
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
            if (participantData == null)
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
            }

            var serviceProvider = await _CohortDistributionHelper.AllocateServiceProviderAsync(requestBody, participantData.Postcode);

            if (serviceProvider == null)
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
            }

            var transformedParticipant = await _CohortDistributionHelper.TransformParticipantAsync(serviceProvider, participantData);
            await AddCohortDistribution(transformedParticipant);
        }
        catch (Exception ex)
        {
            _logger.LogError("One of the functions failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }

        return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
    }

    private async Task AddCohortDistribution(CohortDistributionParticipant transformedParticipant)
    {
        var json = JsonSerializer.Serialize(transformedParticipant);
        await _callFunction.SendPost(Environment.GetEnvironmentVariable("AddCohortDistributionURL"), json);
        _logger.LogInformation("Called add cohort distribution function");
    }
}
