namespace ReconciliationService;

using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Common;
using DataServices.Client;
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

    public ReconciliationService(ILogger<ReconciliationService> logger,
        ICreateResponse createResponse,
        IRequestHandler<InboundMetric> inboundMetricRequestHandler,
        IDataServiceAccessor<InboundMetric> inboundMetricDataServiceAccessor,
        IReconciliationProcessor reconciliationProcessor)
    {
        _logger = logger;
        _createResponse = createResponse;
        _inboundMetricRequestHandler = inboundMetricRequestHandler;
        _inboundMetricDataServiceAccessor = inboundMetricDataServiceAccessor;
        _reconciliationProcessor = reconciliationProcessor;
    }

    [Function("InboundMetricsTracker")]
    public async Task RunInboundMetric(
        [ServiceBusTrigger("%inboundMetricTopic%", "%inboundMetricSub%", Connection = "ServiceBusConnectionString")]
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

    [Function("ReconcileParticipants")]
    public async Task RunReconciliation([TimerTrigger("%ReconciliationTimer%")] TimerInfo myTimer)
    {
        DateTime lastRun;
        if (myTimer.ScheduleStatus is not null)
        {
            lastRun = myTimer.ScheduleStatus.Last.ToUniversalTime();
        }
        else
        {
            lastRun = DateTime.UtcNow.AddHours(-24);
        }
        await _reconciliationProcessor.RunReconciliation(lastRun);


    }

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
