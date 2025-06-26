namespace ReconciliationService;

using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Common;
using DataServices.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;

public class ReconciliationService
{
    private readonly ILogger<ReconciliationService> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IRequestHandler<InboundMetric> _inboundMetricRequestHandler;

    public ReconciliationService(ILogger<ReconciliationService> logger,
        ICreateResponse createResponse,
        IRequestHandler<InboundMetric> inboundMetricRequestHandler)
    {
        _logger = logger;
        _createResponse = createResponse;
        _inboundMetricRequestHandler = inboundMetricRequestHandler;
    }

    [Function("InboundMetricsTracker")]
    public async Task Run(
        [ServiceBusTrigger("mytopic", "mysubscription", Connection = "ServiceBusConnectionString")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation("Message ID: {id}", message.MessageId);
        _logger.LogInformation("Message Body: {body}", message.Body);
        _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

        // Complete the message
        await messageActions.CompleteMessageAsync(message);
    }

    [Function("ReconcileParticipants")]
    public async Task RunAsync([TimerTrigger("%ReconciliationTimer%")] TimerInfo myTimer)
    {
        _logger.LogInformation("Timer triggered");
        await Task.CompletedTask;
        return;
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
