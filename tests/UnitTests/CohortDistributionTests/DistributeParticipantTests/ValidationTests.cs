namespace NHS.CohortManager.CohortDistributionServicesTests;

using System.Linq.Expressions;
using System.Net;
using System.Text.Json;
using Common;
using DataServices.Client;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Model;
using Moq;
using NHS.CohortManager.CohortDistributionServices;

[TestClass]
public class ValidationTests
{
    private readonly Mock<IHttpClientFunction> _httpClient = new();
    private readonly Mock<IOptions<DistributeParticipantConfig>> _config = new();
    private readonly Mock<IDataServiceClient<ParticipantManagement>> _participantManagementClient = new();
    private readonly Mock<IDataServiceClient<CohortDistribution>> _cohortDistributionClient = new();
    private readonly Mock<IExceptionHandler> _exceptionHandler = new();
    private ValidateParticipant _sut;
    private readonly Mock<TaskOrchestrationContext> _mockContext = new();
    private CohortDistributionParticipant _cohortDistributionParticipant;

    public ValidationTests()
    {
        DistributeParticipantConfig config = new()
        {
            LookupValidationURL = "LookupValidationURL",
            StaticValidationURL = "StaticValidationURL",
            TransformDataServiceURL = "TransformDataServiceURL",
            ParticipantManagementUrl = "ParticipantManagementUrl",
            CohortDistributionDataServiceUrl = "CohortDistributionDataServiceUrl",
            ParticipantDemographicDataServiceUrl = "ParticipantDemographicDataServiceUrl",
            IgnoreParticipantExceptions = false
        };

        _config.Setup(x => x.Value).Returns(config);

        _cohortDistributionParticipant = new()
        {
            ParticipantId = "1234",
            NhsNumber = "5678",
            ScreeningServiceId = "Screening123",
            Postcode = "AB1 2CD"
        };

        var request = new ValidationRecord
        {
            FileName = "test.csv",
            Participant = _cohortDistributionParticipant
        };

        ValidationExceptionLog validationResponseBody = new()
        {
            CreatedException = false,
            IsFatal = false
        };

        HttpResponseMessage validationSuccessResponse = new()
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(validationResponseBody))
        };

        HttpResponseMessage transformSuccessResponse = new()
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(new CohortDistributionParticipant()))
        };

        _mockContext
            .Setup(x => x.GetInput<ValidationRecord>())
            .Returns(request);

        _mockContext
            .Setup(x => x.CallActivityAsync<CohortDistributionParticipant>("GetCohortDistributionRecord", It.IsAny<string>(), null))
            .ReturnsAsync(new CohortDistributionParticipant());

        _mockContext
            .Setup(x => x.CallActivityAsync<ValidationExceptionLog>("LookupValidation", It.IsAny<ValidationRecord>(), null))
            .ReturnsAsync(validationResponseBody);

        _mockContext
            .Setup(x => x.CallActivityAsync<ValidationExceptionLog>("StaticValidation", It.IsAny<ValidationRecord>(), null))
            .ReturnsAsync(validationResponseBody);

        _mockContext
            .Setup(x => x.CallActivityAsync<CohortDistributionParticipant?>("TransformParticipant", It.IsAny<ValidationRecord>(), null))
            .ReturnsAsync(_cohortDistributionParticipant);

        _mockContext
            .Setup(x => x.CallActivityAsync("UpdateExceptionFlag", It.IsAny<string>(), null));

        _sut = new ValidateParticipant(
            _cohortDistributionClient.Object,
            _participantManagementClient.Object,
            _config.Object,
            _httpClient.Object,
            NullLogger<ValidateParticipant>.Instance,
            _exceptionHandler.Object);
    }

    [TestMethod]
    public async Task ValidationOrchestator_ValidRequest_ReturnTransformedParticipant()
    {
        // Act
        var result = await _sut.ValidationOrchestrator(_mockContext.Object);

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task ValidationOrchestator_ValidationRuleTriggered_UpdateExceptionFlagAndReturnNull()
    {
        // Arrange
        _mockContext.Setup(x => x.CallActivityAsync<ValidationExceptionLog>("StaticValidation", It.IsAny<ValidationRecord>(), null))
            .ReturnsAsync(new ValidationExceptionLog { CreatedException = true });

        // Act
        var result = await _sut.ValidationOrchestrator(_mockContext.Object);

        // Assert
        Assert.IsNull(result);
        _mockContext
            .Verify(x => x.CallActivityAsync("UpdateExceptionFlag", It.IsAny<string>(), null), Times.Once);
    }

    [TestMethod]
    public async Task ValidationOrchestator_RuleTriggeredIgnoreExceptions_UpdateExceptionFlagAndReturnParticipant()
    {
        // Arrange
        _mockContext.Setup(x => x.CallActivityAsync<ValidationExceptionLog>("StaticValidation", It.IsAny<ValidationRecord>(), null))
            .ReturnsAsync(new ValidationExceptionLog { CreatedException = true });

        DistributeParticipantConfig config = new()
        {
            LookupValidationURL = "LookupValidationURL",
            StaticValidationURL = "StaticValidationURL",
            TransformDataServiceURL = "TransformDataServiceURL",
            ParticipantManagementUrl = "ParticipantManagementUrl",
            CohortDistributionDataServiceUrl = "CohortDistributionDataServiceUrl",
            ParticipantDemographicDataServiceUrl = "ParticipantDemographicDataServiceUrl",
            IgnoreParticipantExceptions = true
        };

        _config.Setup(x => x.Value).Returns(config);

        _sut = new ValidateParticipant(
            _cohortDistributionClient.Object,
            _participantManagementClient.Object,
            _config.Object,
            _httpClient.Object,
            NullLogger<ValidateParticipant>.Instance,
            _exceptionHandler.Object);

        // Act
        var result = await _sut.ValidationOrchestrator(_mockContext.Object);

        // Assert
        Assert.IsNotNull(result);
        _mockContext
            .Verify(x => x.CallActivityAsync("UpdateExceptionFlag", It.IsAny<string>(), null), Times.Once);
    }
}