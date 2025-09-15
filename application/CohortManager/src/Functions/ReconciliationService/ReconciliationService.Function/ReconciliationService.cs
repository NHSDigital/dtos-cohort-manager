namespace NHS.CohortManager.ReconciliationService;

using System;
using System.Net;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Common;
using DataServices.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;
using ReconciliationServiceCore;

public class ReconciliationService
{
    private readonly ILogger<ReconciliationService> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IRequestHandler<InboundMetric> _inboundMetricRequestHandler;
    private readonly IDataServiceAccessor<InboundMetric> _inboundMetricDataServiceAccessor;
    private readonly IReconciliationProcessor _reconciliationProcessor;
    private readonly IStateStore _stateStore;

    public ReconciliationService(ILogger<ReconciliationService> logger,
        ICreateResponse createResponse,
        IRequestHandler<InboundMetric> inboundMetricRequestHandler,
        IDataServiceAccessor<InboundMetric> inboundMetricDataServiceAccessor,
        IReconciliationProcessor reconciliationProcessor,
        IStateStore stateStore)
    {
        _logger = logger;
        _createResponse = createResponse;
        _inboundMetricRequestHandler = inboundMetricRequestHandler;
        _inboundMetricDataServiceAccessor = inboundMetricDataServiceAccessor;
        _reconciliationProcessor = reconciliationProcessor;
        _stateStore = stateStore;
    }
    /// <summary>
    /// Service Bus triggered Function for inbound Metrics
    /// This receives details of a metric to be reconciled i.e. number of participants expected and will log is to the database
    /// </summary>
    /// <param name="message">Message in the format of InboundMetricRequest</param>
    /// <param name="messageActions">Service Bus Actions to be performed on the received message</param>
    /// <returns></returns>
    [Function("InboundMetricsTracker")]
    public async Task RunInboundMetric(
        [ServiceBusTrigger("%inboundMetricTopic%", "%ReconciliationServiceSubscription%", Connection = "ServiceBusConnectionString_internal")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {

        var metric = message.Body.ToObjectFromJson<InboundMetricRequest>();

        if (metric == null)
        {
            _logger.LogError("Metric message was empty");
            await messageActions.DeadLetterMessageAsync(message);
            return;
        }
        var inboundMetric = new InboundMetric
        {
            MetricAuditId = Guid.NewGuid(),
            ProcessName = metric.AuditProcess,
            ReceivedDateTime = metric.ReceivedDateTime,
            Source = metric.Source,
            RecordCount = metric.RecordCount

        };

        var result = await _inboundMetricDataServiceAccessor.InsertSingle(inboundMetric);

        if (!result)
        {
            _logger.LogWarning("Metric failed to add to the database, Message will be deferred");
            await messageActions.DeferMessageAsync(message);
            return;
        }


        await messageActions.CompleteMessageAsync(message);

    }
    /// <summary>
    /// Reconcile Participants will validate that all participants received within a set window matches the number expected (Sent via inbound metrics)
    /// </summary>
    /// <param name="myTimer">Timer trigger data</param>
    /// <returns></returns>
    [Function("ReconcileParticipants")]
    public async Task RunReconciliation([TimerTrigger("%ReconciliationTimer%")] TimerInfo myTimer)
    {
        DateTime lastRun;
        var state = await _stateStore.GetState<ReconciliationRunState>("ReconciliationRunState");

        if (state == null)
        {
            lastRun = DateTime.UtcNow.AddHours(-24);
        }
        else
        {
            lastRun = state.LastRun;
        }

        var runtime = DateTime.UtcNow;

        await _reconciliationProcessor.RunReconciliation(lastRun);

        await _stateStore.SetState<ReconciliationRunState>("ReconciliationRunState", new ReconciliationRunState
        {
            LastRun = runtime
        });



    }
    /// <summary>
    /// Data Service function for inbound metric database table
    /// </summary>
    /// <param name="req"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    [Function("InboundMetricDataService")]
    public async Task<HttpResponseData> RunInboundMetricDataService([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = "InboundMetricDataService/{*key}")] HttpRequestData req, string? key)
    {
        try
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var result = await _inboundMetricRequestHandler.HandleRequest(req, key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error has occurred ");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, $"An error has occurred {ex.Message}");
        }
    }


}
