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

    public CreateCohortDistribution(ICreateResponse createResponse, ILogger<CreateCohortDistribution> logger, ICallFunction callFunction)
    {
        _createResponse = createResponse;
        _logger = logger;
        _callFunction = callFunction;
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
            var participantData = await RetrieveParticipantData(requestBody);
            var serviceProvider = await AllocateServiceProvider(requestBody, participantData);
            var transformedParticipant = await TransformParticipant(requestBody, serviceProvider, participantData);
            await AddCohortDistribution(transformedParticipant);
        }
        catch (Exception ex)
        {
            _logger.LogError("One of the functions failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }

        return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
    }

    private async Task<CohortDistributionParticipant> RetrieveParticipantData(CreateCohortDistributionRequestBody requestBody)
    {
        var retrieveParticipantRequestBody = new RetrieveParticipantRequestBody()
        {
            NhsNumber = requestBody.NhsNumber,
            ScreeningService = requestBody.ScreeningService
        };

        var json = JsonSerializer.Serialize(retrieveParticipantRequestBody);
        var response = await _callFunction.SendPost(Environment.GetEnvironmentVariable("RetrieveParticipantDataURL"), json);
        _logger.LogInformation("Called retrieve participant data service");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                var body = await reader.ReadToEndAsync();
                CohortDistributionParticipant result = JsonSerializer.Deserialize<CohortDistributionParticipant>(body);
                return result;
            }
        }
        else
        {
            string logMessage = "Retrieve participant data service function failed.";
            _logger.LogError(logMessage);
            throw new Exception(logMessage);
        }
    }

    private async Task<string> AllocateServiceProvider(CreateCohortDistributionRequestBody requestBody, CohortDistributionParticipant participantData)
    {
        var allocationConfigRequestBody = new AllocationConfigRequestBody
        {
            NhsNumber = requestBody.NhsNumber,
            Postcode = participantData.Postcode,
            ScreeningService = requestBody.ScreeningService
        };

        var json = JsonSerializer.Serialize(allocationConfigRequestBody);
        var response = await _callFunction.SendPost(Environment.GetEnvironmentVariable("AllocateScreeningProviderURL"), json);
        _logger.LogInformation("Called allocate screening provider service");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                var body = await reader.ReadToEndAsync();
                return body;
            }
        }
        else
        {
            string logMessage = "Allocate service provider function failed.";
            _logger.LogError(logMessage);
            throw new Exception(logMessage);
        }
    }

    private async Task<CohortDistributionParticipant> TransformParticipant(CreateCohortDistributionRequestBody requestBody, string serviceProvider, CohortDistributionParticipant participantData)
    {
        var transformDataRequestBody = new TransformDataRequestBody()
        {
            Participant = participantData,
            ServiceProvider = serviceProvider
        };

        var json = JsonSerializer.Serialize(transformDataRequestBody);
        var response = await _callFunction.SendPost(Environment.GetEnvironmentVariable("TransformDataServiceURL"), json);
        _logger.LogInformation("Called transform data service");

        if (response.StatusCode == HttpStatusCode.OK)
        {

            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                var body = await reader.ReadToEndAsync();
                CohortDistributionParticipant result = JsonSerializer.Deserialize<CohortDistributionParticipant>(body);
                return result;
            }
        }
        else
        {
            string logMessage = "Transform participant function failed.";
            _logger.LogError(logMessage);
            throw new Exception(logMessage);
        }
    }

    private async Task AddCohortDistribution(CohortDistributionParticipant transformedParticipant)
    {
        var json = JsonSerializer.Serialize(transformedParticipant);
        var response = await _callFunction.SendPost(Environment.GetEnvironmentVariable("AddCohortDistributionURL"), json);
        _logger.LogInformation("Called add cohort distribution function");

        if (response.StatusCode != HttpStatusCode.OK)
        {
            string logMessage = "Add cohort distribution function failed.";
            _logger.LogError(logMessage);
            throw new Exception(logMessage);
        }
    }
}
