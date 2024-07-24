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

        var screeningService = requestBody.ScreeningService;
        var nhsNumber = requestBody.NhsNumber;

        CohortDistributionParticipant participantData = new CohortDistributionParticipant();
        string serviceProvider;
        CohortDistributionParticipant transformedParticipant = new CohortDistributionParticipant();

        // Retrieve Participant
        try
        {
            var retrieveParticipantRequestBody = new RetrieveParticipantRequestBody()
            {
                NhsNumber = nhsNumber,
                ScreeningService = "1"
            };

            var json = JsonSerializer.Serialize(retrieveParticipantRequestBody);
            var response = await _callFunction.SendPost(Environment.GetEnvironmentVariable("RetrieveParticipantDataURL"), json);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation("Called retrieve participant data service");

                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    var body = await reader.ReadToEndAsync();
                    CohortDistributionParticipant result = JsonSerializer.Deserialize<CohortDistributionParticipant>(body);
                    participantData = result;
                }
            }
            else return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
        catch (Exception ex)
        {
            _logger.LogError("Retrieve participant data service function failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }

        // Allocate Screening Provider
        try
        {
            var allocationConfigRequestBody = new AllocationConfigRequestBody
            {
                NhsNumber = nhsNumber,
                Postcode = participantData.Postcode,
                ScreeningService = screeningService
            };

            var json = JsonSerializer.Serialize(allocationConfigRequestBody);
            var response = await _callFunction.SendPost(Environment.GetEnvironmentVariable("AllocateScreeningProviderURL"), json);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation("Called allocate screening provider service");

                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    var body = await reader.ReadToEndAsync();
                    serviceProvider = body;
                }
            }
            else return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);

        }
        catch (Exception ex)
        {
            _logger.LogError("Allocate screening provider service function failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }

        // Transform Participant
        try
        {
            var transformDataRequestBody = new TransformDataRequestBody()
            {
                Participant = participantData,
                ScreeningService = "1"
            };

            var json = JsonSerializer.Serialize(transformDataRequestBody);
            var response = await _callFunction.SendPost(Environment.GetEnvironmentVariable("TransformDataServiceURL"), json);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation("Called transform data service");

                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    var body = await reader.ReadToEndAsync();
                    CohortDistributionParticipant result = JsonSerializer.Deserialize<CohortDistributionParticipant>(body);
                    transformedParticipant = result;
                }
            }
            else return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
        catch (Exception ex)
        {
            _logger.LogError("Transform data service function failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }

        // Add Cohort Distribution
        try
        {
            var json = JsonSerializer.Serialize(transformedParticipant);
            var response = await _callFunction.SendPost(Environment.GetEnvironmentVariable("AddCohortDistributionURL"), json);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation("Called add cohort distribution function");
            }
            else return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
        catch (Exception ex)
        {
            _logger.LogError("Add cohort distribution function failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }

        return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
    }
}
