namespace NHS.CohortManager.Tests.CohortDistributionServiceTests;

using Common;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Model;
using Model.Enums;
using Moq;
using NHS.CohortManager.CohortDistributionServices;

[TestClass]
public class DistributeParticipantTests
{
    private readonly DistributeParticipant _sut;
    private readonly Mock<IOptions<DistributeParticipantConfig>> _config = new();
    private readonly Mock<IExceptionHandler> _handleException = new();
    private readonly Mock<TaskOrchestrationContext> _mockContext = new();
    private readonly BasicParticipantData  _request;
    private readonly CohortDistributionParticipant _cohortDistributionRecord;

    public DistributeParticipantTests()
    {
        _request = new()
        {
            Source = "testfile",
            RecordType = "ADD",
            NhsNumber = "122345",
            ScreeningId = "1"

        };

        _cohortDistributionRecord = new()
        {
            ParticipantId = "1234",
            NhsNumber = "122345",
            ScreeningId = "Screening123",
            Postcode = "AB1 2CD"
        };

        DistributeParticipantConfig config = new()
        {
            LookupValidationURL = "LookupValidationURL",
            StaticValidationURL = "StaticValidationURL",
            TransformDataServiceURL = "TransformDataServiceURL",
            ParticipantManagementUrl = "ParticipantManagementUrl",
            CohortDistributionDataServiceUrl = "CohortDistributionDataServiceUrl",
            ParticipantDemographicDataServiceUrl = "ParticipantDemographicDataServiceUrl",
            CohortDistributionTopic = "cohort-distribution-topic",
            DistributeParticipantSubscription = "distribute-participant-sub",
            RemoveOldValidationRecordUrl = "RemoveOldValidationRecordUrl",
            SendServiceNowMessageURL = "SendServiceNowMessageURL"
        };

        _config.Setup(x => x.Value).Returns(config);

        _mockContext
            .Setup(x => x.GetInput<BasicParticipantData >())
            .Returns(_request);

        _mockContext
            .Setup(x => x.CallActivityAsync<CohortDistributionParticipant?>("RetrieveParticipantData", It.IsAny<BasicParticipantData>(), null))
            .ReturnsAsync(_cohortDistributionRecord);

        _mockContext
            .Setup(x => x.CallActivityAsync<string>("AllocateServiceProvider", It.IsAny<Participant>(), null))
            .ReturnsAsync("BS SELECT");

        _mockContext
            .Setup(x => x.CallSubOrchestratorAsync<CohortDistributionParticipant?>("ValidationOrchestrator", It.IsAny<ValidationRecord>(), null))
            .ReturnsAsync(_cohortDistributionRecord);

        _mockContext
            .Setup(x => x.CallActivityAsync<bool>("AddParticipant", It.IsAny<CohortDistributionParticipant>(), null))
            .ReturnsAsync(true);

        _sut = new(NullLogger<DistributeParticipant>.Instance,
                  _config.Object,
                  _handleException.Object);
    }

    [TestMethod]
    public async Task DistributeParticipantOrchestrator_ValidRequest_AddParticipantAndDoesNotSendServiceNowMessage()
    {
        // Act
        await _sut.DistributeParticipantOrchestrator(_mockContext.Object);

        // Assert
        _mockContext
            .Verify(x => x.CallActivityAsync<bool>("AddParticipant", It.IsAny<CohortDistributionParticipant>(), null));
        _mockContext
            .Verify(x => x.CallActivityAsync("SendServiceNowMessage", It.IsAny<string>(), null), Times.Never());
    }

    [TestMethod]
    public async Task DistributeParticipantOrchestrator_ValidRequestForReferredParticipant_AddParticipantAndSendsServiceNowMessage()
    {
        // Arrange
        var caseNumber = "CS123";
        _request.ReferralFlag = true;
        _request.Source = caseNumber;

        // Act
        await _sut.DistributeParticipantOrchestrator(_mockContext.Object);

        // Assert
        _mockContext
            .Verify(x => x.CallActivityAsync<bool>("AddParticipant", It.IsAny<CohortDistributionParticipant>(), null));
        _mockContext
            .Verify(x => x.CallActivityAsync("SendServiceNowMessage", caseNumber, null), Times.Once());
    }

    [TestMethod]
    public async Task DistributeParticipantOrchestrator_RetreiveParticipantDataReturnsNull_CreateException()
    {
        // Arrange
        _mockContext
            .Setup(x => x.CallActivityAsync<CohortDistributionParticipant?>("RetrieveParticipantData", It.IsAny<BasicParticipantData>(), null))
            .ReturnsAsync((CohortDistributionParticipant)null);

        // Act
        await _sut.DistributeParticipantOrchestrator(_mockContext.Object);

        // Assert
        _mockContext
            .Verify(x => x.CallActivityAsync<bool>("AddParticipant", It.IsAny<CohortDistributionParticipant>(), null), Times.Never);
        _handleException
            .Verify(x => x.CreateSystemExceptionLog(
                It.IsAny<KeyNotFoundException>(),
                It.IsAny<BasicParticipantData>(),
                ExceptionCategory.Non
            ));
    }

    [TestMethod]
    public async Task DistributeParticipantOrchestrator_ParticipantHasExistingException_CreateException()
    {
        // Arrange
        _cohortDistributionRecord.ExceptionFlag = 1;

        // Act
        await _sut.DistributeParticipantOrchestrator(_mockContext.Object);

        // Assert
        _mockContext
            .Verify(x => x.CallActivityAsync<bool>("AddParticipant", It.IsAny<CohortDistributionParticipant>(), null), Times.Never);
        _handleException
            .Verify(x => x.CreateSystemExceptionLog(
                It.IsAny<ArgumentException>(),
                It.IsAny<BasicParticipantData>(),
                ExceptionCategory.Non
            ));
    }

    [TestMethod]
    public async Task DistributeParticipantOrchestrator_ValidationReturnsNull_ReturnEarly()
    {
        // Arrange
        _mockContext
            .Setup(x => x.CallSubOrchestratorAsync<CohortDistributionParticipant?>("ValidationOrchestrator", It.IsAny<ValidationRecord>(), null))
            .ReturnsAsync((CohortDistributionParticipant)null);

        // Act
        await _sut.DistributeParticipantOrchestrator(_mockContext.Object);

        // Assert
        _mockContext
            .Verify(x => x.CallActivityAsync<bool>("AddParticipant", It.IsAny<CohortDistributionParticipant>(), null), Times.Never);
    }

    [TestMethod]
    public async Task DistributeParticipantOrchestrator_AddFails_CreateException()
    {
        // Arrange
        _mockContext
            .Setup(x => x.CallActivityAsync<bool>("AddParticipant", It.IsAny<CohortDistributionParticipant>(), null))
            .ReturnsAsync(false);

        // Act
        await _sut.DistributeParticipantOrchestrator(_mockContext.Object);

        // Assert
        _mockContext
            .Verify(x => x.CallActivityAsync<bool>("AddParticipant", It.IsAny<CohortDistributionParticipant>(), null));
        _handleException
            .Verify(x => x.CreateSystemExceptionLog(
                It.IsAny<InvalidOperationException>(),
                It.IsAny<BasicParticipantData>(),
                ExceptionCategory.Non
            ));
    }

    [TestMethod]
    public async Task DistributeParticipantOrchestrator_ActivityThrowsException_CreateException()
    {
        // Arrange
        _mockContext
            .Setup(x => x.CallActivityAsync<bool>("AddParticipant", It.IsAny<CohortDistributionParticipant>(), null))
            .ThrowsAsync(new InvalidOperationException());

        // Act
        await _sut.DistributeParticipantOrchestrator(_mockContext.Object);

        // Assert
        _mockContext
            .Verify(x => x.CallActivityAsync<bool>("AddParticipant", It.IsAny<CohortDistributionParticipant>(), null));
        _handleException
            .Verify(x => x.CreateSystemExceptionLog(
                It.IsAny<Exception>(),
                It.IsAny<BasicParticipantData>(),
                ExceptionCategory.Non
            ));
    }
}
