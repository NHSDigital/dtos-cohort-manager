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
using Microsoft.Extensions.Configuration;
using NHS.CohortManager.ServiceNowMessageService.Models;
using Common;

public class SendServiceNowMessageFunction
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;
    private readonly ICreateResponse _createResponse;

    public SendServiceNowMessageFunction(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, IConfiguration configuration, ICreateResponse createResponse)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = loggerFactory.CreateLogger<SendServiceNowMessageFunction>();
        _configuration = configuration;
        _createResponse = createResponse;
    }

    [Function("SendServiceNowMessage")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "servicenow/{sysId}")] HttpRequestData req,
        string sysId)
    {
        try
        {
            var requestBody = await req.ReadAsStringAsync();
            var input = JsonSerializer.Deserialize<ServiceNowRequestModel>(requestBody);

            if (input is null || string.IsNullOrWhiteSpace(input.WorkNotes))
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Invalid request payload");

            }

            var result = await SendServiceNowMessage(sysId, input.WorkNotes, input.State);
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, await result.Content.ReadAsStringAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while sending message to ServiceNow.");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("ServiceNow update failed.");
            return errorResponse;
        }
    }

    public async Task<HttpResponseMessage> SendServiceNowMessage(string sysId, string workNotes, int state = 1)
    {
        try
        {
            var baseUrl = _configuration["ServiceNowBaseUrl"];
            var profile = _configuration["Profile"];
            var definition = _configuration["Definition"];
            var accessToken = _configuration["AccessToken"];

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
