using System.Runtime.CompilerServices;
using Castle.Core.Logging;
using Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ReconciliationServiceCore;

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
        _inboundMetricClient = new InboundMetricClient(_mockLogger.Object, _mockQueueClient.Object, _mockConfig.Object);

    }
    [TestMethod]
    public void TestMethod1()
    {
    }
}
