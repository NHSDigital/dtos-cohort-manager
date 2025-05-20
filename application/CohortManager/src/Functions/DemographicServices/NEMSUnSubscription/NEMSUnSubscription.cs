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

    private readonly INemsSubscriptionService _nemsSubscriptionService;


    protected virtual async Task<HttpResponseData> HandleNotFoundAsync(HttpRequestData req, string message)
    {
        var response = req.CreateResponse(HttpStatusCode.NotFound);
        await response.WriteStringAsync(message);
        return response;
    }

    public NEMSUnSubscription(ILogger<NEMSUnSubscription> logger,
    //IDataServiceClient<NemsSubscription> nemsSubscriptionClient, /* To Do Later */
    IHttpClientFactory httpClientFactory,
    IOptions<NEMSUnSubscriptionConfig> nemsUnSubscriptionConfig,
    INemsSubscriptionService nemsSubscriptionService)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _nemsSubscriptionService = nemsSubscriptionService;

    }
    // Constructor for dependency injection (testability)
    public NEMSUnSubscription(ILogger<NEMSUnSubscription> logger, NemsSubscriptionService nemsSubscriptionService, TableClient tableClient, HttpClient httpClient)
    {
        _logger = logger;
        _nemsSubscriptionService = nemsSubscriptionService;
        _tableClient = tableClient;
        _httpClient = httpClient;
    }

    [Function("NEMSUnsubscribe")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogInformation("Received unsubscribe request");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(requestBody))
        {
            var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequestResponse.WriteStringAsync("Request body is empty.");
            return badRequestResponse;
        }

        var request = JsonSerializer.Deserialize<UnsubscriptionRequest>(requestBody);

        if (request == null || string.IsNullOrEmpty(request.NhsNumber))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync("Invalid or missing NHS number.");
            return badRequest;
        }

        string nhsNumber = request.NhsNumber;
        string? subscriptionId = await _nemsSubscriptionService.LookupSubscriptionIdAsync(nhsNumber);

        if (string.IsNullOrEmpty(subscriptionId))
        {
            _logger.LogWarning("No subscription record found.");
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteStringAsync("No subscription record found.");
            return notFoundResponse;
        }

        bool isDeletedFromNems = await _nemsSubscriptionService.DeleteSubscriptionFromNems(subscriptionId);

        if (!isDeletedFromNems)
        {
            _logger.LogError("Failed to delete subscription from NEMS.");
            var badGatewayResponse = req.CreateResponse(HttpStatusCode.BadGateway);
            await badGatewayResponse.WriteStringAsync("Failed to delete subscription from NEMS.");
            return badGatewayResponse;
        }

        await _nemsSubscriptionService.DeleteSubscriptionFromTableAsync(nhsNumber);
        _logger.LogInformation("Subscription deleted successfully.");

        var successResponse = req.CreateResponse(HttpStatusCode.OK);
        await successResponse.WriteStringAsync("Successfully unsubscribed.");
        return successResponse;
    }
}
