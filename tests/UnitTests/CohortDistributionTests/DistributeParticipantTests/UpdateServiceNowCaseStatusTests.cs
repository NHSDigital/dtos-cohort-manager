namespace NHS.CohortManager.Tests.CohortDistributionServiceTests;

using Common;
using DataServices.Client;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;
using Moq;
using NHS.CohortManager.CohortDistributionServices;
using System.Linq.Expressions;

[TestClass]
public class UpdateServiceNowCaseStatusTests
{
    private readonly Mock<IDataServiceClient<ServicenowCase>> _mockServiceNowCasesClient;
    private readonly Mock<ILogger<DistributeParticipantActivities>> _mockLogger;
    private readonly DistributeParticipantActivities _sut;
    private const string TestCaseNumber = "CS1234567";

    public UpdateServiceNowCaseStatusTests()
    {
        _mockServiceNowCasesClient = new Mock<IDataServiceClient<ServicenowCase>>();
        _mockLogger = new Mock<ILogger<DistributeParticipantActivities>>();

        // Create minimal mocks for other dependencies
        var mockCohortDistributionClient = new Mock<IDataServiceClient<CohortDistribution>>();
        var mockParticipantManagementClient = new Mock<IDataServiceClient<ParticipantManagement>>();
        var mockParticipantDemographicClient = new Mock<IDataServiceClient<ParticipantDemographic>>();
        var mockHttpClientFunction = new Mock<IHttpClientFunction>();
        var mockConfig = new Mock<Microsoft.Extensions.Options.IOptions<DistributeParticipantConfig>>();

        var config = new DistributeParticipantConfig
        {
            CohortDistributionTopic = "test-topic",
            DistributeParticipantSubscription = "test-sub",
            LookupValidationURL = "http://test",
            StaticValidationURL = "http://test",
            TransformDataServiceURL = "http://test",
            ParticipantManagementUrl = "http://test",
            CohortDistributionDataServiceUrl = "http://test",
            ParticipantDemographicDataServiceUrl = "http://test",
            RemoveOldValidationRecordUrl = "http://test",
            SendServiceNowMessageURL = "http://test",
            ServiceNowCasesDataServiceURL = "http://test"
        };

        mockConfig.Setup(x => x.Value).Returns(config);

        _sut = new DistributeParticipantActivities(
            mockCohortDistributionClient.Object,
            mockParticipantManagementClient.Object,
            mockParticipantDemographicClient.Object,
            _mockServiceNowCasesClient.Object,
            mockConfig.Object,
            _mockLogger.Object,
            mockHttpClientFunction.Object
        );
    }

    [TestMethod]
    public async Task UpdateServiceNowCaseStatus_SingleRecord_UpdatesSuccessfully()
    {
        // Arrange
        var serviceNowCase = new ServicenowCase
        {
            ServicenowId = TestCaseNumber,
            NhsNumber = 1234567890,
            Status = ServiceNowStatus.New,
            RecordInsertDatetime = DateTime.UtcNow.AddDays(-1)
        };

        _mockServiceNowCasesClient
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ServicenowCase, bool>>>()))
            .ReturnsAsync(new List<ServicenowCase> { serviceNowCase });

        _mockServiceNowCasesClient
            .Setup(x => x.Update(It.IsAny<ServicenowCase>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.UpdateServiceNowCaseStatus(TestCaseNumber);

        // Assert
        Assert.IsTrue(result);
        _mockServiceNowCasesClient.Verify(x => x.GetByFilter(It.IsAny<Expression<Func<ServicenowCase, bool>>>()), Times.Once);
        _mockServiceNowCasesClient.Verify(x => x.Update(It.Is<ServicenowCase>(c =>
            c.ServicenowId == TestCaseNumber &&
            c.Status == ServiceNowStatus.Complete &&
            c.RecordUpdateDatetime != null
        )), Times.Once);
    }

    [TestMethod]
    public async Task UpdateServiceNowCaseStatus_MultipleRecords_UpdatesAllSuccessfully()
    {
        // Arrange
        var serviceNowCases = new List<ServicenowCase>
        {
            new ServicenowCase
            {
                ServicenowId = TestCaseNumber,
                NhsNumber = 1234567890,
                Status = ServiceNowStatus.New,
                RecordInsertDatetime = DateTime.UtcNow.AddDays(-1)
            },
            new ServicenowCase
            {
                ServicenowId = TestCaseNumber,
                NhsNumber = 9876543210,
                Status = ServiceNowStatus.New,
                RecordInsertDatetime = DateTime.UtcNow.AddDays(-2)
            }
        };

        _mockServiceNowCasesClient
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ServicenowCase, bool>>>()))
            .ReturnsAsync(serviceNowCases);

        _mockServiceNowCasesClient
            .Setup(x => x.Update(It.IsAny<ServicenowCase>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.UpdateServiceNowCaseStatus(TestCaseNumber);

        // Assert
        Assert.IsTrue(result);
        _mockServiceNowCasesClient.Verify(x => x.GetByFilter(It.IsAny<Expression<Func<ServicenowCase, bool>>>()), Times.Once);
        _mockServiceNowCasesClient.Verify(x => x.Update(It.Is<ServicenowCase>(c =>
            c.ServicenowId == TestCaseNumber &&
            c.Status == ServiceNowStatus.Complete
        )), Times.Exactly(2));
    }

    [TestMethod]
    public async Task UpdateServiceNowCaseStatus_NoRecordsFound_ReturnsFalseAndLogsWarning()
    {
        // Arrange
        _mockServiceNowCasesClient
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ServicenowCase, bool>>>()))
            .ReturnsAsync(new List<ServicenowCase>());

        // Act
        var result = await _sut.UpdateServiceNowCaseStatus(TestCaseNumber);

        // Assert
        Assert.IsFalse(result);
        _mockServiceNowCasesClient.Verify(x => x.Update(It.IsAny<ServicenowCase>()), Times.Never);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task UpdateServiceNowCaseStatus_NullResult_ReturnsFalseAndLogsWarning()
    {
        // Arrange
        _mockServiceNowCasesClient
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ServicenowCase, bool>>>()))
            .ReturnsAsync((IEnumerable<ServicenowCase>)null);

        // Act
        var result = await _sut.UpdateServiceNowCaseStatus(TestCaseNumber);

        // Assert
        Assert.IsFalse(result);
        _mockServiceNowCasesClient.Verify(x => x.Update(It.IsAny<ServicenowCase>()), Times.Never);
    }

    [TestMethod]
    public async Task UpdateServiceNowCaseStatus_UpdateFails_ReturnsFalseAndLogsError()
    {
        // Arrange
        var serviceNowCase = new ServicenowCase
        {
            ServicenowId = TestCaseNumber,
            NhsNumber = 1234567890,
            Status = ServiceNowStatus.New
        };

        _mockServiceNowCasesClient
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ServicenowCase, bool>>>()))
            .ReturnsAsync(new List<ServicenowCase> { serviceNowCase });

        _mockServiceNowCasesClient
            .Setup(x => x.Update(It.IsAny<ServicenowCase>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.UpdateServiceNowCaseStatus(TestCaseNumber);

        // Assert
        Assert.IsFalse(result);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to update")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task UpdateServiceNowCaseStatus_MultipleRecords_SomeUpdatesFail_ReturnsFalse()
    {
        // Arrange
        var serviceNowCases = new List<ServicenowCase>
        {
            new ServicenowCase { ServicenowId = TestCaseNumber, NhsNumber = 1234567890, Status = ServiceNowStatus.New },
            new ServicenowCase { ServicenowId = TestCaseNumber, NhsNumber = 9876543210, Status = ServiceNowStatus.New }
        };

        _mockServiceNowCasesClient
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ServicenowCase, bool>>>()))
            .ReturnsAsync(serviceNowCases);

        // First update succeeds, second fails
        var callCount = 0;
        _mockServiceNowCasesClient
            .Setup(x => x.Update(It.IsAny<ServicenowCase>()))
            .ReturnsAsync(() => ++callCount == 1);

        // Act
        var result = await _sut.UpdateServiceNowCaseStatus(TestCaseNumber);

        // Assert
        Assert.IsFalse(result);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to update some")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task UpdateServiceNowCaseStatus_ExceptionThrown_ReturnsFalseAndLogsError()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Database error");

        _mockServiceNowCasesClient
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ServicenowCase, bool>>>()))
            .ThrowsAsync(expectedException);

        // Act
        var result = await _sut.UpdateServiceNowCaseStatus(TestCaseNumber);

        // Assert
        Assert.IsFalse(result);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error updating")),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task UpdateServiceNowCaseStatus_SetsStatusToComplete()
    {
        // Arrange
        ServicenowCase capturedCase = null;
        var serviceNowCase = new ServicenowCase
        {
            ServicenowId = TestCaseNumber,
            NhsNumber = 1234567890,
            Status = ServiceNowStatus.New
        };

        _mockServiceNowCasesClient
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ServicenowCase, bool>>>()))
            .ReturnsAsync(new List<ServicenowCase> { serviceNowCase });

        _mockServiceNowCasesClient
            .Setup(x => x.Update(It.IsAny<ServicenowCase>()))
            .Callback<ServicenowCase>(c => capturedCase = c)
            .ReturnsAsync(true);

        // Act
        await _sut.UpdateServiceNowCaseStatus(TestCaseNumber);

        // Assert
        Assert.IsNotNull(capturedCase);
        Assert.AreEqual(ServiceNowStatus.Complete, capturedCase.Status);
    }

    [TestMethod]
    public async Task UpdateServiceNowCaseStatus_UpdatesRecordUpdateDatetime()
    {
        // Arrange
        ServicenowCase capturedCase = null;
        var originalDateTime = DateTime.UtcNow.AddDays(-1);
        var serviceNowCase = new ServicenowCase
        {
            ServicenowId = TestCaseNumber,
            NhsNumber = 1234567890,
            Status = ServiceNowStatus.New,
            RecordUpdateDatetime = originalDateTime
        };

        _mockServiceNowCasesClient
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ServicenowCase, bool>>>()))
            .ReturnsAsync(new List<ServicenowCase> { serviceNowCase });

        _mockServiceNowCasesClient
            .Setup(x => x.Update(It.IsAny<ServicenowCase>()))
            .Callback<ServicenowCase>(c => capturedCase = c)
            .ReturnsAsync(true);

        // Act
        await _sut.UpdateServiceNowCaseStatus(TestCaseNumber);

        // Assert
        Assert.IsNotNull(capturedCase);
        Assert.IsNotNull(capturedCase.RecordUpdateDatetime);
        Assert.AreNotEqual(originalDateTime, capturedCase.RecordUpdateDatetime);
        Assert.IsTrue(capturedCase.RecordUpdateDatetime > originalDateTime);
    }
}
