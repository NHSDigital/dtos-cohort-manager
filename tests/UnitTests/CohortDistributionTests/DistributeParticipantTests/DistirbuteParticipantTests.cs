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
    private readonly Mock<TaskOrchestrationContext> _mockContext = new();
    private BasicParticipantCsvRecord _request;
    private CohortDistributionParticipant _cohortDistributionRecord;

    public DistributeParticipantTests()
    {
        _request = new()
        {
            FileName = "testfile",
            BasicParticipantData = new()
            {
                RecordType = "ADD",
                NhsNumber = "122345",
                ScreeningId = "1"
            },
            Participant = new()
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
            ParticipantDemographicDataServiceUrl = "ParticipantDemographicDataServiceUrl"
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
    public async Task DistributeParticipantOrchestrator_ValidRequest_AddParticipant()
    {
        // Act
        await _sut.DistributeParticipantOrchestrator(_mockContext.Object);

        // Assert
        _mockContext
            .Verify(x => x.CallActivityAsync<bool>("AddParticipant", It.IsAny<CohortDistributionParticipant>(), null));
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
}