namespace NHS.CohortManager.DemographicServices;

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;
using Common;
using DataServices.Core;

public class ManageNemsSubscription
{
    private readonly ILogger<ManageNemsSubscription> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly NemsSubscriptionManager _subscriptionManager;
    private readonly IRequestHandler<NemsSubscription> _requestHandler;

    public ManageNemsSubscription
    (
        ILogger<ManageNemsSubscription> logger,
        ICreateResponse createResponse,
        NemsSubscriptionManager subscriptionManager,
        IRequestHandler<NemsSubscription> requestHandler
    )
    {
        _logger = logger;
        _createResponse = createResponse;
        _subscriptionManager = subscriptionManager;
        _requestHandler = requestHandler;
    }

    /// <summary>
    /// Function that creates a subscription in the National Events Management Service (NEMS)
    /// for a participant, identified by their NHS number.
    /// </summary>
    /// <param name="req">HTTP request containing NHS number as query parameter</param>
    /// <returns>
    /// An <see cref="HttpResponseData"/> indicating success (200 OK) with the subscription ID,
    /// or failure with the appropriate HTTP status code and error message.
    /// </returns>
    [Function("Subscribe")]
    public async Task<HttpResponseData> Subscribe([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("Received subscribe request");

            string? nhsNumber = req.Query["nhsNumber"];

            if (nhsNumber != null && !ValidationHelper.ValidateNHSNumber(nhsNumber))
            {
                _logger.LogError("NHS number is required and must be valid format");
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "NHS number is required and must be valid format.");
            }

            bool success = nhsNumber != null ? await _subscriptionManager.CreateAndSendSubscriptionAsync(nhsNumber) : false;

            if (!success)
            {
                _logger.LogError("Failed to create subscription for NHS number REDACTED");
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, "Failed to create subscription in NEMS.");
            }

            string? subscriptionId = nhsNumber != null ? await _subscriptionManager.LookupSubscriptionIdAsync(nhsNumber) : null;
            _logger.LogInformation("Successfully created subscription for NHS number REDACTED");
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, $"Subscription created successfully. Subscription ID: {subscriptionId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create NEMS subscription");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, "An error occurred while creating the subscription.");
        }
    }

    /// <summary>
    /// Function that removes a subscription from the National Events Management Service (NEMS)
    /// for a participant, identified by their NHS number.
    /// </summary>
    /// <param name="req">HTTP request containing NHS number as query parameter</param>
    /// <returns>
    /// An <see cref="HttpResponseData"/> indicating success (200 OK) or failure with the appropriate HTTP status code and error message.
    /// </returns>
    [Function("Unsubscribe")]
    public async Task<HttpResponseData> Unsubscribe([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("Received unsubscribe request");

            string? nhsNumber = req.Query["nhsNumber"];

            if (nhsNumber != null && !ValidationHelper.ValidateNHSNumber(nhsNumber))
            {
                _logger.LogError("NHS number is required and must be valid format");
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "NHS number is required and must be valid format.");
            }

            // Check existence first to provide more informative error handling
            var subscriptionId =  nhsNumber != null ? await _subscriptionManager.LookupSubscriptionIdAsync(nhsNumber) : null;

            if (string.IsNullOrEmpty(subscriptionId))
            {
                _logger.LogWarning("No subscription found for NHS number");
                return _createResponse.CreateHttpResponse(HttpStatusCode.NotFound, req, "No subscription found.");
            }

            // Attempt to remove only if found
            bool success = nhsNumber != null ? await _subscriptionManager.RemoveSubscriptionAsync(nhsNumber) : false;

            if (!success)
            {
                _logger.LogError("Failed to remove subscription for NHS number");
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, "Failed to remove subscription.");
            }

            _logger.LogInformation("Successfully unsubscribed NHS number REDACTED");
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, "Successfully unsubscribed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe from NEMS");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, "An error occurred while removing the subscription.");
        }
    }


    /// <summary>
    /// Function to check the status of a NEMS subscription for a given NHS number
    /// </summary>
    /// <param name="req">HTTP request containing NHS number as query parameter</param>
    /// <returns>
    /// An <see cref="HttpResponseData"/> with subscription details or not found message
    /// </returns>
    [Function("CheckSubscriptionStatus")]
    public async Task<HttpResponseData> CheckSubscriptionStatus([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("Received check subscription request");

            string? nhsNumber = req.Query["nhsNumber"];

            if (nhsNumber != null && !ValidationHelper.ValidateNHSNumber(nhsNumber))
            {
                _logger.LogError("NHS number is required and must be valid format");
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "NHS number is required and must be valid format.");
            }

            string? subscriptionId =  nhsNumber != null ? await _subscriptionManager.LookupSubscriptionIdAsync(nhsNumber) : null;

            if (string.IsNullOrEmpty(subscriptionId))
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.NotFound, req, "No subscription found for this NHS number.");
            }

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, $"Active subscription found. Subscription ID: {subscriptionId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking subscription status");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, "An error occurred while checking subscription status.");
        }
    }

    /// <summary>
    /// Data service for the NEMS Subscription table.
    /// </summary>
    [Function("NemsSubscriptionDataService")]
    public async Task<HttpResponseData> NemsSubscriptionDataService([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = "NemsSubscriptionDataService/{*key}")] HttpRequestData req, string? key)
    {
        try
        {
            _logger.LogInformation("DataService Request Received Method: {Method}, DataObject {DataType} ", req.Method, typeof(NemsSubscription));
            var result = await _requestHandler.HandleRequest(req, key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error has occurred ");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, $"An error has occurred {ex.Message}");
        }
    }

}
