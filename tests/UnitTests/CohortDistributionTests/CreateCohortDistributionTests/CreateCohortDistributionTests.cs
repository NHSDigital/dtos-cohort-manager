namespace NHS.CohortManager.Tests.UnitTests.CreateCohortDistributionTests;

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;
using Common;
using Microsoft.Extensions.Logging;
using NHS.CohortManager.CohortDistributionService;
using NHS.CohortManager.Tests.TestUtils;
using Model;
using Model.Enums;
using Data.Database;
using DataServices.Client;
using System.Linq.Expressions;
using Microsoft.Extensions.Options;

[TestClass]
public class CreateCohortDistributionTests
{
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly Mock<ILogger<CreateCohortDistribution>> _logger = new();
    private readonly Mock<ICohortDistributionHelper> _cohortDistributionHelper = new();
    private CreateCohortDistribution _sut;
    private readonly Mock<IExceptionHandler> _exceptionHandler = new();
    private readonly CreateCohortDistributionRequestBody _requestBody;
    private readonly Mock<IAzureQueueStorageHelper> _azureQueueStorageHelper = new();
    private readonly Mock<HttpWebResponse> _sendToCohortDistributionResponse = new();
    private readonly Mock<IOptions<CreateCohortDistributionConfig>> _config = new();
    private Mock<HttpRequestData> _request;
    private readonly SetupRequest _setupRequest = new();
    private CohortDistributionParticipant _cohortDistributionParticipant;
    private Mock<IDataServiceClient<ParticipantManagement>> _participantManagementClientMock = new();
    private Mock<IDataServiceClient<CohortDistribution>> _cohortDistributionClientMock = new();


    public CreateCohortDistributionTests()
    {
        var testConfig = new CreateCohortDistributionConfig
        {
            IgnoreParticipantExceptions = false,
            CohortQueueNamePoison = "CohortQueueNamePoison",
            LookupValidationURL = "LookupValidationURL",
            TransformDataServiceURL = "TransformDataServiceURL",
            AllocateScreeningProviderURL = "AllocateScreeningProviderURL",
            RetrieveParticipantDataURL = "RetrieveParticipantDataUR"
        };

        _config.Setup(c => c.Value).Returns(testConfig);

        _requestBody = new CreateCohortDistributionRequestBody()
        {
            NhsNumber = "1234567890",
            ScreeningService = "BSS",
        };

        _request = _setupRequest.Setup(JsonSerializer.Serialize(_requestBody));

        _cohortDistributionParticipant = new()
        {
            ParticipantId = "1234",
            NhsNumber = "5678",
            ScreeningServiceId = "Screening123",
            Postcode = "AB1 2CD"
        };

        _cohortDistributionHelper
            .Setup(x => x.RetrieveParticipantDataAsync(It.IsAny<CreateCohortDistributionRequestBody>()))
            .ReturnsAsync(_cohortDistributionParticipant);
        _cohortDistributionHelper
            .Setup(x => x.ValidateCohortDistributionRecordAsync(It.IsAny<string>(), It.IsAny<CohortDistributionParticipant>(), It.IsAny<CohortDistributionParticipant>()))
            .ReturnsAsync(new ValidationExceptionLog { CreatedException = false, IsFatal = false });
        _cohortDistributionHelper
            .Setup(x => x.TransformParticipantAsync(It.IsAny<string>(), It.IsAny<CohortDistributionParticipant>(), It.IsAny<CohortDistributionParticipant>()))
            .ReturnsAsync(new CohortDistributionParticipant());
        _cohortDistributionHelper
            .Setup(x => x.AllocateServiceProviderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(EnumHelper.GetDisplayName(ServiceProvider.BSS));

        _participantManagementClientMock
            .Setup(x => x.GetSingle(It.IsAny<string>()))
            .ReturnsAsync(new ParticipantManagement { ExceptionFlag = 0 });
        _participantManagementClientMock
            .Setup(x => x.Update(It.IsAny<ParticipantManagement>()))
            .ReturnsAsync(true);

        _callFunction
            .Setup(call => call.SendPost("AddCohortDistributionURL", It.IsAny<string>()))
            .ReturnsAsync(_sendToCohortDistributionResponse.Object);

        _sut = new CreateCohortDistribution(_logger.Object, _callFunction.Object, _cohortDistributionHelper.Object,
                                            _exceptionHandler.Object, _azureQueueStorageHelper.Object,
                                            _participantManagementClientMock.Object, _cohortDistributionClientMock.Object,
                                            _config.Object);
    }

    [TestMethod]
    public async Task RunAsync_AllSuccessfulRequests_AddToCohort()
    {
        // Arrange
        _cohortDistributionClientMock.Setup(x => x.Add(It.IsAny<CohortDistribution>())).ReturnsAsync(true);

        // Act
        await _sut.RunAsync(_requestBody);

        // Assert
        _cohortDistributionClientMock.Verify(x => x.Add(It.IsAny<CohortDistribution>()), Times.Once);
    }


    [TestMethod]
    [DataRow(null, "BSS")]
    [DataRow("1234567890", null)]
    public async Task RunAsync_MissingFieldsOnRequestBody_CreateExceptionAndSendToPoisonQueue(string nhsNumber, string screeningService)
    {
        // Arrange
        _requestBody.NhsNumber = nhsNumber;
        _requestBody.ScreeningService = screeningService;
        _request = _setupRequest.Setup(JsonSerializer.Serialize(_requestBody));

        // Act & Assert
        await _sut.RunAsync(_requestBody);

        _exceptionHandler
            .Verify(x => x.CreateSystemExceptionLog(
                It.Is<Exception>(ex => ex.Message == "One or more of the required parameters is missing."),
                It.IsAny<Participant>(),
                It.IsAny<string>(),
                It.IsAny<string>()));

        _azureQueueStorageHelper
            .Verify(x => x.AddItemToQueueAsync(It.IsAny<CohortDistributionParticipant>(), It.IsAny<string>()));

        _callFunction
            .Verify(x => x.SendPost("AddCohortDistributionURL", It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task RunAsync_RetrieveParticipantDataRequestFails_CreateExceptionAndSendToPoisonQueue()
    {
        // Arrange
        _request = _setupRequest.Setup(JsonSerializer.Serialize(_requestBody));

        _cohortDistributionHelper
            .Setup(x => x.RetrieveParticipantDataAsync(It.IsAny<CreateCohortDistributionRequestBody>()))
            .Throws(new Exception("some error"));

        // Act & Assert
        Assert.ThrowsExceptionAsync<Exception>(async () =>
        {
            await _sut.RunAsync(_requestBody);
        });

        _exceptionHandler
            .Verify(x => x.CreateSystemExceptionLog(
                It.Is<Exception>(ex => ex.Message.Contains("Create Cohort Distribution failed")),
                It.IsAny<Participant>(),
                It.IsAny<string>(),
                It.IsAny<string>()));

        _azureQueueStorageHelper
            .Verify(x => x.AddItemToQueueAsync(It.IsAny<CohortDistributionParticipant>(), It.IsAny<string>()));

        _callFunction
            .Verify(x => x.SendPost("AddCohortDistributionURL", It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task RunAsync_AllocateServiceProviderFails_CreateExceptionAndSendToPoisonQueue()
    {
        // Arrange
        _request = _setupRequest.Setup(JsonSerializer.Serialize(_requestBody));

        CohortDistributionParticipant cohortParticipant = new()
        {
            ScreeningServiceId = "screeningservice",
            NhsNumber = "11111111",
            RecordType = "NEW",
        };

        _cohortDistributionHelper
            .Setup(x => x.AllocateServiceProviderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new Exception("some error"));

        // Act & Assert
        Assert.ThrowsExceptionAsync<Exception>(async () =>
        {
            await _sut.RunAsync(_requestBody);
        });

        _exceptionHandler
            .Verify(x => x.CreateSystemExceptionLog(
                It.Is<Exception>(ex => ex.Message.Contains("Create Cohort Distribution failed")),
                It.IsAny<Participant>(),
                It.IsAny<string>(),
                It.IsAny<string>()));

        _azureQueueStorageHelper
            .Verify(x => x.AddItemToQueueAsync(It.IsAny<CohortDistributionParticipant>(), It.IsAny<string>()));

        _callFunction
            .Verify(x => x.SendPost("AddCohortDistributionURL", It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task RunAsync_TransformDataServiceRequestFails_ReturnEarly()
    {
        // Arrange
        _cohortDistributionHelper
            .Setup(x => x.TransformParticipantAsync(It.IsAny<string>(), It.IsAny<CohortDistributionParticipant>(), It.IsAny<CohortDistributionParticipant>()))
            .ReturnsAsync((CohortDistributionParticipant)null);

        // Act
        await _sut.RunAsync(_requestBody);

        // Assert
        _callFunction
            .Verify(x => x.SendPost("AddCohortDistributionURL", It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task RunAsync_AddCohortDistributionRequestFails_CreateExceptionAndSendToPoisonQueue()
    {
        // Arrange
        _request = _setupRequest.Setup(JsonSerializer.Serialize(_requestBody));

        _cohortDistributionClientMock.Setup(x => x.Add(It.IsAny<CohortDistribution>())).Throws(new Exception("an error happened"));

        // Act & Assert
        Assert.ThrowsExceptionAsync<Exception>(async () =>
        {
            await _sut.RunAsync(_requestBody);
        });

        _exceptionHandler
            .Verify(x => x.CreateSystemExceptionLog(
                It.Is<Exception>(ex => ex.Message.Contains("Create Cohort Distribution failed")),
                It.IsAny<Participant>(),
                It.IsAny<string>(),
                It.IsAny<string>()));

        _azureQueueStorageHelper
            .Verify(x => x.AddItemToQueueAsync(It.IsAny<CohortDistributionParticipant>(), It.IsAny<string>()));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task RunAsync_ParticipantHasException_CreateExceptionAndSendToPoisonQueue(bool ignoreExceptionsValue)
    {
        // Arrange
        var testConfig = new CreateCohortDistributionConfig
        {
            IgnoreParticipantExceptions = ignoreExceptionsValue,
            CohortQueueNamePoison = "CohortQueueNamePoison",
            LookupValidationURL = "LookupValidationURL",
            TransformDataServiceURL = "TransformDataServiceURL",
            AllocateScreeningProviderURL = "AllocateScreeningProviderURL",
            RetrieveParticipantDataURL = "RetrieveParticipantDataUR"
        };

        _config.Setup(c => c.Value).Returns(testConfig);

        _cohortDistributionParticipant.ExceptionFlag = 1;
        _cohortDistributionHelper
            .Setup(x => x.RetrieveParticipantDataAsync(It.IsAny<CreateCohortDistributionRequestBody>()))
            .ReturnsAsync(_cohortDistributionParticipant);

        _request = _setupRequest.Setup(JsonSerializer.Serialize(_requestBody));

        _participantManagementClientMock
            .Setup(c => c.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
            .ReturnsAsync(new ParticipantManagement() { ExceptionFlag = 1 });

        _sut = new CreateCohortDistribution(_logger.Object, _callFunction.Object, _cohortDistributionHelper.Object,
                            _exceptionHandler.Object, _azureQueueStorageHelper.Object,
                            _participantManagementClientMock.Object, _cohortDistributionClientMock.Object,
                            _config.Object);

        // Act & Assert
        Assert.ThrowsExceptionAsync<Exception>(async () =>
        {
            await _sut.RunAsync(_requestBody);
        });

        _azureQueueStorageHelper
            .Verify(x => x.AddItemToQueueAsync(It.IsAny<CohortDistributionParticipant>(), It.IsAny<string>()));

        if (ignoreExceptionsValue)
        {
            _exceptionHandler
            .Verify(x => x.CreateSystemExceptionLog(
                It.Is<Exception>(ex => ex.Message.Contains("Failed to add the participant")),
                It.IsAny<Participant>(),
                It.IsAny<string>(),
                It.IsAny<string>()));
            _cohortDistributionClientMock.Verify(x => x.Add(It.IsAny<CohortDistribution>()), Times.Once());
        }
        else
        {
            _exceptionHandler
                .Verify(x => x.CreateSystemExceptionLog(
                    It.Is<Exception>(ex => ex.Message.Contains("Unable to add to cohort distribution")),
                    It.IsAny<Participant>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()));
            _cohortDistributionClientMock.Verify(x => x.Add(It.IsAny<CohortDistribution>()), Times.Never());
        }
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task RunAsync_ValidationRuleTriggered_UpdateExceptionFlagAndCreateException(bool ignoreExceptionsValue)
    {
        // Arrange
        var testConfig = new CreateCohortDistributionConfig
        {
            IgnoreParticipantExceptions = ignoreExceptionsValue,
            CohortQueueNamePoison = "CohortQueueNamePoison",
            LookupValidationURL = "LookupValidationURL",
            TransformDataServiceURL = "TransformDataServiceURL",
            AllocateScreeningProviderURL = "AllocateScreeningProviderURL",
            RetrieveParticipantDataURL = "RetrieveParticipantDataUR"
        };

        _config.Setup(c => c.Value).Returns(testConfig);

        _cohortDistributionHelper
            .Setup(x => x.ValidateCohortDistributionRecordAsync(It.IsAny<string>(), It.IsAny<CohortDistributionParticipant>(), It.IsAny<CohortDistributionParticipant>()))
            .ReturnsAsync(new ValidationExceptionLog { CreatedException = true, IsFatal = false });

        _sut = new CreateCohortDistribution(_logger.Object, _callFunction.Object, _cohortDistributionHelper.Object,
                                    _exceptionHandler.Object, _azureQueueStorageHelper.Object,
                                    _participantManagementClientMock.Object, _cohortDistributionClientMock.Object,
                                    _config.Object);

        // Act
        await _sut.RunAsync(_requestBody);

        // Assert
        _exceptionHandler
            .Verify(x => x.CreateSystemExceptionLog(
                It.Is<Exception>(ex => ex.Message.Contains("triggered a validation rule, so will not be added to cohort distribution")),
                It.IsAny<Participant>(),
                It.IsAny<string>(),
                It.IsAny<string>()));

        _participantManagementClientMock
            .Verify(x => x.Update(It.IsAny<ParticipantManagement>()), Times.Once);

        _azureQueueStorageHelper
            .Verify(x => x.AddItemToQueueAsync(It.IsAny<CohortDistributionParticipant>(), It.IsAny<string>()));

        if (ignoreExceptionsValue)
        {
            _cohortDistributionClientMock.Verify(x => x.Add(It.IsAny<CohortDistribution>()), Times.Once());
        }
        else
        {
            _cohortDistributionClientMock.Verify(x => x.Add(It.IsAny<CohortDistribution>()), Times.Never());
        }

    }
}
