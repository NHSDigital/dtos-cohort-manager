namespace NHS.CohortManager.ServiceNowIntegrationService;

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

public class ServiceNowIntegration
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ServiceNowIntegration> _logger;
    private readonly string _updateEndpoint;
    private readonly string _accessToken;

    // Constructor: Inject HttpClient, IConfiguration, and ILogger
    public ServiceNowIntegration(HttpClient httpClient, IConfiguration configuration, ILogger<ServiceNowIntegration> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Load the base update URL from configuration
        // Example provided by ServiceNow: https://nhsdigitaldev.service-now.com/api/x_nhsd_intstation/nhs_integration/<Profile>/<Definition>/<sys_id>
        _updateEndpoint = Environment.GetEnvironmentVariable("UpdateEndpoint");

        // Access token should be securely passed from config or key vault
        // This token is used as Bearer token in Authorization header
        _accessToken = Environment.GetEnvironmentVariable("AccessToken");
    }

    /// <summary>
    /// Sends a PUT request to update a ServiceNow case using sys_id.
    /// API Endpoint Format: https://nhsdigitaldev.service-now.com/api/x_nhsd_intstation/nhs_integration/9c78f87c97912e10dd80f2df9153aff5/CohortCaseUpdate/304df4f497d12e10dd80f2df9153af78
    /// Payload Example: { "state":1, "work_notes": "Lorem ipsum dolor sit amet" }
    /// </summary>
    /// <param name="sysId">ServiceNow sys_id of the case (e.g., "00dc017497552e10dd80f2df9153af69", "8f1d05f497552e10dd80f2df9153afa0", "9f4d01b497552e10dd80f2df9153af10")</param>
    /// <param name="workNotes">Work notes to include in the update</param>
    /// <param name="state">State of the case (default: 1)</param>
    /// <returns>HTTP response message from ServiceNow</returns>
    public async Task<HttpResponseMessage> SendServiceNowMessage(string sysId, string workNotes, int state = 1)
    {
        try
        {
            // Build the full API URL using the provided sys_id
            // Example for sys_id "304df4f497d12e10dd80f2df9153af78":  https://nhsdigitaldev.service-now.com/api/x_nhsd_intstation/nhs_integration/9c78f87c97912e10dd80f2df9153aff5/CohortCaseUpdate/304df4f497d12e10dd80f2df9153af78
            var url = $"{_updateEndpoint}/{sysId}";

            // Build the JSON body as per ServiceNow spec
            // Example payload: { "state":1, "work_notes": "Lorem ipsum dolor sit amet" }
            var payload = new
            {
                state,
                work_notes = workNotes
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Set Bearer Token Authentication in request header
            // Example: Authorization: Bearer <access_token>
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Log request details
            _logger.LogInformation("Sending PUT request to ServiceNow for sys_id: {SysId} at URL: {Url}", sysId, url);

            // Make PUT request to ServiceNow
            var response = await _httpClient.PutAsync(url, content);

            // Throw if not successful
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Successfully updated ServiceNow case: {SysId}", sysId);
            return response;
        }
        catch (Exception ex)
        {
            // Log any errors encountered during the request
            _logger.LogError(ex, "Failed to update ServiceNow case with sys_id: {SysId}", sysId);
            throw;
        }
    }
}
