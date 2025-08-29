namespace NHS.CohortManager.DemographicServices;

using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Common;
using System.Collections.Specialized;
using System.Text;
using DataServices.Core;
using Model;
using NHS.CohortManager.DemographicServices;

public class ManageCaasSubscription
{
    private readonly ILogger<ManageCaasSubscription> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ManageCaasSubscriptionConfig _config;
    private readonly IMeshSendCaasSubscribe _meshSendCaasSubscribe;
    private readonly IRequestHandler<NemsSubscription> _requestHandler;
    private readonly IDataServiceAccessor<NemsSubscription> _nemsSubscriptionAccessor;
    private readonly IMeshPoller _meshPoller;

    public ManageCaasSubscription(
        ILogger<ManageCaasSubscription> logger,
        ICreateResponse createResponse,
        IOptions<ManageCaasSubscriptionConfig> config,
        IMeshSendCaasSubscribe meshSendCaasSubscribe,
        IRequestHandler<NemsSubscription> requestHandler,
        IDataServiceAccessor<NemsSubscription> nemsSubscriptionAccessor,
        IMeshPoller meshPoller)
    {
        _logger = logger;
        _createResponse = createResponse;
        _config = config.Value;
        _meshSendCaasSubscribe = meshSendCaasSubscribe;
        _requestHandler = requestHandler;
        _nemsSubscriptionAccessor = nemsSubscriptionAccessor;
        _meshPoller = meshPoller;
    }

    [Function("Subscribe")]
    public async Task<HttpResponseData> Subscribe([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        try
        {
            var nhsNumber = req.Query["nhsNumber"];
            if (!ValidationHelper.ValidateNHSNumber(nhsNumber!))
            {
                return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.BadRequest, req, "NHS number is required and must be valid format.");
            }

            // Forward to MeshSendCaasSubscribeStub (Shared)
            var nhsNo = long.Parse(nhsNumber!);
            var toMailbox = _config.CaasToMailbox!;
            var fromMailbox = _config.CaasFromMailbox!;
            var messageId = await _meshSendCaasSubscribe.SendSubscriptionRequest(nhsNo, toMailbox, fromMailbox);

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
                _logger.LogError("Failed to write CAAS subscription record to database");
                return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.InternalServerError, req, "Failed to save subscription record.");
            }

            _logger.LogInformation("CAAS Subscribe forwarded to Mesh stub. MessageId: {Msg}", messageId);
            return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.OK, req, $"Subscription request accepted. MessageId: {messageId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending CAAS subscribe request");
            return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.InternalServerError, req, "An error occurred while sending the CAAS subscription request.");
        }
    }

    [Function("Unsubscribe")]
    public async Task<HttpResponseData> Unsubscribe([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        var nhsNumber = req.Query["nhsNumber"];
        if (!ValidationHelper.ValidateNHSNumber(nhsNumber!))
        {
            return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.BadRequest, req, "NHS number is required and must be valid format.");
        }

        _logger.LogInformation("[CAAS-Stub] Unsubscribe called");
        return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.OK, req, "Stub: CAAS subscription would be removed.");
    }

    [Function("CheckSubscriptionStatus")]
    public async Task<HttpResponseData> CheckSubscriptionStatus([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("Received check subscription request");

            string? nhsNumber = req.Query["nhsNumber"];

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
            return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.InternalServerError, req, "An error occurred while checking subscription status.");
        }
    }

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
            return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.InternalServerError, req, "An error occurred while processing the data service request.");
        }
    }

    [Function("PollMeshMailbox")]
    public async Task RunAsync([TimerTrigger("59 23 * * *")] TimerInfo myTimer)
    {
        await _meshPoller.ExecuteHandshake(_config.CaasFromMailbox!);
    }


}
