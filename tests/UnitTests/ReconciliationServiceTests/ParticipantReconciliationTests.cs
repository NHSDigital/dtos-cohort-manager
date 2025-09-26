namespace ReconciliationServiceTests;

using System.Linq.Expressions;
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
using NHS.CohortManager.Tests.TestUtils;

[TestClass]
public sealed class ParticipantReconciliationTests
{
    private readonly Mock<ILogger<ParticipantReconciliation>> _mockLogger = new();
    private readonly Mock<IDataServiceAccessor<InboundMetric>> _mockInboundMetricDataServiceAccessor = new();
    private readonly Mock<IDataServiceClient<CohortDistribution>> _mockCohortDistributionDataService = new();
    private readonly Mock<IDataServiceClient<ExceptionManagement>> _mockExceptionManagementDataService = new();
    private readonly ParticipantReconciliation _participantReconciliation;


    public ParticipantReconciliationTests()
    {
        _participantReconciliation = new ParticipantReconciliation(
            _mockLogger.Object,
            _mockInboundMetricDataServiceAccessor.Object,
            _mockCohortDistributionDataService.Object,
            _mockExceptionManagementDataService.Object
        );

    }
    [TestMethod]
    public async Task RunReconciliation_ValidDataMatches_LogsDataMatches()
    {
        //arrange
        DateTime fromDate = DateTime.UtcNow.AddDays(-1);
        var cohortDistributionRecords = Enumerable.Range(1, 100)
            .Select(i => new CohortDistribution { NHSNumber = i });

        _mockCohortDistributionDataService
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()))
            .ReturnsAsync(cohortDistributionRecords);

        var exceptionRecords = Enumerable.Range(1, 25)
            .Select(i => new ExceptionManagement
            {
                NhsNumber = i.ToString()
            });

        _mockExceptionManagementDataService
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ExceptionManagement, bool>>>()))
            .ReturnsAsync(exceptionRecords);

        var metrics = new List<InboundMetric>
        {
            new InboundMetric{
                MetricAuditId = Guid.NewGuid(),
                ProcessName = "test",
                ReceivedDateTime = DateTime.UtcNow,
                Source = "source",
                RecordCount = 125

            }
        };

        _mockInboundMetricDataServiceAccessor
            .Setup(x => x.GetRange(It.IsAny<Expression<Func<InboundMetric, bool>>>()))
            .ReturnsAsync(metrics);

        //act
        var result = await _participantReconciliation.RunReconciliation(fromDate);

        //assert
        _mockLogger.Verify(
        x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) =>
                v.ToString().Contains($"Expected Records {125} equaled Records Processed {125}")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        Times.Once);
        _mockLogger.VerifyNoOtherCalls();
        Assert.IsTrue(result);

    }
    [TestMethod]
    public async Task RunReconciliation_ValidDataNoMatches_LogsDataDoesntMatch()
    {
        //arrange
        DateTime fromDate = DateTime.UtcNow.AddDays(-1);
        var cohortDistributionRecords = Enumerable.Range(1, 150)
            .Select(i => new CohortDistribution { NHSNumber = i });

        _mockCohortDistributionDataService
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()))
            .ReturnsAsync(cohortDistributionRecords);

        var exceptionRecords = Enumerable.Range(1, 25)
            .Select(i => new ExceptionManagement
            {
                NhsNumber = i.ToString()
            });

        _mockExceptionManagementDataService
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ExceptionManagement, bool>>>()))
            .ReturnsAsync(exceptionRecords);

        var metrics = new List<InboundMetric>
        {
            new InboundMetric{
                MetricAuditId = Guid.NewGuid(),
                ProcessName = "test",
                ReceivedDateTime = DateTime.UtcNow,
                Source = "source",
                RecordCount = 125

            }
        };

        _mockInboundMetricDataServiceAccessor
            .Setup(x => x.GetRange(It.IsAny<Expression<Func<InboundMetric, bool>>>()))
            .ReturnsAsync(metrics);

        //act
        var result = await _participantReconciliation.RunReconciliation(fromDate);

        //assert
        _mockLogger.Verify(
        x => x.Log(
            LogLevel.Critical,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) =>
                v.ToString().Contains($"Expected Records {125} Didn't equal Records Processed {175}")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        Times.Once);
        _mockLogger.VerifyNoOtherCalls();
        Assert.IsTrue(result);

    }
    [TestMethod]
    public async Task RunReconciliation_ValidDataMatchesDuplicateNHSNumbers_LogsDataMatches()
    {
        //arrange
        DateTime fromDate = DateTime.UtcNow.AddDays(-1);
        var cohortDistributionRecords = Enumerable.Range(1, 100)
            .Select(i => new CohortDistribution { NHSNumber = i });

        _mockCohortDistributionDataService
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()))
            .ReturnsAsync(cohortDistributionRecords);

        var exceptionRecords = Enumerable.Range(1, 25)
            .SelectMany(i => new[]
                {
                    new ExceptionManagement{NhsNumber = i.ToString()},
                    new ExceptionManagement{NhsNumber = i.ToString()}
                });

        _mockExceptionManagementDataService
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ExceptionManagement, bool>>>()))
            .ReturnsAsync(exceptionRecords);

        var metrics = new List<InboundMetric>
        {
            new InboundMetric{
                MetricAuditId = Guid.NewGuid(),
                ProcessName = "test",
                ReceivedDateTime = DateTime.UtcNow,
                Source = "source",
                RecordCount = 125

            }
        };

        _mockInboundMetricDataServiceAccessor
            .Setup(x => x.GetRange(It.IsAny<Expression<Func<InboundMetric, bool>>>()))
            .ReturnsAsync(metrics);

        //act
        var result = await _participantReconciliation.RunReconciliation(fromDate);

        //assert
        _mockLogger.Verify(
        x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) =>
                v.ToString().Contains($"Expected Records {125} equaled Records Processed {125}")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        Times.Once);
        _mockLogger.VerifyNoOtherCalls();
        Assert.IsTrue(result);

    }

}
