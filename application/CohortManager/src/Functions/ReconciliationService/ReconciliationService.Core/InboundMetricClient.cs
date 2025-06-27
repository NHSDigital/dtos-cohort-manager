namespace ReconciliationServiceCore;

using System.Threading.Tasks;
using Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class InboundMetricClient : IInboundMetricClient
{
    private readonly ILogger<InboundMetricClient> _logger;
    private readonly IQueueClient _queueClient;
    private readonly InboundMetricClientConfig _config;
    public InboundMetricClient(ILogger<InboundMetricClient> logger, IQueueClient queueClient, IOptions<InboundMetricClientConfig> config)
    {
        _logger = logger;
        _queueClient = queueClient;
        _config = config.Value;
    }

    public async Task<bool> LogInboundMetric(string source, int recordCount)
    {
        var metricRequest = new InboundMetricRequest
        {
            AuditProcess = "AuditProcess",
            ReceivedDateTime = DateTime.UtcNow,
            Source = source,
            RecordCount = recordCount
        };

        return await _queueClient.AddAsync<InboundMetricRequest>(metricRequest, _config.InboundMetricTopic);
    }
}
