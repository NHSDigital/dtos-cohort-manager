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
using NHS.Screening.NEMSSubscribe;

public class NEMSSubscribe
{
    private readonly ILogger<NEMSSubscribe> _logger;
    private readonly FhirJsonSerializer _fhirSerializer = new();
    private readonly FhirJsonParser _fhirParser = new();
    private readonly IHttpClientFunction _httpClientFunction;
    private readonly ICreateResponse _createResponse;
    private readonly NEMSSubscribeConfig _config;
    private readonly IDataServiceClient<NemsSubscription> _nemsSubscriptionClient;
    private const string urlFormat = "{0}/{1}";

    public NEMSSubscribe
    (
        ILogger<NEMSSubscribe> logger,
        IDataServiceClient<NemsSubscription> nemsSubscriptionClient,
        IHttpClientFunction httpClientFunction,
        ICreateResponse createResponse,
        IOptions<NEMSSubscribeConfig> nemsSubscribeConfig
    )
    {
        _logger = logger;
        _nemsSubscriptionClient = nemsSubscriptionClient;
        _httpClientFunction = httpClientFunction;
        _createResponse = createResponse;
        _config = nemsSubscribeConfig.Value;
    }

    /// <summary>
    /// Azure Function that processes a NEMS subscription request by validating the NHS number,
    /// verifying the patient in PDS, creating a FHIR subscription resource, posting it to NEMS,
    /// and attempting to store the subscription locally in Sql-Server.
    /// </summary>
    /// <param name="req">
    /// The HTTP request data containing a JSON payload with the NHS number to subscribe.
    /// </param>
    /// <returns>
    /// An <see cref="HttpResponseData"/> indicating success (200 OK) with the subscription ID,
    /// or failure with the appropriate HTTP status code and error message.
    /// </returns>
    /// <remarks>
    /// This function performs the following steps:
    /// 1. Validates the NHS number format.
    /// 2. Calls PDS by calling "RetrievePDSDemographic" function to confirm patient existence.
    /// 3. Constructs and sends a FHIR subscription resource to NEMS.
    /// 4. After successful subscription creation, it will store the subscription details like
    /// subscriptionId, Subscription Logic Id etc locally in Sql-Server.
    /// </remarks>
    /// <exception cref="Exception">
    /// Returns HTTP 500 if an unexpected error occurs during processing.
    /// </exception>

    [Function("NEMSSubscribe")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "subscriptions/nems")] HttpRequestData req)
    {
        try
        {
            var nhsNumber = req.Query["nhsNumber"];

            // 1. Validate against PDS, if it returns record with matching NHS number
            bool pdsValidationResult = await ValidateAgainstPds(nhsNumber);
            if (!pdsValidationResult)
            {
                _logger.LogError("No matching patient found in PDS.");
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req); // skip subscription
            }

            // 2. Create Subscription Resource
            Subscription subscription = CreateNemsSubscriptionResource(nhsNumber);
            string subscriptionJson = _fhirSerializer.SerializeToString(subscription);

            // 3. Post to NEMS FHIR endpoint
            string subscriptionId = await PostSubscriptionToNems(subscriptionJson);
            if (string.IsNullOrEmpty(subscriptionId))
            {
                _logger.LogError("Failed to create subscription in NEMS.");
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, "Failed to create subscription in NEMS.");
            }

            // 4. Store in SQL Database
            bool storageSuccess = await StoreSubscriptionInDatabase(nhsNumber, subscriptionId);
            if (!storageSuccess)
            {
                _logger.LogError("Subscription created but failed to store locally.");
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, "Subscription created but failed to store locally.");
            }

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, subscriptionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in NEMS subscription workflow: {Message}", ex.Message);
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }

    public async Task<bool> ValidateAgainstPds(string nhsNumber)
    {
        try
        {
            _logger.LogInformation("Validating NHS number against PDS.");
            /* Calling RetrievePDSDemographic function */
            string url = _config.RetrievePdsDemographicURL;
            var parameters = new Dictionary<string, string>
            {
                { "nhsNumber", nhsNumber }
            };

            var response = await _httpClientFunction.SendGetAsync(url, parameters);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogError($"Record not found in PDS");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PDS validation error: {Message}", ex.Message);
            return false;
        }
    }

    private async Task<string> PostSubscriptionToNems(string subscriptionJson)
    {
        /* This is a WIP as additional work is required to use the NEMS endpoint after onboarding to NemsApi hub. */
        try
        {
            var url = string.Format(urlFormat, _config.NEMS_FHIR_ENDPOINT, "Subscription");
            var response = await _httpClientFunction.PostNemsGet(url, subscriptionJson, _config.SPINE_ACCESS_TOKEN, _config.FROM_ASID, _config.TO_ASID);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"NEMS subscription failed: {response.StatusCode}");
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdSubscription = _fhirParser.Parse<Subscription>(responseContent);
            return createdSubscription.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NEMS subscription error: {Message}", ex.Message);
            return null;
        }
    }

    public async Task<bool> StoreSubscriptionInDatabase(string nhsNumber, string subscriptionId)
    {
        /* This is a WIP as additional work is required to use the NEMS endpoint after onboarding to NemsApi hub. */
        _logger.LogInformation("Start saving the SubscriptionId in the database.");
        var objNemsSubscription = new NemsSubscription
        {
            SubscriptionId = Guid.Parse(subscriptionId), //WIP , might change after onboarding as datatype might change
            NhsNumber = Convert.ToInt64(nhsNumber), //WIP , might change after onboarding as datatype might change
            RecordInsertDateTime = DateTime.UtcNow
        };
        var subscriptionCreated = await _nemsSubscriptionClient.Add(objNemsSubscription);

        if (subscriptionCreated)
        {
            _logger.LogInformation("Successfully created the subscription");
            return true;
        }
        _logger.LogError("Failed to create the subscription");
        return false;
    }

    public Subscription CreateNemsSubscriptionResource(string nhsNumber)
    {
        /* This is a WIP as additional work is required to use the NEMS endpoint after onboarding to NemsApi hub. */
        var subscription = new Subscription
        {
            Meta = new Meta
            {
                //Profile = new[] { "https://fhir.nhs.uk/StructureDefinition/EMS-Subscription-1" }, // WIP, Will remove this after onboarding
                Profile = new[] { _config.Subscription_Profile },
                LastUpdated = DateTimeOffset.UtcNow
            },
            Status = Subscription.SubscriptionStatus.Requested,
            Reason = "NEMS event notification subscription",
            //Criteria = $"Patient?identifier=https://fhir.nhs.uk/Id/nhs-number|{nhsNumber}", // WIP, Will remove this after onboarding
            Criteria = $"Patient?identifier={_config.Subscription_Criteria}|{nhsNumber}",
            Channel = new Subscription.ChannelComponent
            {
                Type = Subscription.SubscriptionChannelType.RestHook,
                Endpoint = _config.CALLBACK_ENDPOINT,
                Payload = "application/fhir+json",
                Header = new[] {
                    $"Authorization: Bearer {_config.CALLBACK_AUTH_TOKEN}",
                    "X-Correlation-ID: " + Guid.NewGuid().ToString()
                }
            }
        };

        // Add NHS Digital extensions
        var parameters = new Parameters();
        parameters.Add("allowedEventType", new FhirString("new-event"));
        parameters.Add("subscriptionPeriod", new Period
        {
            Start = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"),
            End = DateTimeOffset.UtcNow.AddYears(1).ToString("yyyy-MM-dd")
        });

        return subscription;
    }
}
