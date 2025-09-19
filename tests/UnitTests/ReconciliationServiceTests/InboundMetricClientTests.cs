
using System.Threading.Tasks;
using Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NHS.CohortManager.ReconciliationServiceCore;

namespace ReconciliationServiceTests;

[TestClass]
public sealed class InboundMetricClientTests
{
    private readonly Mock<ILogger<InboundMetricClient>> _mockLogger = new();
    private readonly Mock<IQueueClient> _mockQueueClient = new();
    private readonly Mock<IOptions<InboundMetricClientConfig>> _mockConfig = new();

    private readonly InboundMetricClient _inboundMetricClient;

    public InboundMetricClientTests()
    {
        _mockConfig.Setup(x => x.Value).Returns(new InboundMetricClientConfig
        {
            InboundMetricTopic = "TopicName",
            ServiceBusConnectionString_client_internal = "connectionString"
        });
        _inboundMetricClient = new InboundMetricClient(_mockLogger.Object, _mockQueueClient.Object, _mockConfig.Object);

    }
    [TestMethod]
    public async Task LogInboundMetric_SendNormalMetric_ReturnsTrue()
    {
        //arrange
        _mockQueueClient.Setup(x => x.AddAsync<InboundMetricRequest>(It.IsAny<InboundMetricRequest>(), It.IsAny<string>())).ReturnsAsync(true);
        //act
        var result = await _inboundMetricClient.LogInboundMetric("Source", 123);

        //assert
        Assert.IsTrue(result);
    }
    [TestMethod]
    public async Task LogInboundMetric_CannotAddToQueue_ReturnsFalse()
    {
        //arrange
        _mockQueueClient.Setup(x => x.AddAsync<InboundMetricRequest>(It.IsAny<InboundMetricRequest>(), It.IsAny<string>())).ReturnsAsync(false);
        //act
        var result = await _inboundMetricClient.LogInboundMetric("Source", 123);

        //assert
        Assert.IsFalse(result);
    }
}
