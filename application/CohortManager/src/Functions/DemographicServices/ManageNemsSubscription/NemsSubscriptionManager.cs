namespace NHS.CohortManager.DemographicServices;

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using Common;
using DataServices.Core;
using System.Security.Cryptography.X509Certificates;
using Hl7.Fhir.Serialization;
// Use explicit STU3 aliases to avoid R4 conflicts
using STU3Subscription = Hl7.Fhir.Model.Subscription;
using STU3Meta = Hl7.Fhir.Model.Meta;
using STU3ContactPoint = Hl7.Fhir.Model.ContactPoint;
using System.Text.RegularExpressions;

public class NemsSubscriptionManager
{
    private readonly INemsHttpClientFunction _httpClient;
    private readonly ILogger<NemsSubscriptionManager> _logger;
    private readonly ManageNemsSubscriptionConfig _config;
    private readonly IDataServiceAccessor<NemsSubscription> _nemsSubscriptionAccessor;
    private readonly X509Certificate2 _nemsCertificate; // injected!

    public NemsSubscriptionManager(
        INemsHttpClientFunction httpClient,
        IOptions<ManageNemsSubscriptionConfig> config,
        ILogger<NemsSubscriptionManager> logger,
        IDataServiceAccessor<NemsSubscription> nemsSubscriptionAccessor,
        X509Certificate2 nemsCertificate)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;
        _nemsSubscriptionAccessor = nemsSubscriptionAccessor;
        _nemsCertificate = nemsCertificate;
    }

    /// <summary>
    /// Looks up the subscription ID for a given NHS number in the database
    /// </summary>
    /// <param name="nhsNumber">The NHS number to look up.</param>
    /// <returns>
    /// The subscription ID if found, otherwise null.
    /// </returns>
    public async Task<string?> LookupSubscriptionIdAsync(string nhsNumber)
    {
        try
        {
            var subscription = await _nemsSubscriptionAccessor.GetSingle(i => i.NhsNumber == long.Parse(nhsNumber));
            if (subscription != null)
            {
                return subscription.SubscriptionId;
            }

            return null;
        }
        catch (Exception ex)
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
    /// True if deletion was successful, otherwise false.
    /// </returns>
    public async Task<bool> DeleteSubscriptionFromNemsAsync(string subscriptionId)
    {
        try
        {
            // Generate JWT token for delete operation - use base URL for audience
            var baseUri = new Uri(_config.NemsFhirEndpoint);
            var baseUrl = $"{baseUri.Scheme}://{baseUri.Host}";
            var jwtToken = _httpClient.GenerateJwtToken(
                _config.NemsFromAsid,
                baseUrl,
                "patient/Subscription.write"
            );

            string deleteUrl = $"{_config.NemsFhirEndpoint}/Subscription/{subscriptionId}";

            var bypassCert = _config.NemsBypassServerCertificateValidation;

            var deleteRequest = new NemsSubscriptionRequest
            {
                Url = deleteUrl,
                JwtToken = jwtToken,
                FromAsid = _config.NemsFromAsid,
                ToAsid = _config.NemsToAsid,
                ClientCertificate = _nemsCertificate,
                BypassCertValidation = bypassCert
            };

            var response = await _httpClient.SendSubscriptionDelete(deleteRequest, _config.NemsHttpClientTimeoutSeconds);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete subscription ID {SubscriptionId}", subscriptionId);
            return false;
        }
    }

    /// <summary>
    /// Sends the given subscription to NEMS
    /// </summary>
    /// <param name="subscriptionJson">The serialised subscription object</param>
    /// <returns>
    /// The subscription ID if successful, otherwise null.
    /// </returns>
    public async Task<string?> SendSubscriptionToNemsAsync(string subscriptionJson)
    {
        try
        {
            var baseUrl = _config.NemsFhirEndpoint.Replace("/STU3", "");
            var jwtToken = _httpClient.GenerateJwtToken(
                _config.NemsFromAsid,
                baseUrl,
                "patient/Subscription.write"
            );

            var url = $"{_config.NemsFhirEndpoint}/Subscription";
            var bypassCert = _config.NemsBypassServerCertificateValidation;

            var postRequest = new NemsSubscriptionPostRequest
            {
                Url = url,
                SubscriptionJson = subscriptionJson,
                JwtToken = jwtToken,
                FromAsid = _config.NemsFromAsid,
                ToAsid = _config.NemsToAsid,
                ClientCertificate = _nemsCertificate,
                BypassCertValidation = bypassCert
            };

            var response = await _httpClient.SendSubscriptionPost(postRequest, _config.NemsHttpClientTimeoutSeconds);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("NEMS returned error response: {Response}", errorContent);

                // Handle known duplicate error
                if (errorContent.Contains("DUPLICATE_REJECTED", StringComparison.OrdinalIgnoreCase))
                {
                    var match = Regex.Match(
                        errorContent,
                        @"subscription already exists\s*:\s*""?([a-zA-Z0-9-_]+)""?",
                        RegexOptions.None,
                        TimeSpan.FromMilliseconds(100)
                    );

                    if (match.Success)
                    {
                        var existingId = match.Groups[1].Value;
                        _logger.LogWarning("Subscription already exists in Spine with ID: {Id}", existingId);
                        return existingId;
                    }

                    _logger.LogWarning("Duplicate response received but failed to parse existing subscription ID.");
                }

                _logger.LogError("Failed to create NEMS subscription. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode, errorContent);
                return null;
            }

            if (response.Headers.Location == null)
            {
                _logger.LogWarning("Subscription created but no Location header found in response");
                return null;
            }

            var locationPath = response.Headers.Location.ToString();
            var subscriptionId = locationPath.Split('/').LastOrDefault();

            _logger.LogInformation("Successfully created NEMS subscription with ID: {SubscriptionId}", subscriptionId);
            return subscriptionId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending subscription to NEMS");
            return null;
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
            _logger.LogError(ex, "Exception occurred while deleting the subscription");
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
    public async Task<bool> SaveSubscriptionInDatabase(string nhsNumber, string subscriptionId)
    {
        try
        {
            _logger.LogInformation("Start saving the SubscriptionId in the database.");
            var subscription = new NemsSubscription
            {
                SubscriptionId = subscriptionId,
                NhsNumber = Convert.ToInt64(nhsNumber),
                RecordInsertDateTime = DateTime.UtcNow,
                SubscriptionSource = SubscriptionSource.NEMS
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
    /// Creates a new subscription object for the NEMS API using proper FHIR STU3 types.
    /// </summary>
    /// <param name="nhsNumber">The NHS number to create the subscription for.</param>
    /// <param name="eventType">The event type to subscribe to (e.g., pds-record-change-1)</param>
    /// <returns>
    /// A STU3 FHIR Subscription object
    /// </returns>
    public STU3Subscription CreateSubscription(string nhsNumber, string eventType = "pds-record-change-1")
    {
        var subscription = new STU3Subscription
        {
            Meta = new STU3Meta
            {
                Profile = new[] { _config.NemsSubscriptionProfile },
                LastUpdated = DateTimeOffset.UtcNow
            },
            Status = STU3Subscription.SubscriptionStatus.Requested,
            Reason = $"Subscribe to {eventType} events for patient",

            Criteria = $"/Bundle?type=message&Patient.identifier={_config.NemsSubscriptionCriteria}|{nhsNumber}&MessageHeader.event={eventType}",

            Channel = new STU3Subscription.ChannelComponent
            {
                Type = STU3Subscription.SubscriptionChannelType.Message, // Use 'message' for MESH delivery
                Endpoint = _config.NemsMeshMailboxId, // Use MESH mailbox ID, not ODS code
                Payload = "application/fhir+json"
            }
        };

        _logger.LogInformation("Creating subscription with endpoint: {Endpoint}", subscription.Channel.Endpoint);


        subscription.Contact = new List<STU3ContactPoint>
        {
            new STU3ContactPoint
            {
                System = STU3ContactPoint.ContactPointSystem.Url,
                Value = $"https://directory.spineservices.nhs.uk/STU3/Organization/{_config.NemsOdsCode}",
                Use = STU3ContactPoint.ContactPointUse.Work
            }
        };


        return subscription;
    }

    /// <summary>
    /// Serializes a subscription object to JSON for sending to NEMS using proper FHIR serialisation
    /// </summary>
    /// <param name="subscription">The subscription to serialise</param>
    /// <returns>JSON string representation of the subscription</returns>
    public static string SerialiseSubscription(STU3Subscription subscription)
    {
        var serialiser = new FhirJsonSerializer();
        return serialiser.SerializeToString(subscription);
    }

    /// <summary>
    /// Creates and sends a subscription to NEMS, then saves it to the database.
    /// Returns a SubscriptionResult with the subscription ID or error details.
    /// </summary>
    /// <param name="nhsNumber">NHS number to subscribe to</param>
    /// <param name="eventType">Event type to subscribe to</param>
    /// <returns>SubscriptionResult with success status and subscription ID or error message</returns>
    public async Task<SubscriptionResult> CreateAndSendSubscriptionAsync(string nhsNumber, string eventType = "pds-record-change-1")
    {
        try
        {
            // Check if subscription already exists
            var existingSubscriptionId = await LookupSubscriptionIdAsync(nhsNumber);
            if (!string.IsNullOrEmpty(existingSubscriptionId))
            {
                _logger.LogInformation("Subscription already exists with ID {SubscriptionId}",
                    existingSubscriptionId);
                return SubscriptionResult.CreateSuccess(existingSubscriptionId);
            }

            // Create subscription object using proper FHIR STU3 types
            var subscription = CreateSubscription(nhsNumber, eventType);

            // Serialize to JSON using FHIR serializer
            var subscriptionJson = SerialiseSubscription(subscription);

            // Send to NEMS
            var subscriptionId = await SendSubscriptionToNemsAsync(subscriptionJson);

            if (string.IsNullOrEmpty(subscriptionId))
            {
                return SubscriptionResult.CreateFailure("Failed to create subscription in NEMS");
            }

            // Save to database
            var saved = await SaveSubscriptionInDatabase(nhsNumber, subscriptionId);

            if (saved)
            {
                _logger.LogInformation("Successfully created and saved subscription");
                return SubscriptionResult.CreateSuccess(subscriptionId);
            }

            _logger.LogError("Failed to save subscription to database");

            // Cleanup: delete from NEMS since database save failed
            await DeleteSubscriptionFromNemsAsync(subscriptionId);
            return SubscriptionResult.CreateFailure("Failed to save subscription to database");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create and send subscription");
            return SubscriptionResult.CreateFailure("An error occurred while creating subscription");
        }
    }

    /// <summary>
    /// Removes a subscription both from NEMS and the local database
    /// </summary>
    /// <param name="nhsNumber">NHS number to unsubscribe</param>
    /// <returns>True if successful, false otherwise</returns>
    public async Task<bool> RemoveSubscriptionAsync(string nhsNumber)
    {
        try
        {
            // Look up subscription ID
            var subscriptionId = await LookupSubscriptionIdAsync(nhsNumber);
            if (string.IsNullOrEmpty(subscriptionId))
            {
                _logger.LogWarning("No subscription found in database");
                return false;
            }

            // Delete from NEMS first
            var nemsDeleted = await DeleteSubscriptionFromNemsAsync(subscriptionId);

            // Delete from database regardless of NEMS result (cleanup)
            var dbDeleted = await DeleteSubscriptionFromDatabaseAsync(nhsNumber);

            if (nemsDeleted && dbDeleted)
            {
                _logger.LogInformation("Successfully removed subscription");
                return true;
            }
            else
            {
                _logger.LogWarning("Partial removal completed. NEMS: {NemsDeleted}, DB: {DbDeleted}",
                    nemsDeleted, dbDeleted);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove subscription");
            return false;
        }
    }
}
