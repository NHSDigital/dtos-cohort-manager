namespace NHS.CohortManager.Tests.CohortDistributionServiceTests;

using Common;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Model;
using Moq;
using NHS.CohortManager.CohortDistributionServices;
using NHS.CohortManager.Shared.Utilities;

[TestClass]
public class DistributeParticipantTests
{
    private readonly DistributeParticipant _sut;
    private readonly Mock<IOptions<DistributeParticipantConfig>> _config = new();
    private readonly Mock<IExceptionHandler> _handleException = new();
    private readonly Mock<TaskOrchestrationContext> _mockContext = new();
    private readonly BasicParticipantCsvRecord _request;
    private readonly CohortDistributionParticipant _cohortDistributionRecord;
    private readonly string _today = MappingUtilities.FormatDateTime(DateTime.UtcNow);

    public DistributeParticipantTests()
    {
        _request = new()
        {
            FileName = "testfile",
            BasicParticipantData = new()
            {
                RecordType = "ADD",
                NhsNumber = "122345",
                ScreeningId = "1",
            },
            Participant = new()
            {
                PrimaryCareProvider = "T35 7ING",
                PrimaryCareProviderEffectiveFromDate = _today
            }
        };

        _cohortDistributionRecord = new()
        {
            ParticipantId = "1234",
            NhsNumber = "122345",
            ScreeningServiceId = "Screening123",
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
            .Setup(x => x.GetInput<BasicParticipantCsvRecord>())
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
        // Arrange
        _request.Participant.ParticipantId = "1234";

        // Act
        await _sut.DistributeParticipantOrchestrator(_mockContext.Object);

        // Assert
        _mockContext.Verify(x => x.CallActivityAsync<bool>("AddParticipant", It.IsAny<CohortDistributionParticipant>(), null));
        _mockContext.Verify(x => x.CallActivityAsync("SendServiceNowMessage", It.IsAny<string>(), null), Times.Never());
    }

    [TestMethod]
    public async Task DistributeParticipantOrchestrator_ValidRequestForReferredParticipant_AddParticipantAndSendsServiceNowMessage()
    {
        // Arrange
        var caseNumber = "CS123";
        _request.Participant.ReferralFlag = "1";
        _request.FileName = caseNumber;

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
                It.IsAny<string>()
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
                It.IsAny<string>()
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
                It.IsAny<string>()
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
                It.IsAny<string>()
            ));
    }

    [TestMethod]
    [DataRow("ZZZ123")]
    [DataRow("zzz456")]
    public async Task DistributeParticipantOrchestrator_ServiceNowParticipantWithGpCode_ProcessesNormally(string gpCode)
    {
        // Arrange
        _request.Participant.ReferralFlag = "1";
        _request.Participant.PrimaryCareProvider = gpCode;
        _request.Participant.ParticipantId = "1234";

        // Act
        await _sut.DistributeParticipantOrchestrator(_mockContext.Object);

        // Assert
        _mockContext.Verify(x => x.CallActivityAsync<bool>("AddParticipant", It.IsAny<CohortDistributionParticipant>(), null), Times.Once);
        _mockContext.Verify(x => x.CallActivityAsync<bool>("UpdateCohortDistributionGpCode", It.IsAny<object>(), null), Times.Never);
        _mockContext.Verify(x => x.CallActivityAsync("SendServiceNowMessage", It.IsAny<string>(), null), Times.Once);
    }

    [TestMethod]
    [DataRow("ABC123")]
    [DataRow("")]
    [DataRow(null)]
    public async Task DistributeParticipantOrchestrator_ServiceNowParticipantWithAnyGpCode_ProcessesNormally(string gpCode)
    {
        // Arrange
        _request.Participant.ReferralFlag = "1";
        _request.Participant.PrimaryCareProvider = gpCode;

        // Act
        await _sut.DistributeParticipantOrchestrator(_mockContext.Object);

        // Assert
        _mockContext.Verify(x => x.CallActivityAsync<bool>("AddParticipant", It.IsAny<CohortDistributionParticipant>(), null), Times.Once);
        _mockContext.Verify(x => x.CallActivityAsync<bool>("UpdateCohortDistributionGpCode", It.IsAny<object>(), null), Times.Never);
        _mockContext.Verify(x => x.CallActivityAsync("SendServiceNowMessage", It.IsAny<string>(), null), Times.Once);
    }

    [TestMethod]
    public async Task DistributeParticipantOrchestrator_ServiceNowParticipant_ProcessesAndSendsMessage()
    {
        // Arrange
        var caseNumber = "CS123";
        _request.Participant.ReferralFlag = "1";
        _request.FileName = caseNumber;
        _request.Participant.PrimaryCareProvider = "ZZZ999";
        _request.Participant.ParticipantId = "1234";

        // Act
        await _sut.DistributeParticipantOrchestrator(_mockContext.Object);

        // Assert
        _mockContext.Verify(x => x.CallActivityAsync<bool>("AddParticipant", It.IsAny<CohortDistributionParticipant>(), null), Times.Once);
        _mockContext.Verify(x => x.CallActivityAsync<bool>("UpdateCohortDistributionGpCode", It.IsAny<object>(), null), Times.Never);
        _mockContext.Verify(x => x.CallActivityAsync("SendServiceNowMessage", caseNumber, null), Times.Once);
    }

    [TestMethod]
    public async Task DistributeParticipantOrchestrator_ServiceNowParticipantWithGpCode_PassesGpCodeToTransform()
    {
        // Arrange
        var expectedGpCode = "ZZZ123";
        _request.Participant.ReferralFlag = "1";
        _request.Participant.PrimaryCareProvider = expectedGpCode;

        // Act
        await _sut.DistributeParticipantOrchestrator(_mockContext.Object);

        // Assert
        _mockContext.Verify(x => x.CallSubOrchestratorAsync<CohortDistributionParticipant?>(
            "ValidationOrchestrator",
            It.Is<ValidationRecord>(vr => vr.Participant.PrimaryCareProvider == expectedGpCode &&
            vr.Participant.PrimaryCareProviderEffectiveFromDate == _today), null), Times.Once);
    }

    [TestMethod]
    public async Task DistributeParticipantOrchestrator_ServiceNowParticipantWithGpCode_UpdatesPrimaryCareProviderAndReferralFlag()
    {
        // Arrange
        var serviceNowGpCode = "ZZZ123";
        _cohortDistributionRecord.PrimaryCareProvider = "T35 7ING";
        _cohortDistributionRecord.ReferralFlag = null;
        _request.Participant.ReferralFlag = "1";
        _request.Participant.PrimaryCareProvider = serviceNowGpCode;

        // Act
        await _sut.DistributeParticipantOrchestrator(_mockContext.Object);

        // Assert
        _mockContext.Verify(x => x.CallSubOrchestratorAsync<CohortDistributionParticipant?>(
            "ValidationOrchestrator",
            It.Is<ValidationRecord>(vr =>
                vr.Participant.PrimaryCareProvider == serviceNowGpCode &&
                vr.Participant.ReferralFlag == true &&
            vr.Participant.PrimaryCareProviderEffectiveFromDate == _today),
            null), Times.Once);
    }

    [TestMethod]
    public async Task DistributeParticipantOrchestrator_NonServiceNowParticipant_DoesNotUpdate()
    {
        // Arrange
        var originalGpCode = "T35 7ING";
        _cohortDistributionRecord.PrimaryCareProvider = originalGpCode;
        _cohortDistributionRecord.ReferralFlag = null;
        _request.Participant.ReferralFlag = "0";
        _request.Participant.PrimaryCareProvider = "ZZZ123";

        // Act
        await _sut.DistributeParticipantOrchestrator(_mockContext.Object);

        // Assert
        _mockContext.Verify(x => x.CallSubOrchestratorAsync<CohortDistributionParticipant?>(
            "ValidationOrchestrator",
            It.Is<ValidationRecord>(vr =>
                vr.Participant.PrimaryCareProvider == originalGpCode &&
                vr.Participant.ReferralFlag == null),
            null), Times.Once);
    }

    [TestMethod]
    public async Task DistributeParticipantOrchestrator_ServiceNowParticipantWithoutGpCode_DoesNotUpdateData()
    {
        // Arrange
        var originalGpCode = "T35 7ING";
        _cohortDistributionRecord.PrimaryCareProvider = originalGpCode;
        _cohortDistributionRecord.ReferralFlag = null;
        _request.Participant.ReferralFlag = "1";
        _request.Participant.PrimaryCareProvider = null;

        // Act
        await _sut.DistributeParticipantOrchestrator(_mockContext.Object);

        // Assert
        _mockContext.Verify(x => x.CallSubOrchestratorAsync<CohortDistributionParticipant?>(
            "ValidationOrchestrator",
            It.Is<ValidationRecord>(vr => vr.Participant.PrimaryCareProvider == originalGpCode), null), Times.Once);
    }
}
