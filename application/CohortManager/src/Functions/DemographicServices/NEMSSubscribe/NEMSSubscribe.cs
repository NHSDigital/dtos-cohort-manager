namespace NHS.CohortManager.DemographicServices;

using System;
using System.Net;
using System.Net.Http;
using System.Text;
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
    private readonly FhirJsonSerializer _fhirSerializer;
    private readonly FhirJsonParser _fhirParser;
    private readonly HttpClient _httpClient;
    private readonly ICreateResponse _createResponse;
    private readonly IExceptionHandler _handleException;
    private readonly ICallFunction _callFunction;
    private readonly NEMSSubscribeConfig _config;

    // private readonly IDataServiceClient<NemsSubscription> _nemsSubscriptionClient; /* To Do Later */

    public NEMSSubscribe(ILogger<NEMSSubscribe> logger,
    //IDataServiceClient<NEMSSubscribe> nemsSubscriptionClient, /* To Do Later */
    IHttpClientFactory httpClientFactory,
    IExceptionHandler handleException,
    ICreateResponse createResponse,
    ICallFunction callFunction,
    IOptions<NEMSSubscribeConfig> nemsSubscribeConfig)
    {
        _logger = logger;
        _fhirSerializer = new FhirJsonSerializer();
        _fhirParser = new FhirJsonParser();
        _httpClient = httpClientFactory.CreateClient();
        _handleException = handleException;
        _createResponse = createResponse;
        _callFunction = callFunction;
        _config = nemsSubscribeConfig.Value;
        //_nemsSubscriptionClient = nemsSubscriptionClient; /* To Do Later */
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
            // Parse NHS Number from request using System.Text.Json
            var requestBody = await req.ReadFromJsonAsync<NemsSubscriptionRequest>();
            string nhsNumber = requestBody?.NhsNumber;

            // Validate NHS Number format
            if (!ValidationHelper.ValidateNHSNumber(nhsNumber))
            {
                _logger.LogError("Invalid NHS Number {nhsNumber} was passed.", nhsNumber);
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Invalid NHS Number was passed."); // skip subscription
            }

            // 1. Validate against PDS, if it returns record with matching NHS number
            bool pdsValidationResult = await ValidateAgainstPds(nhsNumber);
            if (!pdsValidationResult)
            {
                _logger.LogError("No matching patient found in PDS having NHS Number {nhsNumber}.", nhsNumber);
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "No matching patient found in PDS."); // skip subscription
            }

            // 2. Create Subscription Resource
            Subscription subscription = CreateNemsSubscriptionResource(nhsNumber);
            string subscriptionJson = _fhirSerializer.SerializeToString(subscription);

            // 3. Post to NEMS FHIR endpoint
            string subscriptionId = await PostSubscriptionToNems(subscriptionJson);
            if (string.IsNullOrEmpty(subscriptionId))
            {
                _logger.LogError("Failed to create subscription in NEMS.");
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Failed to create subscription in NEMS.");
            }

            // 4. Store in SQL Database
            bool storageSuccess = await StoreSubscriptionInDatabase(nhsNumber, subscriptionId);
            if (!storageSuccess)
            {
                _logger.LogError("Subscription created but failed to store locally.");
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Subscription created but failed to store locally.");
            }

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, subscriptionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in NEMS subscription workflow: {Message}", ex.Message);
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }

    private async Task<bool> ValidateAgainstPds(string nhsNumber)
    {
        /* To Do Later - After Onboarding */
        try
        {
            /*
             string pdsApiUrl = $"{Environment.GetEnvironmentVariable("PDS_FHIR_ENDPOINT")}/Patient?identifier=https://fhir.nhs.uk/Id/nhs-number|{nhsNumber}";
            var request = new HttpRequestMessage(HttpMethod.Get, pdsApiUrl);
            request.Headers.Add("Authorization", $"Bearer {Environment.GetEnvironmentVariable("SPINE_ACCESS_TOKEN")}");
            request.Headers.Add("fromASID", Environment.GetEnvironmentVariable("FROM_ASID"));
            request.Headers.Add("toASID", Environment.GetEnvironmentVariable("PDS_ASID"));

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"PDS lookup failed: {response.StatusCode}");
                return false;
            }

            var content = await response.Content.ReadAsStringAsync();
            var bundle = _fhirParser.Parse<Bundle>(content);
            return bundle.Entry.Count > 0;
            */
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
        /* To Do Later - Modify and replace the place holder after onboarding */
        string nemsFhirEndpoint = _config.NEMS_FHIR_ENDPOINT;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{nemsFhirEndpoint}/Subscription")
            {
                Content = new StringContent(subscriptionJson, Encoding.UTF8, "application/fhir+json")
            };

            request.Headers.Add("Authorization", $"Bearer {_config.SPINE_ACCESS_TOKEN}");
            request.Headers.Add("fromASID", _config.FROM_ASID);
            request.Headers.Add("toASID", _config.TO_ASID);
            request.Headers.Add("Interaction-ID", "urn:nhs:names:services:nems:CreateSubscription");

            var response = await _httpClient.SendAsync(request);
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

    private async Task<bool> StoreSubscriptionInDatabase(string nhsNumber, string subscriptionId)
    {
        /** To Do Later - Implement the data service to save subscription in database **/

        try
        {
            /* To Do Later -  use _nemsSubscriptionClient to save subscription in DB after onboarding and code merge*/
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database storage error: {Message}", ex.Message);
            return false;
        }
    }

    public Subscription CreateNemsSubscriptionResource(string nhsNumber)
    {
        /* To Do Later - Modify the code and replace the placeholder with actual value */
        var subscription = new Subscription
        {
            Meta = new Meta
            {
                //Profile = new[] { "https://fhir.nhs.uk/StructureDefinition/EMS-Subscription-1" },
                Profile = new[] { _config.Subscription_Profile },
                LastUpdated = DateTimeOffset.UtcNow
            },
            Status = Subscription.SubscriptionStatus.Requested,
            Reason = "NEMS event notification subscription",
            //Criteria = $"Patient?identifier=https://fhir.nhs.uk/Id/nhs-number|{nhsNumber}",
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

        /* If necessary - uncomment after on-boarding */
        /* subscription.AddExtension(
         "https://fhir.nhs.uk/StructureDefinition/Extension-NHSNumberVerificationStatus-1",
         new CodeableConcept("https://fhir.nhs.uk/CodeSystem/NHSNumberVerificationStatus-1", "01", "Number present and verified")
         );
         */

        return subscription;
    }
}
