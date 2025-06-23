namespace NHS.CohortManager.Tests.UnitTests.ServiceNowCohortLookupTests;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using Moq;
using DataServices.Client;
using NHS.CohortManager.ServiceNowIntegrationService;
using System.Linq.Expressions;
using Microsoft.Azure.Functions.Worker;

[TestClass]
public class ServiceNowCohortLookupTests
{
    private Mock<ILogger<ServiceNowCohortLookup>> _loggerMock;
    private Mock<IDataServiceClient<CohortDistribution>> _cohortDistributionClientMock;
    private Mock<IDataServiceClient<ServicenowCases>> _serviceNowCasesClientMock;
    private ServiceNowCohortLookup _service;
    private TimerInfo _timerInfo;
    private const string ValidNhsNumber = "3112728165";
    private const string ServiceNowId = "SN12345";

    [TestInitialize]
    public void Initialize()
    {
        _loggerMock = new Mock<ILogger<ServiceNowCohortLookup>>();
        _cohortDistributionClientMock = new Mock<IDataServiceClient<CohortDistribution>>();
        _serviceNowCasesClientMock = new Mock<IDataServiceClient<ServicenowCases>>();

        var config = Options.Create(new ServiceNowCohortLookupConfig
        {
            CohortDistributionDataServiceURL = "CohortDistributionDataServiceURL",
            ServiceNowCasesDataServiceURL = "ServiceNowCasesDataServiceURL"
        });
        _service = new ServiceNowCohortLookup(
            _loggerMock.Object,
            _cohortDistributionClientMock.Object,
            _serviceNowCasesClientMock.Object);

        _timerInfo = new TimerInfo
        {
            ScheduleStatus = new ScheduleStatus
            {
                Last = DateTime.Now.AddDays(-1),
                Next = DateTime.Now.AddDays(1),
                LastUpdated = DateTime.Now
            },
            IsPastDue = false
        };
    }

    [TestMethod]
    public async Task Run_WithNewServiceNowCases_FoundInCohort_UpdatesStatusToComplete()
    {
        // Arrange
        var validNhsNumberLong = long.Parse(ValidNhsNumber);
        var newCase = new ServicenowCases
        {
            ServicenowId = ServiceNowId,
            NhsNumber = validNhsNumberLong,
            Status = ServiceNowStatus.New,
            RecordUpdateDatetime = DateTime.Now
        };

        var cohortParticipant = new CohortDistribution { NHSNumber = validNhsNumberLong };

        _serviceNowCasesClientMock.Setup(x =>
                x.GetByFilter(It.IsAny<Expression<Func<ServicenowCases, bool>>>()))
            .ReturnsAsync(new List<ServicenowCases> { newCase });

        _cohortDistributionClientMock.Setup(x =>
                x.GetSingleByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()))
            .ReturnsAsync(cohortParticipant);

        _serviceNowCasesClientMock.Setup(x =>
                x.Update(It.Is<ServicenowCases>(c =>
                    c.ServicenowId == ServiceNowId && c.Status == ServiceNowStatus.Complete)))
            .ReturnsAsync(true);

        // Act
        await _service.Run(_timerInfo);

        // Assert
        _serviceNowCasesClientMock.Verify(x => x.Update(It.Is<ServicenowCases>(c => c.Status == ServiceNowStatus.Complete && c.ServicenowId == ServiceNowId)), Times.Once);
    }

    [TestMethod]
    public async Task Run_WithNoNewServiceNowCases_DoesNothing()
    {
        // Arrange
        _serviceNowCasesClientMock.Setup(x =>
                x.GetByFilter(It.IsAny<Expression<Func<ServicenowCases, bool>>>()))
            .ReturnsAsync(new List<ServicenowCases>());

        // Act
        await _service.Run(_timerInfo);

        // Assert
        _serviceNowCasesClientMock.Verify(x =>
            x.Update(It.IsAny<ServicenowCases>()),
            Times.Never);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("0 servicenow cases")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Run_WhenParticipantNotFound_DoesNotUpdateStatus()
    {
        // Arrange
        var validNhsNumberLong = long.Parse(ValidNhsNumber);
        var newCase = new ServicenowCases
        {
            ServicenowId = ServiceNowId,
            NhsNumber = validNhsNumberLong,
            Status = ServiceNowStatus.New,
            RecordUpdateDatetime = DateTime.Now
        };

        _serviceNowCasesClientMock.Setup(x =>
                x.GetByFilter(It.IsAny<Expression<Func<ServicenowCases, bool>>>()))
            .ReturnsAsync(new List<ServicenowCases> { newCase });

        _cohortDistributionClientMock.Setup(x =>
                x.GetSingleByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()))
            .ReturnsAsync((CohortDistribution)null);

        // Act
        await _service.Run(_timerInfo);

        // Assert
        _serviceNowCasesClientMock.Verify(x =>
            x.Update(It.IsAny<ServicenowCases>()),
            Times.Never);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No participant found")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Run_WhenUpdateFails_LogsError()
    {
        // Arrange
        var validNhsNumberLong = long.Parse(ValidNhsNumber);
        var newCase = new ServicenowCases
        {
            ServicenowId = ServiceNowId,
            NhsNumber = validNhsNumberLong,
            Status = ServiceNowStatus.New,
            RecordUpdateDatetime = DateTime.Now
        };

        var cohortParticipant = new CohortDistribution { NHSNumber = validNhsNumberLong };

        _serviceNowCasesClientMock.Setup(x =>
                x.GetByFilter(It.IsAny<Expression<Func<ServicenowCases, bool>>>()))
            .ReturnsAsync(new List<ServicenowCases> { newCase });

        _cohortDistributionClientMock.Setup(x =>
                x.GetSingleByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()))
            .ReturnsAsync(cohortParticipant);

        _serviceNowCasesClientMock.Setup(x =>
                x.Update(It.IsAny<ServicenowCases>()))
            .ReturnsAsync(false);

        // Act
        await _service.Run(_timerInfo);

        // Assert
        _loggerMock.Verify(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to process servicenow case")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Run_WhenExceptionOccurs_LogsError()
    {
        // Arrange
        _serviceNowCasesClientMock.Setup(x =>
                x.GetByFilter(It.IsAny<Expression<Func<ServicenowCases, bool>>>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        await _service.Run(_timerInfo);

        // Assert
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to process ServiceNow cohort lookup")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Run_WithMultipleServiceNowCases_ProcessesAll()
    {
        // Arrange
        var case1 = new ServicenowCases
        {
            ServicenowId = "SN1",
            NhsNumber = 123,
            Status = ServiceNowStatus.New,
            RecordUpdateDatetime = DateTime.Now
        };

        var case2 = new ServicenowCases
        {
            ServicenowId = "SN2",
            NhsNumber = 456,
            Status = ServiceNowStatus.New,
            RecordUpdateDatetime = DateTime.Now
        };

        _serviceNowCasesClientMock.Setup(x =>
                x.GetByFilter(It.IsAny<Expression<Func<ServicenowCases, bool>>>()))
            .ReturnsAsync(new List<ServicenowCases> { case1, case2 });

        // Mock successful lookup for case1
        _cohortDistributionClientMock.Setup(x =>
                x.GetSingleByFilter(It.Is<Expression<Func<CohortDistribution, bool>>>(f =>
                    f.Compile()(new CohortDistribution { NHSNumber = 123 }))))
            .ReturnsAsync(new CohortDistribution { NHSNumber = 123 });

        // Mock failed lookup for case2
        _cohortDistributionClientMock.Setup(x =>
                x.GetSingleByFilter(It.Is<Expression<Func<CohortDistribution, bool>>>(f =>
                    f.Compile()(new CohortDistribution { NHSNumber = 456 }))))
            .ReturnsAsync((CohortDistribution)null);

        // Mock successful update for case1
        _serviceNowCasesClientMock.Setup(x =>
                x.Update(It.Is<ServicenowCases>(c => c.ServicenowId == "SN1")))
            .ReturnsAsync(true);

        await _service.Run(_timerInfo);

        // Assert
        // Verify case1 was updated
        _serviceNowCasesClientMock.Verify(x =>
            x.Update(It.Is<ServicenowCases>(c =>
                c.ServicenowId == "SN1" &&
                c.Status == ServiceNowStatus.Complete)),
            Times.Once);

        // Verify case2 was not updated
        _serviceNowCasesClientMock.Verify(x =>
            x.Update(It.Is<ServicenowCases>(c => c.ServicenowId == "SN2")),
            Times.Never);

        // Verify logging
        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Updated servicenow case SN1")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No participant found")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

}
