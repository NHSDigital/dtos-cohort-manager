namespace NHS.CohortManager.DemographicServices;

using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Rest;
using Microsoft.Extensions.Options;
using Model;
using Common;
using DataServices.Client;
using Common.Interfaces;
using Azure.Data.Tables;

public class ManageNemsSubscription
{
    private readonly ILogger<ManageNemsSubscription> _logger;
    private readonly IHttpClientFunction _httpClientFunction;
    private readonly ICreateResponse _createResponse;
    private readonly ManageNemsSubscriptionConfig _config;
    private readonly IDataServiceClient<NemsSubscription> _nemsSubscriptionClient;
    private readonly TableClient _tableClient;
    private readonly NemsSubscriptionManager _subscriptionManager;
    private const string urlFormat = "{0}/{1}";

    public ManageNemsSubscription
    (
        ILogger<ManageNemsSubscription> logger,
        IDataServiceClient<NemsSubscription> nemsSubscriptionClient,
        TableClient tableClient,
        IHttpClientFunction httpClientFunction,
        ICreateResponse createResponse,
        IOptions<ManageNemsSubscriptionConfig> nemsSubscribeConfig
    )
    {
        _logger = logger;
        _nemsSubscriptionClient = nemsSubscriptionClient;
        _tableClient = tableClient;
        _httpClientFunction = httpClientFunction;
        _createResponse = createResponse;
        _config = nemsSubscribeConfig.Value;
        _subscriptionManager = new NemsSubscriptionManager(
            _tableClient,
            httpClientFunction,
            logger,
            nemsSubscribeConfig,
            nemsSubscriptionClient);
    }

    /// <summary>
    /// Azure Function that processes a NEMS subscription request by
    /// verifying the participant in PDS, creating a FHIR subscription resource, posting it to NEMS,
    /// and attempting to store the subscription locally in Sql-Server.
    /// </summary>
    /// <param name="req">
    /// The HTTP request data containing a JSON payload with the NHS number to subscribe.
    /// </param>
    /// <returns>
    /// An <see cref="HttpResponseData"/> indicating success (200 OK) with the subscription ID,
    /// or failure with the appropriate HTTP status code and error message.
    /// </returns>
    [Function("NEMSSubscribe")]
    public async Task<HttpResponseData> Subscribe([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "subscriptions/nems")] HttpRequestData req)
    {
        try
        {
            string nhsNumber = req.Query["nhsNumber"]
                ?? throw new ArgumentNullException("NHS number is required.");

            // 1. Create Subscription Resource
            Subscription subscription = CreateNemsSubscriptionResource(nhsNumber);

            // 2. Post to NEMS FHIR endpoint
            string subscriptionId = await PostSubscriptionToNems(subscription);
            if (string.IsNullOrEmpty(subscriptionId))
            {
                _logger.LogError("Failed to create subscription in NEMS.");
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, "Failed to create subscription in NEMS.");
            }

            // 3. Store in SQL Database
            bool storageSuccess = await StoreSubscriptionInDatabase(nhsNumber, subscriptionId);
            if (!storageSuccess)
            {
                _logger.LogError("Subscription created but failed to store locally.");
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, "Subscription created but failed to store locally.");
            }
            _logger.LogInformation("Successfully created the subscription");

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, subscriptionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in NEMS subscription workflow: {Message}", ex.Message);
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }

    [Function("NEMSUnsubscribe")]
    public async Task<HttpResponseData> Unsubscribe([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("Received unsubscribe request");

        string nhsNumber = req.Query["nhsNumber"] ?? throw new ArgumentNullException("NHS number is required.");

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
