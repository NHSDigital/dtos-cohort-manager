namespace NHS.CohortManager.DemographicServices;

using System;
using System.Linq;
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

            if (string.IsNullOrEmpty(nhsNumber))
            {
                _logger.LogError("NHS number is required.");
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "NHS number is required.");
            }

            // Validate NHS number format (basic validation)
            if (!IsValidNhsNumber(nhsNumber))
            {
                _logger.LogError("Invalid NHS number format: {NhsNumber}", nhsNumber);
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Invalid NHS number format.");
            }

            bool success = await _subscriptionManager.CreateAndSendSubscriptionAsync(nhsNumber);

            if (success)
            {
                // Get the subscription ID that was created
                string? subscriptionId = await _subscriptionManager.LookupSubscriptionIdAsync(nhsNumber);

                _logger.LogInformation("Successfully created subscription for NHS number {NhsNumber}", nhsNumber);
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, $"Subscription created successfully. Subscription ID: {subscriptionId}");
            }
            else
            {
                _logger.LogError("Failed to create subscription for NHS number {NhsNumber}", nhsNumber);
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, "Failed to create subscription in NEMS.");
            }
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

            if (string.IsNullOrEmpty(nhsNumber))
            {
                _logger.LogError("NHS number is required.");
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "NHS number is required.");
            }

            if (!IsValidNhsNumber(nhsNumber))
            {
                _logger.LogError("Invalid NHS number format: {NhsNumber}", nhsNumber);
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Invalid NHS number format.");
            }

            bool success = await _subscriptionManager.RemoveSubscriptionAsync(nhsNumber);

            if (success)
            {
                _logger.LogInformation("Successfully unsubscribed NHS number {NhsNumber}", nhsNumber);
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, "Successfully unsubscribed");
            }
            else
            {
                _logger.LogWarning("No subscription found or failed to remove for NHS number {NhsNumber}", nhsNumber);
                return _createResponse.CreateHttpResponse(HttpStatusCode.NotFound, req, "No subscription found or failed to remove subscription.");
            }
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
    [Function("CheckSubscription")]
    public async Task<HttpResponseData> CheckSubscription([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("Received check subscription request");

            string? nhsNumber = req.Query["nhsNumber"];

            if (string.IsNullOrEmpty(nhsNumber))
            {
                _logger.LogError("NHS number is required.");
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "NHS number is required.");
            }

            // Validate NHS number format
            if (!IsValidNhsNumber(nhsNumber))
            {
                _logger.LogError("Invalid NHS number format: {NhsNumber}", nhsNumber);
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Invalid NHS number format.");
            }

            string? subscriptionId = await _subscriptionManager.LookupSubscriptionIdAsync(nhsNumber);

            if (!string.IsNullOrEmpty(subscriptionId))
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, $"Active subscription found. Subscription ID: {subscriptionId}");
            }
            else
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.NotFound, req, "No subscription found for this NHS number.");
            }
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

    /// <summary>
    /// Basic NHS number validation
    /// </summary>
    /// <param name="nhsNumber">NHS number to validate</param>
    /// <returns>True if valid format, false otherwise</returns>
    private static bool IsValidNhsNumber(string nhsNumber)
    {
        if (string.IsNullOrEmpty(nhsNumber))
            return false;

        // Remove spaces and check if it's exactly 10 digits
        var cleanNumber = nhsNumber.Replace(" ", "");
        return cleanNumber.Length == 10 && cleanNumber.All(char.IsDigit);
    }
}
