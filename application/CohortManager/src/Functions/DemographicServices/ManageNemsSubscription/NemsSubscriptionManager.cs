namespace NHS.CohortManager.DemographicServices;

using System;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using DataServices.Client;
using Microsoft.Extensions.Options;
using Model;
using Hl7.Fhir.Model;
using Common;
using DataServices.Core;

public class NemsSubscriptionManager
{
    private readonly IHttpClientFunction _httpClient;
    private readonly ILogger<NemsSubscriptionManager> _logger;
    private readonly ManageNemsSubscriptionConfig _config;
    private readonly IDataServiceAccessor<NemsSubscription> _nemsSubscriptionAccessor;

    public NemsSubscriptionManager(
        IHttpClientFunction httpClient,
        ManageNemsSubscriptionConfig config,
        ILogger<NemsSubscriptionManager> logger,
        IDataServiceAccessor<NemsSubscription> nemsSubscriptionAccessor)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
        _nemsSubscriptionAccessor = nemsSubscriptionAccessor;
    }

    /// <summary>
    /// Looks up the subscription ID for a given NHS number in the database
    /// </summary> 
    /// <param name="nhsNumber">The NHS number to look up.</param>
    /// <returns>
    /// The subscription ID if found, otherwise null.
    /// </returns>
    /// <remarks>
    /// WIP as there will be changes to this method after we are onboarded to the NEMS API.
    /// </remarks>
    public async Task <string?> LookupSubscriptionIdAsync(string nhsNumber)
    {
        try
        {
            var subscription = await _nemsSubscriptionAccessor.GetSingle(i => i.NhsNumber == long.Parse(nhsNumber));
            if (subscription != null)
            {
                return subscription.SubscriptionId.ToString();
            }

            return null;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to lookup subscription ID");
            return null;
        }
    }

    /// <summary>
    /// Deletes a subscription from the NEMS API using the provided subscription ID.
    /// </summary> 
    /// <param name="subscriptionId">The subscription ID to delete.</param>
    /// <returns>
    /// The subscription ID if found, otherwise null.
    /// </returns>
    /// <remarks>
    /// WIP as there will be changes to this method after we are onboarded to the NEMS API.
    /// </remarks>
    public async Task<bool> DeleteSubscriptionFromNemsAsync(string subscriptionId)
    {
        try
        {
            string nemsEndpoint = _config.NemsDeleteEndpoint;
            bool isSuccess = await _httpClient.SendDelete($"{nemsEndpoint}/{subscriptionId}");
            return isSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete subscription ID {SubscriptionId}", subscriptionId);
            return false;
        }
    }

    /// <summary>
    /// Sends the given subscpription to NEMS
    /// </summary> 
    /// <param name="subscription">The serialised subsciption object</param>
    /// <returns>
    /// The subscription ID if found, otherwise null.
    /// </returns>
    /// <remarks>
    /// WIP as there will be changes to this method after we are onboarded to the NEMS API.
    /// </remarks>
    public async Task<Guid> SendSubscriptionToNemsAsync(string subscription)
    {
        try
        {
            var url = string.Format("{0}/{1}", _config.NemsFhirEndpoint, "Subscription");
            var response = await _httpClient.SendNemsPost(
                url,
                subscription,
                _config.SpineAccessToken,
                _config.FromAsid,
                _config.ToAsid
            );

            response.EnsureSuccessStatusCode();
            Guid subscriptionId = new();
            _logger.LogInformation("Sent subscription to NEMS with ID: {SubscriptionId}", subscriptionId);
            
            return subscriptionId;
        }
        catch (Exception ex)
        {
            return Guid.Empty;
        }
    }

    /// <summary>
    /// Deletes a subscription from the NEMS subscriptions table in the cohort manager database.
    /// </summary>
    /// <param name="nhsNumber">The NHS number associated with the subscription to delete.</param>
    /// <returns>
    /// A boolean indicating whether the deletion was successful.
    /// </returns>
    public async Task<bool> DeleteSubscriptionFromDatabaseAsync(string nhsNumberStr)
    {
        try
        {
            _logger.LogInformation("Deleting subscription from database");
            long nhsNumber = long.Parse(nhsNumberStr);
            bool deleted = await _nemsSubscriptionAccessor.Remove(i => i.NhsNumber == nhsNumber);

            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while deleting the subscription}");
            return false;
        }
    }

    /// <summary>
    /// Saves a subscription in the NEMS subscriptions table in the cohort manager database.    
    /// </summary>
    /// <param name="nhsNumber">The NHS number associated with the subscription.</param>
    /// <param name="subscriptionId">The subscription ID to save.</param>
    /// <returns>
    /// A boolean indicating whether the subscription was successfully saved.
    /// </returns>
    /// <remarks>
    /// WIP as there will be changes to this method after we are onboarded to the NEMS API.
    /// </remarks>
    public async Task<bool> SaveSubscriptionInDatabase(string nhsNumber, Guid subscriptionId)
    {
        try
        {
            _logger.LogInformation("Start saving the SubscriptionId in the database.");
            var subscription = new NemsSubscription
            {
                SubscriptionId = subscriptionId, //WIP , might change after onboarding as datatype might change
                NhsNumber = Convert.ToInt64(nhsNumber), //WIP , might change after onboarding as datatype might change
                RecordInsertDateTime = DateTime.UtcNow
            };
            bool subscriptionCreated = await _nemsSubscriptionAccessor.InsertSingle(subscription);

            return subscriptionCreated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while saving the subscription in the database");
            return false;
        }
    }

    /// <summary>
    /// Creates a new subscription object for the NEMS API.
    /// </summary>
    /// <param name="nhsNumber">The NHS number to create the subscription for.</param>
    /// <returns>
    /// <see cref="Subscription"/> A subscription object
    /// </returns>
    /// <remarks>
    /// WIP as there will be changes to this method after we are onboarded to the NEMS API.
    /// </remarks>
    public Subscription CreateSubscription(string nhsNumber)
    {
        /* This is a WIP as additional work is required to use the NEMS endpoint after onboarding to NemsApi hub. */
        var subscription = new Subscription
        {
            Meta = new Meta
            {
                //Profile = new[] { "https://fhir.nhs.uk/StructureDefinition/EMS-Subscription-1" }, // WIP, Will remove this after onboarding
                Profile = new[] { _config.SubscriptionProfile },
                LastUpdated = DateTimeOffset.UtcNow
            },
            Status = Subscription.SubscriptionStatus.Requested,
            Reason = "NEMS event notification subscription",
            //Criteria = $"Patient?identifier=https://fhir.nhs.uk/Id/nhs-number|{nhsNumber}", // WIP, Will remove this after onboarding
            Criteria = $"Patient?identifier={_config.SubscriptionCriteria}|{nhsNumber}",
            Channel = new Subscription.ChannelComponent
            {
                Type = Subscription.SubscriptionChannelType.RestHook,
                Endpoint = _config.CallbackEndpoint,
                Payload = "application/fhir+json",
                Header = new[] {
                    $"Authorization: Bearer {_config.CallAuthToken}",
                    "X-Correlation-ID: " + Guid.NewGuid().ToString()
                }
            }
        };

        return subscription;
    }
}