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
using NHS.CohortManager.CohortDistribution;
using NHS.CohortManager.Tests.TestUtils;
using Model;
using Model.Enums;
using Data.Database;

[TestClass]
public class CreateCohortDistributionTests
{
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly Mock<ILogger<CreateCohortDistribution>> _logger = new();
    private readonly Mock<ICohortDistributionHelper> _cohortDistributionHelper = new();
    private readonly CreateCohortDistribution _function;
    private readonly Mock<FunctionContext> _context = new();
    private readonly Mock<HttpRequestData> _request;
    private readonly Mock<IExceptionHandler> _exceptionHandler = new();
    private readonly CreateCohortDistributionRequestBody _requestBody;
    private readonly Mock<IParticipantManagerData> _participantManagerData = new();
    private readonly Mock<IAzureQueueStorageHelper> _azureQueueStorageHelper = new();
    private readonly Mock<HttpWebResponse> _sendToCohortDistributionResponse = new();

    public CreateCohortDistributionTests()
    {
        Environment.SetEnvironmentVariable("RetrieveParticipantDataURL", "RetrieveParticipantDataURL");
        Environment.SetEnvironmentVariable("AllocateScreeningProviderURL", "AllocateScreeningProviderURL");
        Environment.SetEnvironmentVariable("TransformDataServiceURL", "TransformDataServiceURL");
        Environment.SetEnvironmentVariable("AddCohortDistributionURL", "AddCohortDistributionURL");
        Environment.SetEnvironmentVariable("IsExtractedToBSSelect", "IsExtractedToBSSelect");
        Environment.SetEnvironmentVariable("IgnoreParticipantExceptions", "IgnoreParticipantExceptions");

        _request = new Mock<HttpRequestData>(_context.Object);

        _requestBody = new CreateCohortDistributionRequestBody()
        {
            NhsNumber = "1234567890",
            ScreeningService = "BSS",
        };

        _function = new CreateCohortDistribution(_logger.Object, _callFunction.Object, _cohortDistributionHelper.Object, _exceptionHandler.Object, _participantManagerData.Object, _azureQueueStorageHelper.Object);

        _request.Setup(r => r.CreateResponse()).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(_context.Object);
            response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
            response.SetupProperty(r => r.StatusCode);
            response.SetupProperty(r => r.Body, new MemoryStream());
            return response.Object;
        });
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public async Task RunAsync_BadRequestBody_ReturnsBadRequest(string screeningService)
    {
        // Act
        _requestBody.ScreeningService = screeningService;
        await _function.RunAsync(_requestBody);

        // Assert
        _logger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("One or more of the required parameters is missing")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        Times.Once);
    }

    [TestMethod]
    [DataRow(null, "BSS")]
    [DataRow("1234567890", null)]
    public async Task RunAsync_MissingFieldsOnRequestBody_ReturnsBadRequest(string nhsNumber, string screeningService)
    {
        // Arrange
        _requestBody.NhsNumber = nhsNumber;
        _requestBody.ScreeningService = screeningService;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        _function.RunAsync(_requestBody);

        // Assert
        _logger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("One or more of the required parameters is missing")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        Times.Once);
    }

    [TestMethod]
    public async Task RunAsync_RetrieveParticipantDataRequestFails_ReturnsBadRequest()
    {
        // Arrange
        Exception caughtException = null;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _cohortDistributionHelper.Setup(x => x.RetrieveParticipantDataAsync(It.IsAny<CreateCohortDistributionRequestBody>())).Throws(new Exception("some error"));

        // Act
        try
        {
            await _function.RunAsync(_requestBody);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert
        _logger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed during TransformParticipant or AddCohortDistribution Function.")),
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
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);
        _cohortDistributionHelper.Setup(x => x.RetrieveParticipantDataAsync(It.IsAny<CreateCohortDistributionRequestBody>())).Returns(Task.FromResult(new CohortDistributionParticipant()
        {
            ScreeningServiceId = "screeningservice",
            NhsNumber = "11111111",
            RecordType = "NEW",

        }));
        _participantManagerData.Setup(x => x.GetParticipant(It.IsAny<string>(), It.IsAny<string>())).Returns(new Participant()
        {
            ExceptionFlag = "0",
        });
        _cohortDistributionHelper.Setup(x => x.RetrieveParticipantDataAsync(It.IsAny<CreateCohortDistributionRequestBody>()))
        .Returns(Task.FromResult(new CohortDistributionParticipant()
        {
            ScreeningServiceId = "screeningservice",
            Postcode = "POSTCODE",
        }));
        _cohortDistributionHelper.Setup(x => x.AllocateServiceProviderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
        .Throws(new Exception("some error"));

        // Assert
        try
        {
            await _function.RunAsync(_requestBody);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert
        _logger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed during TransformParticipant or AddCohortDistribution Function.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        Times.Once);

        Assert.IsNotNull(caughtException);
        Assert.AreEqual("some error", caughtException.Message);
    }

    [TestMethod]
    public async Task RunAsync_TransformDataServiceRequestFails_ReturnsBadRequest()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _cohortDistributionHelper.Setup(x => x.RetrieveParticipantDataAsync(It.IsAny<CreateCohortDistributionRequestBody>())).Returns(Task.FromResult(new CohortDistributionParticipant() { ScreeningServiceId = "Screening123" }));
        _cohortDistributionHelper.Setup(x => x.ValidateCohortDistributionRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CohortDistributionParticipant>())).Returns(Task.FromResult(false));
        _cohortDistributionHelper.Setup(x => x.AllocateServiceProviderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(""));
        _cohortDistributionHelper.Setup(x => x.AllocateServiceProviderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(""));
        _participantManagerData.Setup(x => x.GetParticipant(It.IsAny<string>(), It.IsAny<string>())).Returns(new Participant()
        {
            ExceptionFlag = "0",
        });
        _cohortDistributionHelper.Setup(x => x.TransformParticipantAsync(It.IsAny<string>(), It.IsAny<CohortDistributionParticipant>()))
        .Returns(Task.FromResult<CohortDistributionParticipant>(null));

        // Act
        await _function.RunAsync(_requestBody);

        _logger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("The transform participant returned null in cohort distribution")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        Times.Once);
    }

    [TestMethod]
    public async Task RunAsync_AddCohortDistributionRequestFails_ReturnsBadRequest()
    {
        // Arrange
        Exception caughtException = null;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _cohortDistributionHelper.Setup(x => x.RetrieveParticipantDataAsync(It.IsAny<CreateCohortDistributionRequestBody>())).Returns(Task.FromResult(new CohortDistributionParticipant() { ScreeningServiceId = "Screening123" }));
        _cohortDistributionHelper.Setup(x => x.ValidateCohortDistributionRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CohortDistributionParticipant>())).Returns(Task.FromResult(false));
        _cohortDistributionHelper.Setup(x => x.AllocateServiceProviderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(""));
        _cohortDistributionHelper.Setup(x => x.AllocateServiceProviderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(""));
        _participantManagerData.Setup(x => x.GetParticipant(It.IsAny<string>(), It.IsAny<string>())).Returns(new Participant()
        {
            ExceptionFlag = "0",
        });
        _cohortDistributionHelper.Setup(x => x.TransformParticipantAsync(It.IsAny<string>(), It.IsAny<CohortDistributionParticipant>()))
        .Returns(Task.FromResult(new CohortDistributionParticipant()));

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("AddCohortDistributionURL")), It.IsAny<string>()))
            .Throws(new Exception("an error happened"));

        try
        {
            //Act
            await _function.RunAsync(_requestBody);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert
        _logger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed during TransformParticipant or AddCohortDistribution Function.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        Times.Once);

        Assert.IsNotNull(caughtException);
        Assert.AreEqual("an error happened", caughtException.Message);
    }

    [TestMethod]
    public async Task RunAsync_AllSuccessfulRequests_AddsToCOhort()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _cohortDistributionHelper.Setup(x => x.RetrieveParticipantDataAsync(It.IsAny<CreateCohortDistributionRequestBody>())).Returns(Task.FromResult(new CohortDistributionParticipant() { ScreeningServiceId = "Screening123" }));
        _cohortDistributionHelper.Setup(x => x.ValidateCohortDistributionRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CohortDistributionParticipant>())).Returns(Task.FromResult(false));
        _cohortDistributionHelper.Setup(x => x.AllocateServiceProviderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(""));
        _cohortDistributionHelper.Setup(x => x.AllocateServiceProviderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(""));
        _participantManagerData.Setup(x => x.GetParticipant(It.IsAny<string>(), It.IsAny<string>())).Returns(new Participant()
        {
            ExceptionFlag = "0",
        });
        _cohortDistributionHelper.Setup(x => x.TransformParticipantAsync(It.IsAny<string>(), It.IsAny<CohortDistributionParticipant>()))
        .Returns(Task.FromResult(new CohortDistributionParticipant()));


        _sendToCohortDistributionResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("AddCohortDistributionURL")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_sendToCohortDistributionResponse.Object));
        ParticipantException(false);

        var response = MockHelpers.CreateMockHttpResponseData(HttpStatusCode.OK);
        _callFunction.Setup(call => call.SendPost(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(response));

        // Act
        await _function.RunAsync(_requestBody);

        // Assert
        _logger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Participant has been successfully put on the cohort distribution table")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        Times.Once);
    }

    [TestMethod]
    public async Task RunAsync_ParticipantExceptionExpected_AddToCohortNotCalled()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _cohortDistributionHelper.Setup(x => x.TransformParticipantAsync(It.IsAny<string>(), It.IsAny<CohortDistributionParticipant>())).Returns(Task.FromResult(new CohortDistributionParticipant()));
        _cohortDistributionHelper.Setup(x => x.RetrieveParticipantDataAsync(It.IsAny<CreateCohortDistributionRequestBody>())).Returns(Task.FromResult(new CohortDistributionParticipant()));
        _cohortDistributionHelper.Setup(x => x.AllocateServiceProviderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(""));
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("AddCohortDistributionURL")), It.IsAny<string>())).Verifiable();

        ParticipantException(true);

        // Act
        await _function.RunAsync(_requestBody);

        // Assert
        _callFunction.Verify(call => call.SendPost(It.Is<string>(s => s == "AddCohortDistributionURL"), It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    public async Task RunAsync_ParticipantIsNull_CreatesSystemExceptionLog()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _cohortDistributionHelper.Setup(x => x.TransformParticipantAsync(It.IsAny<string>(), It.IsAny<CohortDistributionParticipant>())).Returns(Task.FromResult(new CohortDistributionParticipant()));
        _cohortDistributionHelper.Setup(x => x.AllocateServiceProviderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(""));

        ParticipantException(false);

        // Act
        await _function.RunAsync(_requestBody);

        // Assert
        _exceptionHandler.Verify(x => x.CreateSystemExceptionLog(
            It.IsAny<Exception>(),
            It.IsAny<Participant>(),
            It.IsAny<string>()),
            Times.Once);

        _callFunction.Verify(call => call.SendPost(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    public async Task RunAsync_ParticipantMissingPostcodeAndServiceProvider_CreatesSystemExceptionLog()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _cohortDistributionHelper.Setup(x => x.TransformParticipantAsync(It.IsAny<string>(), It.IsAny<CohortDistributionParticipant>())).Returns(Task.FromResult(new CohortDistributionParticipant()));
        _cohortDistributionHelper.Setup(x => x.RetrieveParticipantDataAsync(It.IsAny<CreateCohortDistributionRequestBody>())).Returns(Task.FromResult(new CohortDistributionParticipant()
        {
            ScreeningServiceId = "screeningServiceId",
            Postcode = "T357 P01",
        }));

        ParticipantException(false);

        // Act
        await _function.RunAsync(_requestBody);

        // Assert
        _exceptionHandler.Verify(x => x.CreateSystemExceptionLog(
            It.IsAny<Exception>(),
            It.IsAny<Participant>(),
            It.IsAny<string>()),
            Times.Once);

        _callFunction.Verify(call => call.SendPost(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    public async Task RunAsync_ParticipantHasExceptionAndRetrieveEnvironmentalVariableAsBool_CreatesSystemExceptionLog()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _cohortDistributionHelper.Setup(x => x.TransformParticipantAsync(It.IsAny<string>(), It.IsAny<CohortDistributionParticipant>())).Returns(Task.FromResult(new CohortDistributionParticipant()));
        _cohortDistributionHelper.Setup(x => x.RetrieveParticipantDataAsync(It.IsAny<CreateCohortDistributionRequestBody>())).Returns(Task.FromResult(new CohortDistributionParticipant()
        {
            ScreeningServiceId = "screeningServiceId",
            Postcode = string.Empty,
        }));

        var response = new Mock<HttpWebResponse>();
        response.Setup(r => r.StatusCode).Returns(HttpStatusCode.BadRequest);
        _callFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response.Object);
        ParticipantException(true);

        // Act
        await _function.RunAsync(_requestBody);

        // Assert
        _exceptionHandler.Verify(x => x.CreateSystemExceptionLog(
            It.IsAny<Exception>(),
            It.IsAny<Participant>(),
            It.IsAny<string>()),
            Times.Once);

        _callFunction.Verify(call => call.SendPost(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
    }

    [TestMethod]
    public async Task RunAsync_WhenAddCohortDistributionFails_CreatesRecordValidationExceptionLog()
    {
        // Arrange
        var response = new Mock<HttpWebResponse>();
        response.Setup(r => r.StatusCode).Returns(HttpStatusCode.BadRequest);

        _cohortDistributionHelper.Setup(x => x.TransformParticipantAsync(It.IsAny<string>(), It.IsAny<CohortDistributionParticipant>())).Returns(Task.FromResult(new CohortDistributionParticipant()));
        _cohortDistributionHelper.Setup(x => x.ValidateCohortDistributionRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CohortDistributionParticipant>())).Returns(Task.FromResult(false));
        _cohortDistributionHelper.Setup(x => x.RetrieveParticipantDataAsync(It.IsAny<CreateCohortDistributionRequestBody>())).Returns(Task.FromResult(new CohortDistributionParticipant()
        {
            ScreeningServiceId = "screeningServiceId",
            Postcode = string.Empty,
        }));

        _callFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response.Object);

        ParticipantException(false);
        // Act
        await _function.RunAsync(_requestBody);

        // Assert
        _callFunction.Verify(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _exceptionHandler.Verify(x => x.CreateRecordValidationExceptionLog(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()),
            Times.Never);
    }

    [TestMethod]
    public async Task RunAsync_WhenExceptionOccurs_CreatesSystemExceptionLogFromNhsNumber()
    {
        // Arrange
        _cohortDistributionHelper.Setup(x => x.RetrieveParticipantDataAsync(It.IsAny<CreateCohortDistributionRequestBody>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act & Assert
        await Assert.ThrowsExceptionAsync<Exception>(() => _function.RunAsync(_requestBody));

        _exceptionHandler.Verify(x => x.CreateSystemExceptionLogFromNhsNumber(
            It.IsAny<Exception>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()
        ), Times.Once);

        _callFunction.Verify(call => call.SendPost(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    public async Task RunAsync_ValidationFailed_CreatesValidationExceptionLog()
    {
        // Arrange
        var participant = new CohortDistributionParticipant
        {
            ParticipantId = "123",
            NhsNumber = "NHS123",
            ScreeningServiceId = "Screening123"
        };

        _cohortDistributionHelper.Setup(x => x.RetrieveParticipantDataAsync(It.IsAny<CreateCohortDistributionRequestBody>()))
            .ReturnsAsync(participant);

        _cohortDistributionHelper.Setup(x => x.ValidateCohortDistributionRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CohortDistributionParticipant>()))
            .ReturnsAsync(true);

        var response = new Mock<HttpWebResponse>();
        response.Setup(r => r.StatusCode).Returns(HttpStatusCode.BadRequest);

        _callFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response.Object);

        ParticipantException(false);

        // Act
        await _function.RunAsync(_requestBody);

        // Assert
        _exceptionHandler.Verify(x => x.CreateRecordValidationExceptionLog(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()
        ), Times.Once);
    }

    [TestMethod]
    public async Task RunAsync_ParticipantHasExceptionAndEnvironmentVariableTrue_CreateSystemExceptionLog()
    {
        // Arrange
        var participant = new CohortDistributionParticipant
        {
            ScreeningServiceId = "test",
            ParticipantId = "123",
        };

        _cohortDistributionHelper.Setup(x => x.RetrieveParticipantDataAsync(It.IsAny<CreateCohortDistributionRequestBody>()))
            .ReturnsAsync(participant);

        Environment.SetEnvironmentVariable("IgnoreParticipantExceptions", "true");
        ParticipantException(true);

        // Act
        await _function.RunAsync(_requestBody);

        // Assert
        _logger.Verify(x => x.Log(
         LogLevel.Information,
         It.IsAny<EventId>(),
         It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Unable to add to cohort distribution.")),
         It.IsAny<Exception>(),
         It.IsAny<Func<It.IsAnyType, Exception, string>>()));
    }
    private void SetUpRequestBody(string json)
    {
        var byteArray = Encoding.ASCII.GetBytes(json);
        var bodyStream = new MemoryStream(byteArray);

        _request.Setup(r => r.Body).Returns(bodyStream);
    }

    private void ParticipantException(bool hasException)
    {
        var participant = new Participant() { ExceptionFlag = hasException ? Exists.Yes.ToString() : Exists.No.ToString() };
        _participantManagerData.Setup(x => x.GetParticipant(It.IsAny<string>(), It.IsAny<string>())).Returns(participant);
    }
}
