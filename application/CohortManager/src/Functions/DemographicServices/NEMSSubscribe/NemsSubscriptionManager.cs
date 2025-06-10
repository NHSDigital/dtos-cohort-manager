namespace NHS.CohortManager.DemographicServices;

using System;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using DataServices.Client;
using Microsoft.Extensions.Options;
using Model;
using Hl7.Fhir.Model;

public class NemsSubscriptionManager
{
    private readonly TableClient _tableClient;
    private readonly HttpClient _httpClient;
    private readonly ILogger<NemsSubscriptionService> _logger;
    private readonly NEMSUnSubscriptionConfig _config;
    private readonly IDataServiceClient<NemsSubscription> _nemsSubscriptionClient;

    public NemsSubscriptionManager(
        TableClient tableClient,
        HttpClient httpClient,
        IOptions<NEMSUnSubscriptionConfig> nemsUnSubscriptionConfig,
        ILogger<NemsSubscriptionService> logger,
        IDataServiceClient<NemsSubscription> nemsSubscriptionClient)
    {
        _tableClient = tableClient;
        _httpClient = httpClient;
        _config = nemsUnSubscriptionConfig.Value;
        _logger = logger;
        _nemsSubscriptionClient = nemsSubscriptionClient;
    }

    //WIP as there would be changes in the method after we gets proper NEMS API endpoints
    public async Task<string?> LookupSubscriptionIdAsync(string nhsNumber)
    {
        try
        {
            var entity = _tableClient.Query<TableEntity>(e => e.RowKey == nhsNumber).FirstOrDefault();
            return entity?.GetString("SubscriptionId");
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to lookup subscription ID");
            return null;
        }
    }

    //WIP as there would be changes in the method after we gets proper NEMS API endpoints
    public async Task<bool> DeleteSubscriptionFromNemsAsync(string subscriptionId)
    {
        try
        {
            string nemsEndpoint = _config.NemsDeleteEndpoint;
            var response = await _httpClient.DeleteAsync($"{nemsEndpoint}/{subscriptionId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete subscription ID {SubscriptionId}", subscriptionId);
            return false;
        }
    }

    public async Task<string?> PostSubscriptionToNems(string subscription)
    {
        /* This is a WIP as additional work is required to use the NEMS endpoint after onboarding to NemsApi hub. */
        try
        {
            var url = string.Format(urlFormat, _config.NemsFhirEndpoint, "Subscription");
            var response = await _httpClientFunction.PostNemsGet(
                url,
                subscription,
                _config.SpineAccessToken,
                _config.FromAsid,
                _config.ToAsid
            );

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return Guid.NewGuid().ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send subscription to NEMS");
            return null;
        }
    }

    public async Task<bool> DeleteSubscriptionFromDatabaseAsync(string nhsNumber)
    {
        try
        {
            _logger.LogInformation("Deleting subscription from database");

            var deleted = await _nemsSubscriptionClient.Delete(nhsNumber);

            if (deleted)
            {
                _logger.LogInformation("Successfully deleted the subscription");
                return true;
            }

            _logger.LogError("Failed to delete the subscription");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while deleting the subscription for NHS number {NhsNumber}", nhsNumber);
            return false;
        }
    }

    public async Task<bool> SaveSubscriptionInDatabase(string nhsNumber, string subscriptionId)
    {
        /* This is a WIP as additional work is required to use the NEMS endpoint after onboarding to NemsApi hub. */
        _logger.LogInformation("Start saving the SubscriptionId in the database.");
        var objNemsSubscription = new NemsSubscription
        {
            SubscriptionId = Guid.Parse(subscriptionId), //WIP , might change after onboarding as datatype might change
            NhsNumber = Convert.ToInt64(nhsNumber), //WIP , might change after onboarding as datatype might change
            RecordInsertDateTime = DateTime.UtcNow
        };
        bool subscriptionCreated = await _nemsSubscriptionClient.Add(objNemsSubscription);

        return subscriptionCreated;
    }

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