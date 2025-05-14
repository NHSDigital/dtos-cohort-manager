namespace NHS.CohortManager.ServiceNowIntegrationService;

using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;


    public class ServiceNowIntegration
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ServiceNowIntegration> _logger;
        private readonly string _baseServiceNowUrl;

        public ServiceNowIntegration(HttpClient httpClient, IConfiguration configuration, ILogger<ServiceNowIntegration> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _baseServiceNowUrl = configuration["ServiceNow:BaseUrl"];
        }

        public async Task<HttpResponseMessage> SendServiceNowMessage(string caseId, object payload)
        {
            try
            {
                var url = $"{_baseServiceNowUrl}/api/now/table/incident/{caseId}";
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending PUT request to ServiceNow for case {CaseId}", caseId);
                var response = await _httpClient.PutAsync(url, content);

                response.EnsureSuccessStatusCode();
                _logger.LogInformation("Successfully updated case {CaseId} in ServiceNow.", caseId);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update case {CaseId} in ServiceNow.", caseId);
                throw;
            }
        }
    }

