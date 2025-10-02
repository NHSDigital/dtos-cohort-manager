namespace NHS.CohortManager.DemographicServices;

using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Common;
using DataServices.Core;
using Model;
using System.Text.Json;
using System.Linq;

/// <summary>
/// Azure Functions endpoints for managing CaaS subscriptions via MESH and data services.
/// </summary>
public class ManageCaasSubscription
{
    private readonly ILogger<ManageCaasSubscription> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ManageCaasSubscriptionConfig _config;
    private readonly IMeshSendCaasSubscribe _meshSendCaasSubscribe;
    private readonly IRequestHandler<NemsSubscription> _requestHandler;
    private readonly IDataServiceAccessor<NemsSubscription> _nemsSubscriptionAccessor;
    private readonly IMeshPoller _meshPoller;
    private readonly IExceptionHandler _exceptionHandler;
    private const string? nhsNum = "nhsNumber";

    public ManageCaasSubscription(
        ILogger<ManageCaasSubscription> logger,
        ICreateResponse createResponse,
        IOptions<ManageCaasSubscriptionConfig> config,
        IMeshSendCaasSubscribe meshSendCaasSubscribe,
        IRequestHandler<NemsSubscription> requestHandler,
        IDataServiceAccessor<NemsSubscription> nemsSubscriptionAccessor,
        IMeshPoller meshPoller,
        IExceptionHandler exceptionHandler)
    {
        _logger = logger;
        _createResponse = createResponse;
        _config = config.Value;
        _meshSendCaasSubscribe = meshSendCaasSubscribe;
        _requestHandler = requestHandler;
        _nemsSubscriptionAccessor = nemsSubscriptionAccessor;
        _meshPoller = meshPoller;
        _exceptionHandler = exceptionHandler;
    }


    /// <summary>
    /// Creates a new CaaS subscription for the given NHS number and persists a record.
    /// </summary>
    /// <param name="req">HTTP request containing an <c>nhsNumber</c> query parameter.</param>
    /// <returns>HTTP 200 on success, 400 for invalid input, or 500 on error.</returns>
    [Function("Subscribe")]
    public async Task<HttpResponseData> Subscribe([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        string? nhsNumber = req.Query[nhsNum];
        try
        {
            if (!ValidationHelper.ValidateNHSNumber(nhsNumber!))
            {
                return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.BadRequest, req, "NHS number is required and must be valid format.");
            }

            var nhsNo = long.Parse(nhsNumber!);

            var existing = await _nemsSubscriptionAccessor.GetSingle(i => i.NhsNumber == nhsNo);
            if (existing != null && !string.IsNullOrWhiteSpace(existing.SubscriptionId))
            {
                var src = existing.SubscriptionSource?.ToString() ?? "";
                _logger.LogInformation("CAAS Subscribe: existing subscription {SubId}, source {Source}; returning existing.", existing.SubscriptionId, string.IsNullOrWhiteSpace(src) ? "Unknown" : src);
                var message = string.IsNullOrWhiteSpace(src)
                    ? $"Already subscribed. Subscription ID: {existing.SubscriptionId}"
                    : $"Already subscribed. Subscription ID: {existing.SubscriptionId}. Source: {src}";
                return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.OK, req, message);
            }

            var toMailbox = _config.CaasToMailbox!;
            var fromMailbox = _config.CaasFromMailbox!;
            var messageId = await _meshSendCaasSubscribe.SendSubscriptionRequest(nhsNo, toMailbox, fromMailbox);

            if (string.IsNullOrEmpty(messageId))
            {
                var ex = new InvalidOperationException("Failed to send CAAS subscription via MESH");
                await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, nhsNo.ToString(), nameof(ManageCaasSubscription), "", $"to={toMailbox}");
                _logger.LogError("Failed to send CAAS subscription via MESH");
                return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.InternalServerError, req, "Failed to send CAAS subscription via MESH.");
            }

            // Save a record to NEMS_SUBSCRIPTION table with source = MESH
            var record = new NemsSubscription
            {
                SubscriptionId = messageId,
                NhsNumber = nhsNo,
                RecordInsertDateTime = DateTime.UtcNow,
                SubscriptionSource = SubscriptionSource.MESH
            };
            var saved = await _nemsSubscriptionAccessor.InsertSingle(record);
            if (!saved)
            {
                var ex = new InvalidOperationException("Failed to save CAAS subscription record to database");
                await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, nhsNo.ToString(), nameof(ManageCaasSubscription), "", System.Text.Json.JsonSerializer.Serialize(record));
                _logger.LogError("Failed to write CAAS subscription record to database");
                return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.InternalServerError, req, "Failed to save subscription record.");
            }
            LogSubscriptionSuccess(messageId);
            return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.OK, req, $"Subscription request accepted. MessageId: {messageId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending CAAS subscribe request");
            try
            {
                string? rawNhs = req.Query[nhsNum];
                var nhsForLog = ValidationHelper.ValidateNHSNumber(rawNhs!) ? rawNhs! : string.Empty;
                await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, nhsForLog, nameof(ManageCaasSubscription), "", string.Empty);
            }
            catch
            {
                // Swallow secondary errors to preserve primary failure path
            }
            return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.InternalServerError, req, "An error occurred while sending the CAAS subscription request.");
        }
    }
        /// <summary>
    /// Creates a new CaaS subscription for the given NHS number and persists a record.
    /// </summary>
    /// <param name="req">HTTP request containing an <c>nhsNumber</c> query parameter.</param>
    /// <returns>HTTP 200 on success, 400 for invalid input, or 500 on error.</returns>
    [Function("SubscribeMany")]
    public async Task<HttpResponseData> SubscribeMany([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        try
        {
            List<string> nhsNumbers;

            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                var requestBody = await reader.ReadToEndAsync();
                nhsNumbers = JsonSerializer.Deserialize<List<string>>(requestBody);
            }

            if (nhsNumbers.Count == 0)
            {
                return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.BadRequest, req, "No NHS numbers were provided");
            }

            HashSet<long> nhsNumberLongSet = new HashSet<long>();
            HashSet<string> failedNhsNumbers = new HashSet<string>();

            foreach (var nhsNumber in nhsNumbers)
            {
                if (!ValidationHelper.ValidateNHSNumber(nhsNumber!))
                {
                    failedNhsNumbers.Add(nhsNumber!);
                }

                if (!long.TryParse(nhsNumber, out long nhsNumberLong))
                {
                    failedNhsNumbers.Add(nhsNumber!);
                }

                nhsNumberLongSet.Add(nhsNumberLong);
            }

            var existing = await _nemsSubscriptionAccessor.GetRange(x => nhsNumberLongSet.Contains(x.NhsNumber));

            var existingNhsNumbers = existing.Select(e => e.NhsNumber).ToHashSet();
            var removedNhsNumbers = nhsNumberLongSet.Where(existingNhsNumbers.Contains);

            foreach (var nhsNumber in removedNhsNumbers)
            {
                var subscription = existing.Single(x => x.NhsNumber == nhsNumber);
                var src = subscription.SubscriptionSource?.ToString() ?? "";
                _logger.LogInformation("CAAS Subscribe: existing subscription {SubId}, source {Source}; returning existing.", subscription.SubscriptionId, string.IsNullOrWhiteSpace(src) ? "Unknown" : src);
            }

            nhsNumberLongSet.RemoveWhere(existingNhsNumbers.Contains);
            failedNhsNumbers.UnionWith(removedNhsNumbers.Select(x => x.ToString()));

            var toMailbox = _config.CaasToMailbox!;
            var fromMailbox = _config.CaasFromMailbox!;
            var messageId = await _meshSendCaasSubscribe.SendSubscriptionRequest(nhsNumberLongSet.ToArray(), toMailbox, fromMailbox);

            if (string.IsNullOrEmpty(messageId))
            {
                var ex = new InvalidOperationException("Failed to send CAAS subscription via MESH");
                await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, "", nameof(ManageCaasSubscription), "", $"to={toMailbox}");
                _logger.LogError("Failed to send CAAS subscription via MESH");
                return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.InternalServerError, req, "Failed to send CAAS subscription via MESH.");
            }
            int recordCount = 1;
            // Save a record to NEMS_SUBSCRIPTION table with source = MESH
            var records = nhsNumberLongSet.Select(x => new NemsSubscription
            {
                SubscriptionId = $"{messageId}_{recordCount++}",
                NhsNumber = x,
                RecordInsertDateTime = DateTime.UtcNow,
                SubscriptionSource = SubscriptionSource.MESH
            });
            var saved = await _nemsSubscriptionAccessor.InsertMany(records);


            if (!saved)
            {
                var ex = new InvalidOperationException("Failed to save CAAS subscription record to database");
                await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, "", nameof(ManageCaasSubscription), "", "");
                _logger.LogError("Failed to write CAAS subscription record to database");
                return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.InternalServerError, req, "Failed to save subscription record.");
            }
            LogSubscriptionSuccess(messageId);
            return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.OK, req, $"Subscription request accepted. MessageId: {messageId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending CAAS subscribe request");
            try
            {
                string? rawNhs = req.Query["nhsNumber"];
                var nhsForLog = ValidationHelper.ValidateNHSNumber(rawNhs!) ? rawNhs! : string.Empty;
                await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, nhsForLog, nameof(ManageCaasSubscription), "", string.Empty);
            }
            catch
            {
                // Swallow secondary errors to preserve primary failure path
            }
            return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.InternalServerError, req, "An error occurred while sending the CAAS subscription request.");
        }
    }

    /// <summary>
    /// Stub endpoint to remove a CaaS subscription for the given NHS number.
    /// </summary>
    /// <param name="req">HTTP request containing an <c>nhsNumber</c> query parameter.</param>
    /// <returns>HTTP 200 for the stub, or 400 for invalid input.</returns>
    [Function("Unsubscribe")]
    public async Task<HttpResponseData> Unsubscribe([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        var nhsNumber = req.Query[nhsNum];
        if (!ValidationHelper.ValidateNHSNumber(nhsNumber!))
        {
            return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.BadRequest, req, "NHS number is required and must be valid format.");
        }

        _logger.LogInformation("[CAAS-Stub] Unsubscribe called");
        return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.OK, req, "Stub: CAAS subscription would be removed.");
    }

    /// <summary>
    /// Checks subscription status for a given NHS number.
    /// </summary>
    /// <param name="req">HTTP request containing an <c>nhsNumber</c> query parameter.</param>
    /// <returns>HTTP 200 when an active subscription is found, 404 if not, or 400/500 on error.</returns>
    [Function("CheckSubscriptionStatus")]
    public async Task<HttpResponseData> CheckSubscriptionStatus([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("Received check subscription request");

            string? nhsNumber = req.Query[nhsNum];

            if (!ValidationHelper.ValidateNHSNumber(nhsNumber!))
            {
                _logger.LogError("NHS number is required and must be valid format");
                return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.BadRequest, req, "NHS number is required and must be valid format.");
            }

            var record = await _nemsSubscriptionAccessor.GetSingle(i => i.NhsNumber == long.Parse(nhsNumber!));
            string? subscriptionId = record?.SubscriptionId;

            if (string.IsNullOrEmpty(subscriptionId))
            {
                return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.NotFound, req, "No subscription found for this NHS number.");
            }

            return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.OK, req, $"Active subscription found. Subscription ID: {subscriptionId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking subscription status");
            try
            {
                string? rawNhs = req.Query[nhsNum];
                var nhsForLog = ValidationHelper.ValidateNHSNumber(rawNhs!) ? rawNhs! : string.Empty;
                await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, nhsForLog, nameof(ManageCaasSubscription), "", string.Empty);
            }
            catch
            {
                // Swallow secondary errors to preserve primary failure path
            }
            return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.InternalServerError, req, "An error occurred while checking subscription status.");
        }
    }

    /// <summary>
    /// Pass-through data service endpoint for CRUD operations on the NEMS subscription data object.
    /// </summary>
    /// <param name="req">HTTP request containing payload and route parameters.</param>
    /// <param name="key">Optional key or route tail for the data service.</param>
    /// <returns>HTTP response from the underlying data service handler.</returns>
    [Function("NemsSubscriptionDataService")]
    public async Task<HttpResponseData> NemsSubscriptionDataService([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = "NemsSubscriptionDataService/{*key}")] HttpRequestData req, string? key)
    {
        try
        {
            _logger.LogInformation("DataService Request Received Method: {Method}, DataObject {DataType} ", req.Method, typeof(NemsSubscription));
            var result = await _requestHandler.HandleRequest(req, key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error has occurred in data service");
            try
            {
                await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, string.Empty, nameof(ManageCaasSubscription), "", string.Empty);
            }
            catch
            {
                // Swallow secondary errors to preserve primary failure path
            }
            return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.InternalServerError, req, "An error occurred while processing the data service request.");
        }
    }

    /// <summary>
    /// Nightly timer trigger to validate the configured MESH mailbox via handshake.
    /// </summary>
    /// <param name="myTimer">Timer trigger context.</param>
    [Function("PollMeshMailbox")]
    public async Task RunAsync([TimerTrigger("59 23 * * *")] TimerInfo myTimer)
    {
        await _meshPoller.ExecuteHandshake(_config.CaasFromMailbox!);
    }

    private void LogSubscriptionSuccess(string messageId)
    {
        var logMessage = _config.IsStubbed
        ? $"CAAS Subscribe forwarded to MESH stub. MessageId: {messageId}"
        : $"CAAS Subscribe sent to MESH. MessageId: {messageId}";

        _logger.LogInformation(logMessage);
    }

}
