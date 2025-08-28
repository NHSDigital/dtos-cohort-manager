namespace NHS.CohortManager.DemographicServices;

using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Common;
using System.Collections.Specialized;
using System.Net.Http;
using System.Text;

public class ManageCaasSubscription
{
    private readonly ILogger<ManageCaasSubscription> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<ManageCaasSubscriptionConfig> _config;
    private readonly IMeshSendCaasSubscribe _meshSendCaasSubscribe;

    public ManageCaasSubscription(
        ILogger<ManageCaasSubscription> logger,
        ICreateResponse createResponse,
        IHttpClientFactory httpClientFactory,
        IOptions<ManageCaasSubscriptionConfig> config,
        IMeshSendCaasSubscribe meshSendCaasSubscribe)
    {
        _logger = logger;
        _createResponse = createResponse;
        _httpClientFactory = httpClientFactory;
        _config = config;
        _meshSendCaasSubscribe = meshSendCaasSubscribe;
    }

    [Function("Subscribe")]
    public async Task<HttpResponseData> Subscribe([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        var nhsNumber = req.Query["nhsNumber"];
        if (!ValidationHelper.ValidateNHSNumber(nhsNumber))
        {
            return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.BadRequest, req, "NHS number is required and must be valid format.");
        }

        // Forward to MeshSendCaasSubscribeStub (Shared)
        long.TryParse(nhsNumber, out var nhsNo);
        var toMailbox = _config.Value.CaasToMailbox;
        var fromMailbox = _config.Value.CaasFromMailbox;
        if (string.IsNullOrWhiteSpace(toMailbox) || string.IsNullOrWhiteSpace(fromMailbox))
        {
            _logger.LogError("CAAS mailbox configuration missing. CaasToMailbox or CaasFromMailbox not set.");
            return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.InternalServerError, req, "CAAS mailbox configuration missing.");
        }
        var messageId = await _meshSendCaasSubscribe.SendSubscriptionRequest(nhsNo, toMailbox, fromMailbox);
        _logger.LogInformation("CAAS Subscribe forwarded to Mesh stub. NHS: {Nhs}, MessageId: {Msg}", nhsNo, messageId);
        return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.OK, req, $"Subscription request accepted. MessageId: {messageId}");
    }

    [Function("Unsubscribe")]
    public async Task<HttpResponseData> Unsubscribe([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        var nhsNumber = req.Query["nhsNumber"];
        if (!ValidationHelper.ValidateNHSNumber(nhsNumber))
        {
            return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.BadRequest, req, "NHS number is required and must be valid format.");
        }

        _logger.LogInformation("[CAAS-Stub] Unsubscribe called for NHS: {Nhs}", nhsNumber);
        return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.OK, req, "Stub: CAAS subscription would be removed.");
    }

    [Function("CheckSubscriptionStatus")]
    public async Task<HttpResponseData> CheckSubscriptionStatus([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        var baseUrl = _config.Value.ManageNemsSubscriptionBaseURL;
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var forwardUrl = $"{baseUrl.TrimEnd('/')}/api/CheckSubscriptionStatus{(req.Url?.Query ?? CreateQueryString(req.Query))}";

                using var forwardRequest = new HttpRequestMessage(HttpMethod.Get, forwardUrl);
                var accept = req.Headers.GetValues("Accept").FirstOrDefault();
                if (!string.IsNullOrEmpty(accept))
                {
                    forwardRequest.Headers.TryAddWithoutValidation("Accept", accept);
                }

                var forwardResponse = await client.SendAsync(forwardRequest);
                var responseBody = await forwardResponse.Content.ReadAsStringAsync();

                var resp = _createResponse.CreateHttpResponse(forwardResponse.StatusCode, req, responseBody);
                var respContentType = forwardResponse.Content.Headers.ContentType?.ToString();
                if (!string.IsNullOrEmpty(respContentType))
                {
                    resp.Headers.Add("Content-Type", respContentType);
                }
                return resp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error forwarding CheckSubscriptionStatus request");
                return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.InternalServerError, req, "Error forwarding request to NEMS subscription service.");
            }
        }

        // Fallback stubbed behaviour if no forward URL configured
        var nhsNumber = req.Query["nhsNumber"];
        if (!ValidationHelper.ValidateNHSNumber(nhsNumber))
        {
            return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.BadRequest, req, "NHS number is required and must be valid format.");
        }
        _logger.LogInformation("[CAAS-Stub] CheckSubscriptionStatus called for NHS: {Nhs}", nhsNumber);
        return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.OK, req, "Stub: CAAS subscription status check.");
    }

    [Function("NemsSubscriptionDataService")]
    public async Task<HttpResponseData> NemsSubscriptionDataService([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = "NemsSubscriptionDataService/{*key}")] HttpRequestData req, string? key)
    {
        var forwardBase = _config.Value.ManageNemsSubscriptionDataServiceURL;
        if (string.IsNullOrWhiteSpace(forwardBase))
        {
            _logger.LogInformation("[CAAS-Stub] Forward URL not configured; returning stub response. Method: {Method}, Key: {Key}", req.Method, key);
            return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.OK, req, "Stub: CAAS data service placeholder.");
        }

        try
        {
            var client = _httpClientFactory.CreateClient();

            // Build forward URL preserving key and query
            var pathPart = string.IsNullOrEmpty(key) ? string.Empty : $"/{key}";
            var query = req.Url?.Query ?? CreateQueryString(req.Query); // includes leading '?', or empty
            var forwardUrl = $"{forwardBase.TrimEnd('/')}{pathPart}{query}";

            var method = new HttpMethod(req.Method);

            HttpContent? content = null;
            // Only attach body for methods that typically have one
            if (string.Equals(req.Method, HttpMethod.Post.Method, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(req.Method, HttpMethod.Put.Method, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(req.Method, "PATCH", StringComparison.OrdinalIgnoreCase))
            {
                if (req.Body != null)
                {
                    req.Body.Position = 0;
                    using var reader = new StreamReader(req.Body, Encoding.UTF8, leaveOpen: true);
                    var body = await reader.ReadToEndAsync();
                    var contentType = req.Headers.GetValues("Content-Type").FirstOrDefault() ?? "application/json";
                    content = new StringContent(body, Encoding.UTF8, contentType);
                }
            }

            using var forwardRequest = new HttpRequestMessage(method, forwardUrl)
            {
                Content = content
            };

            // Basic header pass-through (Accept)
            var accept = req.Headers.GetValues("Accept").FirstOrDefault();
            if (!string.IsNullOrEmpty(accept))
            {
                forwardRequest.Headers.TryAddWithoutValidation("Accept", accept);
            }

            var forwardResponse = await client.SendAsync(forwardRequest);
            var responseBody = await forwardResponse.Content.ReadAsStringAsync();

            var resp = _createResponse.CreateHttpResponse(forwardResponse.StatusCode, req, responseBody);
            var respContentType = forwardResponse.Content.Headers.ContentType?.ToString();
            if (!string.IsNullOrEmpty(respContentType))
            {
                resp.Headers.Add("Content-Type", respContentType);
            }
            return resp;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forwarding NemsSubscriptionDataService request");
            return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.InternalServerError, req, "Error forwarding request to NEMS subscription service.");
        }
    }

    private static string CreateQueryString(NameValueCollection? query)
    {
        if (query == null || query.Count == 0) return string.Empty;
        var parts = new List<string>();
        foreach (var key in query.AllKeys)
        {
            if (key == null) continue;
            var value = query[key] ?? string.Empty;
            parts.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
        }
        return parts.Count > 0 ? $"?{string.Join("&", parts)}" : string.Empty;
    }
}
