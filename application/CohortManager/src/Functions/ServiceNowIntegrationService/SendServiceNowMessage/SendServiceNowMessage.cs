namespace NHS.CohortManager.ServiceNowIntegrationService;

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NHS.CohortManager.ServiceNowMessageService.Models;
using Microsoft.Extensions.Options;
using Common;

public class ServiceNowMessageHandler
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ServiceNowMessageHandler> _logger;
        private readonly SendServiceNowMsgConfig _config;
        private readonly ICreateResponse _createResponse;

        public ServiceNowMessageHandler(
            IHttpClientFactory httpClientFactory,
            ILogger<ServiceNowMessageHandler> logger,
            IOptions<SendServiceNowMsgConfig> sendServiceNowMsgConfig,
            ICreateResponse createResponse)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
            _config = sendServiceNowMsgConfig.Value;
            _createResponse = createResponse;
        }

        [Function("SendServiceNowMessage")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "servicenow/{baseUrl}/{profile}/{sysId}")] HttpRequestData req,
            string baseUrl,
            string profile,
            string sysId)
        {
            try
            {
                var requestBody = await req.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Request body is missing or empty.");
                }

                var input = JsonSerializer.Deserialize<ServiceNowRequestModel>(requestBody);

                if (string.IsNullOrWhiteSpace(input.WorkNotes))
                {
                    return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Invalid request payload");
                }
                var url = $"https://{baseUrl}/{_config.EndpointPath}/{profile}/{_config.Definition}/{sysId}";

                var payload = new
                {
                    state = input.State,
                    work_notes = input.WorkNotes
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config.AccessToken);

                _logger.LogInformation("Sending PUT request to: {Url}", url);

                var response = await _httpClient.PutAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Response: {Code} - {Body}", response.StatusCode, responseBody);

                response.EnsureSuccessStatusCode();
                return _createResponse.CreateHttpResponse(response.StatusCode, req, responseBody);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error occurred while sending message to ServiceNow.");
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, "ServiceNow update failed.");
            }
        }
    }
