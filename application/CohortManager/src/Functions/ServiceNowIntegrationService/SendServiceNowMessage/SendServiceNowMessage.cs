namespace NHS.CohortManager.ServiceNowMessageService;

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

public class SendServiceNowMessageFunction
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly SendServiceNowMsgConfig _config;
    private readonly ICreateResponse _createResponse;

    public SendServiceNowMessageFunction(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, IOptions<SendServiceNowMsgConfig> sendServiceNowMsgConfig, ICreateResponse createResponse)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = loggerFactory.CreateLogger<SendServiceNowMessageFunction>();
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

            if (input is null || string.IsNullOrWhiteSpace(input.WorkNotes))
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Invalid request payload");

            }

            var response = await SendServiceNowMessage(baseUrl, profile, sysId, input.WorkNotes, input.State);
            var responseBody = await response.Content.ReadAsStringAsync();
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while sending message to ServiceNow.");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, "ServiceNow update failed.");
        }
    }

    private async Task<HttpResponseMessage> SendServiceNowMessage(string baseUrl, string profile, string sysId, string workNotes, int state)
    {
        try
        {
            var definition = _config.Definition;
            var accessToken = _config.AccessToken;
            var endPointPath = _config.EndpointPath;

            var url = $"https://{baseUrl}/{endPointPath}/{profile}/{definition}/{sysId}";

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

            var response = await _httpClient.PutAsync(url, content);

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Response: {Code} - {Body}", response.StatusCode, responseBody);

            response.EnsureSuccessStatusCode();
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error occurred while sending message to ServiceNow");

            // Return an appropriate fallback response
            var errorResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("An error occurred while processing the request.")
            };
            return errorResponse;
        }
    }
}
