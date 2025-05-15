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

    public ServiceNowIntegration(HttpClient httpClient, IConfiguration configuration, ILogger<ServiceNowIntegration> logger)
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
            var url = $"{_updateEndpoint}/{sysId}";

            var payload = new
            {
                state,
                work_notes = workNotes
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");


            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);


            _logger.LogInformation("Sending PUT request to ServiceNow for sys_id: {SysId} at URL: {Url}", sysId, url);


            var response = await _httpClient.PutAsync(url, content);

            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Successfully updated ServiceNow case: {SysId}", sysId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update ServiceNow case with sys_id: {SysId}", sysId);
            throw;
        }
    }
}
