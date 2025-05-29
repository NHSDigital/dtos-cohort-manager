namespace NHS.CohortManager.ServiceNowMessageService;

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

public class ServiceNowMessage
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ServiceNowMessage> _logger;
    private readonly string _updateEndpoint;
    private readonly string _accessToken;

    public ServiceNowMessage(HttpClient httpClient, IConfiguration configuration, ILogger<ServiceNowMessage> logger)
    {
        _httpClient = httpClient;
        _logger = logger;


        _updateEndpoint = Environment.GetEnvironmentVariable("UpdateEndpoint");

        _accessToken = Environment.GetEnvironmentVariable("AccessToken");
    }


    public async Task<HttpResponseMessage> SendServiceNowMessage(string sysId, string workNotes, int state = 1)
    {
        try
        {
            var baseUrl = Environment.GetEnvironmentVariable("ServiceNowBaseUrl");
            var profile = Environment.GetEnvironmentVariable("Profile");
            var definition = Environment.GetEnvironmentVariable("Definition");
            var accessToken = Environment.GetEnvironmentVariable("AccessToken");

            var url = $"{baseUrl}/api/x_nhsd_intstation/nhs_integration/{profile}/{definition}/{sysId}";

            var payload = new
            {
                state,
                work_notes = workNotes
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            _logger.LogInformation("Sending PUT request to: {Url}", url);

             _logger.LogInformation("Calling ServiceNow URL: {Url}", url);

            var response = await _httpClient.PutAsync(url, content);

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Response: {Code} - {Body}", response.StatusCode, responseBody);

            response.EnsureSuccessStatusCode();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update ServiceNow case");
            throw;
        }
    }

}
