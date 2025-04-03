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
            AddCohortDistributionURL = "AddCohortDistributionURL",
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
        _cohortDistributionHelper
            .Setup(x => x.AllocateServiceProviderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("");

        _sendToCohortDistributionResponse
            .Setup(x => x.StatusCode)
            .Returns(HttpStatusCode.OK);

        var response = MockHelpers.CreateMockHttpResponseData(HttpStatusCode.OK);
        _callFunction
            .Setup(call => call.SendPost(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(response);

        // Act
        await _sut.RunAsync(_requestBody);

        // Assert
        _callFunction
            .Verify(x => x.SendPost("AddCohortDistributionURL", It.IsAny<string>()));
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

        // Act
        await _sut.RunAsync(_requestBody);

        // Assert
        _exceptionHandler
            .Verify(x => x.CreateSystemExceptionLog(
                It.Is<Exception>(ex => ex.Message == "One or more of the required parameters is missing."),
                It.IsAny<Participant>(),
                It.IsAny<string>()));
        
        
    }

    [TestMethod]
    public async Task RunAsync_RetrieveParticipantDataRequestFails_ReturnBadRequest()
    {
        // Arrange
        Exception caughtException = null;
        _request = _setupRequest.Setup(JsonSerializer.Serialize(_requestBody));

        _cohortDistributionHelper
            .Setup(x => x.RetrieveParticipantDataAsync(It.IsAny<CreateCohortDistributionRequestBody>()))
            .Throws(new Exception("some error"));

        // Act
        try
        {
            await _sut.RunAsync(_requestBody);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert
        _logger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Create Cohort Distribution failed")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        Times.Once);

        Assert.IsNotNull(caughtException);
        Assert.AreEqual("some error", caughtException.Message);
    }

    [TestMethod]
    public async Task RunAsync_AllocateServiceProviderToParticipantRequestFails_ReturnsBadRequest()
    {
        // Arrange
        Exception caughtException = null;
        _request = _setupRequest.Setup(JsonSerializer.Serialize(_requestBody));

        CohortDistributionParticipant cohortParticipant = new()
        {
            ScreeningServiceId = "screeningservice",
            NhsNumber = "11111111",
            RecordType = "NEW",
        };

        _participantManagementClientMock
            .Setup(c => c.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
            .ReturnsAsync(new ParticipantManagement { ExceptionFlag = 0 });

        _cohortDistributionHelper
            .Setup(x => x.RetrieveParticipantDataAsync(It.IsAny<CreateCohortDistributionRequestBody>()))
            .ReturnsAsync(new CohortDistributionParticipant() { ScreeningServiceId = "screeningservice", Postcode = "POSTCODE" });

        _cohortDistributionHelper
            .Setup(x => x.AllocateServiceProviderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new Exception("some error"));

        // Assert
        try
        {
            await _sut.RunAsync(_requestBody);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert
        _logger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Create Cohort Distribution failed")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        Times.Once);

        Assert.IsNotNull(caughtException);
        Assert.AreEqual("some error", caughtException.Message);
    }

    [TestMethod]
    public async Task RunAsync_TransformDataServiceRequestFails_ReturnEarly()
    {
        // Arrange
        _cohortDistributionHelper
            .Setup(x => x.AllocateServiceProviderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("");
        _cohortDistributionHelper
            .Setup(x => x.TransformParticipantAsync(It.IsAny<string>(), It.IsAny<CohortDistributionParticipant>(), It.IsAny<CohortDistributionParticipant>()))
            .ReturnsAsync((CohortDistributionParticipant)null);

        _participantManagementClientMock
            .Setup(c => c.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
            .ReturnsAsync(new ParticipantManagement { ExceptionFlag = 0 });

        // Act
        await _sut.RunAsync(_requestBody);

        // Assert
        _callFunction.Verify(x => x.SendPost("AddCohortDistributionURL", It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task RunAsync_AddCohortDistributionRequestFails_LogError()
    {
        // Arrange
        Exception caughtException = null;
        _request = _setupRequest.Setup(JsonSerializer.Serialize(_requestBody));

        _cohortDistributionHelper
            .Setup(x => x.AllocateServiceProviderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("");
        _participantManagementClientMock
            .Setup(c => c.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
            .ReturnsAsync(new ParticipantManagement { ExceptionFlag = 0 });

        _callFunction
            .Setup(call => call.SendPost(It.Is<string>(s => s.Contains("AddCohortDistributionURL")), It.IsAny<string>()))
            .Throws(new Exception("an error happened"));

        try
        {
            //Act
            await _sut.RunAsync(_requestBody);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert
        _logger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Create Cohort Distribution failed")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        Times.Once);

        Assert.IsNotNull(caughtException);
        Assert.AreEqual("an error happened", caughtException.Message);
    }

    [TestMethod]
    public async Task RunAsync_ParticipantHasException_CreatesSystemExceptionLog()
    {
        // Arrange
        _request = _setupRequest.Setup(JsonSerializer.Serialize(_requestBody));

        var response = new Mock<HttpWebResponse>();
        response.Setup(r => r.StatusCode).Returns(HttpStatusCode.BadRequest);
        _callFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response.Object);
        _participantManagementClientMock
            .Setup(c => c.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
            .ReturnsAsync(new ParticipantManagement() { ExceptionFlag = 1 });

        // Act
        await _sut.RunAsync(_requestBody);

        // Assert
        _exceptionHandler.Verify(x => x.CreateSystemExceptionLog(
            It.IsAny<Exception>(),
            It.IsAny<Participant>(),
            It.IsAny<string>()),
            Times.Once);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task RunAsync_ValidationRuleTriggered_UpdateExceptionFlagAndLog(bool ignoreExceptionsValue)
    {
        // Arrange
        var testConfig = new CreateCohortDistributionConfig
        {
            IgnoreParticipantExceptions = ignoreExceptionsValue,
            CohortQueueNamePoison = "CohortQueueNamePoison",
            AddCohortDistributionURL = "AddCohortDistributionURL",
            LookupValidationURL = "LookupValidationURL",
            TransformDataServiceURL = "TransformDataServiceURL",
            AllocateScreeningProviderURL = "AllocateScreeningProviderURL",
            RetrieveParticipantDataURL = "RetrieveParticipantDataUR"
        };

        _config.Setup(c => c.Value).Returns(testConfig);

        _cohortDistributionHelper
            .Setup(x => x.ValidateCohortDistributionRecordAsync(It.IsAny<string>(), It.IsAny<CohortDistributionParticipant>(), It.IsAny<CohortDistributionParticipant>()))
            .ReturnsAsync(new ValidationExceptionLog { CreatedException = true, IsFatal = false });
        _cohortDistributionHelper
            .Setup(x => x.AllocateServiceProviderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("");

        _participantManagementClientMock
            .Setup(c => c.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
            .ReturnsAsync(new ParticipantManagement() { ExceptionFlag = 0 });

        _sut = new CreateCohortDistribution(_logger.Object, _callFunction.Object, _cohortDistributionHelper.Object,
                                    _exceptionHandler.Object, _azureQueueStorageHelper.Object,
                                    _participantManagementClientMock.Object, _cohortDistributionClientMock.Object,
                                    _config.Object);

        // Act
        await _sut.RunAsync(_requestBody);

        // Assert
        _exceptionHandler.Verify(x => x.CreateRecordValidationExceptionLog(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()
        ), Times.Once);

        _participantManagementClientMock
            .Verify(x => x.Update(It.IsAny<ParticipantManagement>()), Times.Once);

        if (ignoreExceptionsValue)
        {
            _callFunction
                .Verify(call => call.SendPost("AddCohortDistributionURL", It.IsAny<string>()), Times.Once());
        }

    }

    [TestMethod]
    public async Task RunAsync_ParticipantHasExceptionAndEnvironmentVariableFalse_CreateExceptionAndReturn()
    {
        // Arrange
        // Environment.SetEnvironmentVariable("IgnoreParticipantExceptions", "false");
        _cohortDistributionParticipant.ExceptionFlag = 1;
        _cohortDistributionHelper
            .Setup(x => x.RetrieveParticipantDataAsync(It.IsAny<CreateCohortDistributionRequestBody>()))
            .ReturnsAsync(_cohortDistributionParticipant);
        _cohortDistributionHelper
            .Setup(x => x.AllocateServiceProviderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("");

        // Act
        await _sut.RunAsync(_requestBody);

        // Assert
        _logger.Verify(x => x.Log(
         LogLevel.Error,
         It.IsAny<EventId>(),
         It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Unable to add to cohort distribution")),
         It.IsAny<Exception>(),
         It.IsAny<Func<It.IsAnyType, Exception, string>>()));

        _cohortDistributionHelper
            .Verify(x => x.ValidateCohortDistributionRecordAsync(
                It.IsAny<string>(),
                It.IsAny<CohortDistributionParticipant>(),
                It.IsAny<CohortDistributionParticipant>()),
            Times.Never);
    }
}
