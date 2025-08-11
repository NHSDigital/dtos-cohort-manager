namespace NHS.CohortManager.Tests.CohortDistributionServiceTests;

using Common;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Model;
using Moq;
using NHS.CohortManager.CohortDistributionServices;

[TestClass]
public class DistributeParticipantTests
{
    private readonly DistributeParticipant _sut;
    private readonly Mock<IOptions<DistributeParticipantConfig>> _config = new();
    private readonly Mock<IExceptionHandler> _handleException = new();
    private readonly Mock<IHttpClientFunction> _httpClientFunction = new();
    private readonly Mock<TaskOrchestrationContext> _mockContext = new();
    private readonly BasicParticipantCsvRecord _request;
    private readonly CohortDistributionParticipant _cohortDistributionRecord;

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
                Postcode = "T35 7ING"
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
                  _handleException.Object,
                  _httpClientFunction.Object);
    }

    [TestMethod]
    public async Task DistributeParticipantOrchestrator_ValidRequest_AddParticipantAndDoesNotSendServiceNowMessage()
    {
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
    public async Task DistributeParticipantOrchestrator_ServiceNowParticipantWithDummyGpCode_CallsUpdateGpCodeActivity(string dummyGpCode)
    {
        // Arrange
        _request.Participant.ReferralFlag = "1";
        _request.Participant.Postcode = dummyGpCode;
        _request.Participant.ParticipantId = "1234";

        _mockContext
            .Setup(x => x.CallActivityAsync("UpdateCohortDistributionGpCode", It.IsAny<object>(), null))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.DistributeParticipantOrchestrator(_mockContext.Object);

        // Assert
        _mockContext.Verify(x => x.CallActivityAsync<bool>("AddParticipant", It.IsAny<CohortDistributionParticipant>(), null), Times.Once);
        _mockContext.Verify(x => x.CallActivityAsync("UpdateCohortDistributionGpCode",
            It.Is<object>(req =>
                req.GetType().GetProperty("NhsNumber")!.GetValue(req)!.ToString() == "122345" &&
                req.GetType().GetProperty("ParticipantId")!.GetValue(req)!.ToString() == "1234" &&
                req.GetType().GetProperty("PrimaryCareProvider")!.GetValue(req)!.ToString() == dummyGpCode &&
                (bool)req.GetType().GetProperty("IsAmendParticipant")!.GetValue(req)! == false
            ), null), Times.Once);
        _mockContext.Verify(x => x.CallActivityAsync("SendServiceNowMessage", It.IsAny<string>(), null), Times.Once);
    }

    [TestMethod]
    [DataRow("ABC123")]
    [DataRow("")]
    [DataRow(null)]
    public async Task DistributeParticipantOrchestrator_ServiceNowParticipantWithoutDummyGpCode_DoesNotCallUpdateGpCodeActivity(string nonDummyGpCode)
    {
        // Arrange
        _request.Participant.ReferralFlag = "1";
        _request.Participant.Postcode = nonDummyGpCode;

        // Act
        await _sut.DistributeParticipantOrchestrator(_mockContext.Object);

        // Assert
        _mockContext.Verify(x => x.CallActivityAsync<bool>("AddParticipant", It.IsAny<CohortDistributionParticipant>(), null), Times.Once);
        _mockContext.Verify(x => x.CallActivityAsync("UpdateCohortDistributionGpCode", It.IsAny<object>(), null), Times.Never);
        _mockContext.Verify(x => x.CallActivityAsync("SendServiceNowMessage", It.IsAny<string>(), null), Times.Once);
    }

    [TestMethod]
    [DataRow("REAL123")]
    [DataRow("NEW456")]
    public async Task DistributeParticipantOrchestrator_PdsParticipantWithGpCode_CallsUpdateGpCodeActivity(string pdsGpCode)
    {
        // Arrange
        _request.Participant.ReferralFlag = "0";
        _request.Participant.Postcode = pdsGpCode;
        _request.Participant.ParticipantId = "1234";

        _mockContext
            .Setup(x => x.CallActivityAsync("UpdateCohortDistributionGpCode", It.IsAny<object>(), null))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.DistributeParticipantOrchestrator(_mockContext.Object);

        // Assert
        _mockContext.Verify(x => x.CallActivityAsync<bool>("AddParticipant", It.IsAny<CohortDistributionParticipant>(), null), Times.Once);
        _mockContext.Verify(x => x.CallActivityAsync("UpdateCohortDistributionGpCode",
            It.Is<object>(req =>
                req.GetType().GetProperty("NhsNumber")!.GetValue(req)!.ToString() == "122345" &&
                req.GetType().GetProperty("ParticipantId")!.GetValue(req)!.ToString() == "1234" &&
                req.GetType().GetProperty("PrimaryCareProvider")!.GetValue(req)!.ToString() == pdsGpCode &&
                (bool)req.GetType().GetProperty("IsAmendParticipant")!.GetValue(req)! == true
            ), null), Times.Once);
        _mockContext.Verify(x => x.CallActivityAsync("SendServiceNowMessage", It.IsAny<string>(), null), Times.Never);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(null)]
    public async Task DistributeParticipantOrchestrator_PdsParticipantWithoutGpCode_DoesNotCallUpdateGpCodeActivity(string emptyGpCode)
    {
        // Arrange
        _request.Participant.ReferralFlag = "0";
        _request.Participant.Postcode = emptyGpCode;

        // Act
        await _sut.DistributeParticipantOrchestrator(_mockContext.Object);

        // Assert
        _mockContext.Verify(x => x.CallActivityAsync<bool>("AddParticipant", It.IsAny<CohortDistributionParticipant>(), null), Times.Once);
        _mockContext.Verify(x => x.CallActivityAsync("UpdateCohortDistributionGpCode", It.IsAny<object>(), null), Times.Never);
        _mockContext.Verify(x => x.CallActivityAsync("SendServiceNowMessage", It.IsAny<string>(), null), Times.Never);
    }

    [TestMethod]
    public async Task DistributeParticipantOrchestrator_ServiceNowParticipantWithDummyGpCodeAndSendMessage_CallsBothActivities()
    {
        // Arrange
        var caseNumber = "CS123";
        _request.Participant.ReferralFlag = "1";
        _request.FileName = caseNumber;
        _request.Participant.Postcode = "ZZZ999";
        _request.Participant.ParticipantId = "1234";

        _mockContext
            .Setup(x => x.CallActivityAsync("UpdateCohortDistributionGpCode", It.IsAny<object>(), null))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.DistributeParticipantOrchestrator(_mockContext.Object);

        // Assert
        _mockContext.Verify(x => x.CallActivityAsync<bool>("AddParticipant", It.IsAny<CohortDistributionParticipant>(), null), Times.Once);
        _mockContext.Verify(x => x.CallActivityAsync("UpdateCohortDistributionGpCode", It.IsAny<object>(), null), Times.Once);
        _mockContext.Verify(x => x.CallActivityAsync("SendServiceNowMessage", caseNumber, null), Times.Once);
    }
}
