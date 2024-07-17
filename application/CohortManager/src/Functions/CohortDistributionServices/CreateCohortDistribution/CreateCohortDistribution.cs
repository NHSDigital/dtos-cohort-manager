namespace NHS.CohortManager.CohortDistributionService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Common;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NHS.CohortManager.CohortDistribution;
using System.Text;
using Common.Interfaces;

public class CreateCohortDistribution
{
    private readonly ICreateResponse _createResponse;
    private readonly ILogger<CreateCohortDistribution> _logger;
    private readonly ICreateCohortDistributionData _createCohortDistributionData;

    public CreateCohortDistribution(ICreateResponse createResponse, ILogger<CreateCohortDistribution> logger, ICreateCohortDistributionData createCohortDistributionData)
    {
        _createResponse = createResponse;
        _logger = logger;
        _createCohortDistributionData = createCohortDistributionData;
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

        var screeningService = requestBody.ScreeningService;
        var nhsNumber = requestBody.NhsNumber;

        try
        {
            var participantData = _createCohortDistributionData.GetCohortParticipant(nhsNumber);
            var serviceProvider = await _createCohortDistributionData.AllocateCohortParticipantServiceProvider(participantData, screeningService);
            var transformedParticipant = await _createCohortDistributionData.TransformCohortParticipant(participantData, serviceProvider);
            // call add cohort distribution service

            Console.WriteLine(serviceProvider);
            Console.WriteLine(JsonSerializer.Serialize(transformedParticipant));
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
        }
        catch (Exception ex)
        {
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }

    }
}
