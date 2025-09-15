namespace ReconciliationServiceTests;

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Common;
using DataServices.Client;
using DataServices.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using Moq;
using NHS.CohortManager.ReconciliationService;
using NHS.CohortManager.ReconciliationServiceCore;

[TestClass]
public sealed class ReconciliationFunctionTests
{
    private readonly Mock<ILogger<ReconciliationService>> _mockLogger = new();
    private readonly Mock<ICreateResponse> _mockCreateResponse = new();
    private readonly Mock<IRequestHandler<InboundMetric>> _mockRequestHandler = new();
    private readonly Mock<IDataServiceAccessor<InboundMetric>> _mockInboundMetricAccessor = new();
    private readonly Mock<IDataServiceClient<CohortDistribution>> _mockCohortDistributionData = new();
    private readonly Mock<IDataServiceClient<ExceptionManagement>> _mockExceptionDataService = new();
    private readonly Mock<ServiceBusMessageActions> _mockMessageActions = new();
    private readonly ReconciliationService _reconciliationService;
    private readonly Mock<IReconciliationProcessor> _mockReconciliationProcessor = new();
    private readonly Mock<IStateStore> _mockStateStore = new();

    public ReconciliationFunctionTests()
    {
        _reconciliationService = new ReconciliationService(
            _mockLogger.Object,
            _mockCreateResponse.Object,
            _mockRequestHandler.Object,
            _mockInboundMetricAccessor.Object,
            _mockReconciliationProcessor.Object,
            _mockStateStore.Object);

    }
    [TestMethod]
    public async Task LogInboundMetric_SendNormalMetric_MessageCompleted()
    {
        //arrange
        var message = ServiceBusTestHelper.CreateServiceBusMessage<InboundMetricRequest>(new InboundMetricRequest
        {
            AuditProcess = "process",
            ReceivedDateTime = DateTime.UtcNow,
            Source = "Source",
            RecordCount = 123
        });

        _mockInboundMetricAccessor.Setup(x => x.InsertSingle(It.IsAny<InboundMetric>())).ReturnsAsync(true);
        //act
        await _reconciliationService.RunInboundMetric(message, _mockMessageActions.Object);

        //assert
        _mockMessageActions.Verify(x => x.CompleteMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockMessageActions.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task LogInboundMetric_CannotWriteToDatabase_MessageDeferred()
    {
        //arrange
        var message = ServiceBusTestHelper.CreateServiceBusMessage<InboundMetricRequest>(new InboundMetricRequest
        {
            AuditProcess = "process",
            ReceivedDateTime = DateTime.UtcNow,
            Source = "Source",
            RecordCount = 123
        });

        _mockInboundMetricAccessor.Setup(x => x.InsertSingle(It.IsAny<InboundMetric>())).ReturnsAsync(false);

        //act
        await _reconciliationService.RunInboundMetric(message, _mockMessageActions.Object);

        //assert
        _mockMessageActions.Verify(x => x.DeferMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), null, It.IsAny<CancellationToken>()), Times.Once);
        _mockMessageActions.VerifyNoOtherCalls();
    }
    [TestMethod]
    public async Task LogInboundMetric_NullMessage_MessageDeadLettered()
    {
        //arrange
        var message = ServiceBusTestHelper.CreateServiceBusMessage<InboundMetricRequest>(null);

        _mockInboundMetricAccessor.Setup(x => x.InsertSingle(It.IsAny<InboundMetric>())).ReturnsAsync(false);

        //act
        await _reconciliationService.RunInboundMetric(message, _mockMessageActions.Object);

        //assert
        _mockMessageActions.Verify(x => x.DeadLetterMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), null, null, null, It.IsAny<CancellationToken>()), Times.Once);
        _mockMessageActions.VerifyNoOtherCalls();
    }

}
