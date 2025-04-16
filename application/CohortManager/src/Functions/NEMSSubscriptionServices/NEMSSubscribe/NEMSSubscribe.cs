namespace NHS.CohortManager.NEMSSubscriptionServices;

using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Rest;
using System.Net.Http.Headers;
using Model;
using Common;
using Data.Database;
using DataServices.Client;

public class NEMSSubscribe
{
    private readonly ILogger<NEMSSubscribe> _logger;
    private readonly HttpClient _httpClient;
    private readonly FhirJsonSerializer _fhirSerializer;
    private const string NemsFhirEndpoint = "https://nems.api.service.nhs.uk/fhir/Subscription";

    public NEMSSubscribe(ILogger<NEMSSubscribe> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _fhirSerializer = new FhirJsonSerializer();
    }

    [Function("CreateNemsSubscription")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("Received request to create NEMS Subscription.");

        try
        {
            // Parse body
            var body = await req.ReadAsStringAsync();
            var jsonDoc = System.Text.Json.JsonDocument.Parse(body);
            var nhsNumber = jsonDoc.RootElement.GetProperty("nhsNumber").GetString();

            if (string.IsNullOrWhiteSpace(nhsNumber))
            {
                var badReq = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badReq.WriteStringAsync("Missing required NHS Number.");
                return badReq;
            }

            var subscription = BuildEmsSubscriptionResource(nhsNumber);

            var json = _fhirSerializer.SerializeToString(subscription);
            var content = new StringContent(json, Encoding.UTF8, "application/fhir+json");

            // Auth header placeholder — replace with actual token/SSP auth
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "your_access_token");

            var response = await _httpClient.PostAsync(NemsFhirEndpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            var res = req.CreateResponse(response.IsSuccessStatusCode ? System.Net.HttpStatusCode.Created : System.Net.HttpStatusCode.BadRequest);
            await res.WriteStringAsync(responseBody);
            return res;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating subscription: {ex.Message}");
            var errorRes = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorRes.WriteStringAsync("Internal server error: " + ex.Message);
            return errorRes;
        }
    }

    private Subscription BuildEmsSubscriptionResource(string nhsNumber)
    {
        var meta = new Meta
        {
            Profile = new[] { "https://fhir.nhs.uk/StructureDefinition/EMS-Subscription-1" },
            Tag = new List<Coding>
        {
            new Coding
            {
                System = "https://fhir.nhs.uk/CodeSystem/ems-subscription-tag",
                Code = "nems"
            }
        }
        };

        var subscription = new Subscription
        {
            Id = Guid.NewGuid().ToString(),
            Meta = meta,
            Status = Subscription.SubscriptionStatus.Active,
            Reason = "Monitor patient activity for a specific NHS number",
            Criteria = $"Encounter?patient.identifier=https://fhir.nhs.uk/Id/nhs-number|{nhsNumber}",
            Channel = new Subscription.ChannelComponent
            {
                Type = Subscription.SubscriptionChannelType.RestHook,
                Endpoint = "https://your-callback-endpoint.example.com/fhir", // replace with real endpoint
                Payload = "application/fhir+json",
                Header = new[]
                {
                "Authorization: Bearer your-callback-token"
            }
            },
            EndElement = new Instant(DateTimeOffset.UtcNow.AddMonths(6)) //  FHIR-compliant Instant
        };

        return subscription;
    }

}
