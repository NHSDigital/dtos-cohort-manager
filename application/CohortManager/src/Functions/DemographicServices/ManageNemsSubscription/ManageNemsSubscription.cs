namespace NHS.CohortManager.DemographicServices;

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Options;
using Model;
using Common;
using DataServices.Client;
using Azure.Data.Tables;
using DataServices.Core;
using Azure;

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
    /// <param name="NhsNumber">
    /// The NHS number for the participant
    /// </param>
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

            // Create subscription object
            Subscription subscription = _subscriptionManager.CreateSubscription(nhsNumber);
            string subscriptionJson = new FhirJsonSerializer().SerializeToString(subscription);

            // Post to NEMS FHIR endpoint
            Guid subscriptionId = await _subscriptionManager.SendSubscriptionToNemsAsync(subscriptionJson);

            if (subscriptionId == Guid.Empty)
            {
                _logger.LogError("Failed to create subscription in NEMS.");
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, "Failed to create subscription in NEMS.");
            }

            // Store in SQL Database
            bool storageSuccess = await _subscriptionManager.SaveSubscriptionInDatabase(nhsNumber, subscriptionId);
            if (!storageSuccess)
            {
                _logger.LogError("Subscription created but failed to store locally.");
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, "Subscription created but failed to store locally.");
            }

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, subscriptionId.ToString());
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError(ex, "Missing required parameter NHS Number");
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create NEMS subscription");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }

    /// <summary>
    /// Function that creates a subscription in the National Events Management Service (NEMS)
    /// for a participant, identified by their NHS number.
    /// </summary>
    /// <param name="NhsNumber">
    /// The NHS number for the participant
    /// </param>
    /// <returns>
    /// An <see cref="HttpResponseData"/> indicating success (200 OK) with the subscription ID,
    /// or failure with the appropriate HTTP status code and error message.
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
                throw new ArgumentNullException("NHS number is required.");
            }

            string? subscriptionId = await _subscriptionManager.LookupSubscriptionIdAsync(nhsNumber);

            if (string.IsNullOrEmpty(subscriptionId))
            {
                _logger.LogWarning("No subscription record found.");
                return _createResponse.CreateHttpResponse(HttpStatusCode.NotFound, req, "No subscription record found.");
            }

            bool deleted = await _subscriptionManager.DeleteSubscriptionFromNemsAsync(subscriptionId);
            if (!deleted)
            {
                _logger.LogError("Failed to delete subscription from NEMS.");
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, "Failed to delete subscription from NEMS.");
            }

            var unsubscribed = await _subscriptionManager.DeleteSubscriptionFromDatabaseAsync(nhsNumber);
            if (!unsubscribed)
            {
                _logger.LogError("Failed to unsubscribe from NEMS.");
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, "Failed to unsubscribe from NEMS.");
            }

            _logger.LogInformation("Successfully unsubscribed.");
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, "Successfully unsubscribed.");
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError(ex, "Missing required parameter in NEMS unsubscription workflow: {Message}", ex.Message);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe from NEMS");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }

    /// <summary>
    /// Data serivce for the NEMS Subscription table.
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
