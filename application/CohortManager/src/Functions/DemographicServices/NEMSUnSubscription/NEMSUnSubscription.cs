namespace NHS.CohortManager.DemographicServices.NEMSUnSubscription;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Azure.Data.Tables;
using Azure;
using Model;
using Common;
using Microsoft.Extensions.Options;
using NHS.Screening.NEMSUnSubscription;

public class NEMSUnSubscription
{
    protected readonly TableClient _tableClient;
    protected readonly HttpClient _httpClient;

    private readonly ILogger<NEMSUnSubscription> _logger;

    private readonly ICreateResponse _createResponse;

    private readonly INemsSubscriptionService _nemsSubscriptionService;

    public NEMSUnSubscription(ILogger<NEMSUnSubscription> logger,
    //IDataServiceClient<NemsSubscription> nemsSubscriptionClient, /* To Do Later */
    IHttpClientFactory httpClientFactory,
    ICreateResponse createResponse,
    INemsSubscriptionService nemsSubscriptionService)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
       
        _createResponse = createResponse;
       
        _nemsSubscriptionService = nemsSubscriptionService;

    }
    
    // Constructor for dependency injection (testability)
    public NEMSUnSubscription(TableClient tableClient, HttpClient httpClient)
    {
        _tableClient = tableClient;
        _httpClient = httpClient;
    }

    [Function("NEMSUnsubscribe")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("Received unsubscribe request");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(requestBody))
        {
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Request body is empty.");
        }

        var request = JsonSerializer.Deserialize<UnsubscriptionRequest>(requestBody);

        if (request == null || string.IsNullOrEmpty(request.NhsNumber))
        {
            _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Request body is empty.");
        }

        var nhsNumber = request!.NhsNumber;
        var subscriptionId = await _nemsSubscriptionService.LookupSubscriptionIdAsync(nhsNumber);

        if (string.IsNullOrEmpty(subscriptionId))
        {
            _logger.LogWarning("No subscription record found.");

            return _createResponse.CreateHttpResponse(HttpStatusCode.NotFound, req, "No subscription record found.");
        }

        var isDeletedFromNems = await _nemsSubscriptionService.DeleteSubscriptionFromNems(subscriptionId);

        if (!isDeletedFromNems)
        {
            _logger.LogError("Failed to delete subscription from NEMS.");
            _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, "Failed to delete subscription from NEMS.");
        }

        var unsubscribed = await _nemsSubscriptionService.DeleteSubscriptionFromTableAsync(nhsNumber);
        if (!unsubscribed)
        {
            _logger.LogError("Failed to unsubscribe from NEMS.");
            _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, "Failed to unsubscribe from NEMS.");
        }

        _logger.LogInformation("Subscription deleted successfully.");
        return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, "Successfully unsubscribed.");

    }
}
