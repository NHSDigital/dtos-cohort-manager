namespace NHS.CohortManager.Tests.ParticipantManagementServiceTests;

using Common;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using Moq;
using System.Text.Json;
using Model;
using addParticipant;
using NHS.CohortManager.Tests.TestUtils;

[TestClass]
public class AddNewParticipantTestClass
{
    private readonly Mock<ILogger<AddParticipantFunction>> _loggerMock = new();
    private readonly Mock<ILogger<CohortDistributionHandler>> _cohortDistributionLogger = new();
    private readonly Mock<ICallFunction> _callFunctionMock = new();
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<HttpWebResponse> _webResponse = new();
    private readonly Mock<HttpWebResponse> _validationResponse = new();
    private readonly Mock<HttpWebResponse> _sendToCohortDistributionResponse = new();
    private readonly Mock<ICheckDemographic> _checkDemographic = new();
    private readonly CreateParticipant _createParticipant = new();
    private readonly ICohortDistributionHandler _cohortDistributionHandler;
    private readonly Mock<IExceptionHandler> _handleException = new();
    private readonly SetupRequest _setupRequest = new();
    private Mock<HttpRequestData> _request;

    public AddNewParticipantTestClass()
    {
        Environment.SetEnvironmentVariable("DSaddParticipant", "DSaddParticipant");
        Environment.SetEnvironmentVariable("DSmarkParticipantAsEligible", "DSmarkParticipantAsEligible");
        Environment.SetEnvironmentVariable("DemographicURIGet", "DemographicURIGet");
        Environment.SetEnvironmentVariable("StaticValidationURL", "StaticValidationURL");
        Environment.SetEnvironmentVariable("CohortDistributionServiceURL", "CohortDistributionServiceURL");


        var participantCsvRecord = new ParticipantCsvRecord
        {
            FileName = "test.csv",
            Participant = new Participant()
            {
                FirstName = "Joe",
                Surname = "Bloggs",
                NhsNumber = "1",
                RecordType = Actions.New
            }
        };

        var json = JsonSerializer.Serialize(participantCsvRecord);
        _request = _setupRequest.Setup(json);

        _cohortDistributionHandler = new CohortDistributionHandler(_cohortDistributionLogger.Object, _callFunctionMock.Object);

        _callFunctionMock.Setup(call => call.SendPost("CohortDistributionServiceURL", It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_sendToCohortDistributionResponse.Object));

        _callFunctionMock.Setup(call => call.SendPost("StaticValidationURL", It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_validationResponse.Object));
        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), It.IsAny<string>())).Returns(It.IsAny<HttpResponseData>());
    }

    [TestMethod]
    public async Task Run_Should_log_Participant_Created()
    {
        // Arrange
        var basicParticipantCsvRecord = new BasicParticipantCsvRecord
        {
            FileName = "ExampleFileName.CSV",
            Participant = new BasicParticipantData
            {
                NhsNumber = "1234567890"
            }
        };
        var Demographic = new Demographic();
        var participantCsvRecord = new ParticipantCsvRecord
        {
            Participant = new Participant
            {
                NhsNumber = "1234567890",
                ExceptionFlag = "N"
            }
        };
        var RequestJson = JsonSerializer.Serialize(basicParticipantCsvRecord);
        var validateResonseJson = JsonSerializer.Serialize(participantCsvRecord);
        var mockRequest = MockHelpers.CreateMockHttpRequestData(RequestJson);


        _validationResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        _sendToCohortDistributionResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        _callFunctionMock.Setup(call => call.SendPost("DSaddParticipant", It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunctionMock.Setup(call => call.SendPost("DSmarkParticipantAsEligible", It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunctionMock.Setup(call => call.GetResponseText(It.IsAny<HttpWebResponse>()))
            .ReturnsAsync(validateResonseJson);
        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), "DemographicURIGet"))
            .Returns(Task.FromResult<Demographic>(new Demographic()));



        var sut = new AddParticipantFunction(_loggerMock.Object, _callFunctionMock.Object, _createResponse.Object, _checkDemographic.Object, _createParticipant, _handleException.Object, _cohortDistributionHandler);


        // Act
        var result = await sut.Run(mockRequest);

        // Assert
        _loggerMock.Verify(log =>
            log.Log(
            LogLevel.Information,
            0,
            It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("participant created")),
            null,
            (Func<object, Exception, string>)It.IsAny<object>()
            ));
    }

    [TestMethod]
    public async Task Run_Should_Log_Participant_Marked_As_Eligible()
    {
        // Arrange
        _validationResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        _callFunctionMock.Setup(call => call.SendPost("DSmarkParticipantAsEligible", It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunctionMock.Setup(call => call.SendPost("DSaddParticipant", It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunctionMock.Setup(call => call.SendPost("CohortDistributionServiceURL", It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), "DemographicURIGet"))
            .Returns(Task.FromResult<Demographic>(new Demographic()));

        _callFunctionMock.Setup(x => x.GetResponseText(It.IsAny<HttpWebResponse>())).Returns(Task.FromResult<string>(
            JsonSerializer.Serialize<ValidationExceptionLog>(new ValidationExceptionLog()
            {
                IsFatal = false,
                CreatedException = false
            })));

        var sut = new AddParticipantFunction(_loggerMock.Object, _callFunctionMock.Object, _createResponse.Object, _checkDemographic.Object, _createParticipant, _handleException.Object, _cohortDistributionHandler);

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        _loggerMock.Verify(log =>
            log.Log(
            LogLevel.Information,
            0,
            It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("participant created, marked as eligible")),
            null,
            (Func<object, Exception, string>)It.IsAny<object>()
            ));
    }
    [TestMethod]
    public async Task Run_Should_Call_Create_Cohort_EndPoint_Success()
    {
        // Arrange
        _validationResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        _webResponse.Setup(x => x.StatusCode)
            .Returns(HttpStatusCode.Created);

        _sendToCohortDistributionResponse.Setup(x => x.StatusCode)
            .Returns(HttpStatusCode.OK);

        _callFunctionMock.Setup(call => call.SendPost("DSmarkParticipantAsEligible", It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunctionMock.Setup(call => call.SendPost("CohortDistributionServiceURL", It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_sendToCohortDistributionResponse.Object));

        _checkDemographic.Setup(x => x.GetDemographicAsync("DemographicURIGet", It.IsAny<string>()))
            .Returns(Task.FromResult<Demographic>(new Demographic()))
            .Verifiable();



        var sut = new AddParticipantFunction(_loggerMock.Object, _callFunctionMock.Object, _createResponse.Object, _checkDemographic.Object, _createParticipant, _handleException.Object, _cohortDistributionHandler);

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        //_callFunctionMock.Verify(x => x.SendPost("DemographicURIGet", It.IsAny<string>()),Times.Once);
        _checkDemographic.Verify(x => x.GetDemographicAsync(It.IsAny<string>(), "DemographicURIGet"), Times.Once);
    }
    [TestMethod]
    public async Task Run_Should_Call_Create_Cohort_EndPoint_Failure()
    {
        // Arrange
        _validationResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        _webResponse.Setup(x => x.StatusCode)
            .Returns(HttpStatusCode.Created);

        _sendToCohortDistributionResponse.Setup(x => x.StatusCode)
            .Returns(HttpStatusCode.InternalServerError);

        _callFunctionMock.Setup(call => call.SendPost("DSmarkParticipantAsEligible", It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunctionMock.Setup(call => call.SendPost("CohortDistributionServiceURL", It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_sendToCohortDistributionResponse.Object));

        _checkDemographic.Setup(x => x.GetDemographicAsync("DemographicURIGet", It.IsAny<string>()))
            .Returns(Task.FromResult<Demographic>(new Demographic()))
            .Verifiable();

        var sut = new AddParticipantFunction(_loggerMock.Object, _callFunctionMock.Object, _createResponse.Object, _checkDemographic.Object, _createParticipant, _handleException.Object, _cohortDistributionHandler);

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        _checkDemographic.Verify(x => x.GetDemographicAsync(It.IsAny<string>(), "DemographicURIGet"), Times.Once);
    }

    [TestMethod]
    public async Task Run_Should_Log_Participant_Log_Error()
    {
        // Arrange
        _validationResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.Created);
        _callFunctionMock.Setup(call => call.SendPost("DSmarkParticipantAsEligible", It.IsAny<string>()));

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), "DemographicURIGet"))
            .Returns(Task.FromResult<Demographic>(new Demographic()));

        var sut = new AddParticipantFunction(_loggerMock.Object, _callFunctionMock.Object, _createResponse.Object, _checkDemographic.Object, _createParticipant, _handleException.Object, _cohortDistributionHandler);

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        _loggerMock.Verify(log =>
            log.Log(
            LogLevel.Information,
            0,
            It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("Unable to call function")),
            null,
            (Func<object, Exception, string>)It.IsAny<object>()
            ));
    }

    [TestMethod]
    public async Task Run_Should_Marked_As_Eligible_Log_Error()
    {
        // Arrange
        _validationResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.Created);
        _callFunctionMock.Setup(call => call.SendPost("DSaddParticipant", It.IsAny<string>()));

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), "DemographicURIGet"))
            .Returns(Task.FromResult<Demographic>(new Demographic()));

        var sut = new AddParticipantFunction(_loggerMock.Object, _callFunctionMock.Object, _createResponse.Object, _checkDemographic.Object, _createParticipant, _handleException.Object, _cohortDistributionHandler);

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        _loggerMock.Verify(log =>
            log.Log(
            LogLevel.Information,
            0,
            It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("Unable to call function")),
            null,
            (Func<object, Exception, string>)It.IsAny<object>()
            ));
    }

    [TestMethod]
    public async Task Run_Should_Add_Participant_With_ExceptionFlag_When_Validation_Fails()
    {
        // Arrange
        _validationResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.BadRequest);

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), "DemographicURIGet"))
            .Returns(Task.FromResult<Demographic>(new Demographic()));
        _sendToCohortDistributionResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        var sut = new AddParticipantFunction(_loggerMock.Object, _callFunctionMock.Object, _createResponse.Object, _checkDemographic.Object, _createParticipant, _handleException.Object, _cohortDistributionHandler);

        _callFunctionMock.Setup(x => x.GetResponseText(It.IsAny<HttpWebResponse>())).Returns(Task.FromResult<string>(
            JsonSerializer.Serialize<ValidationExceptionLog>(new ValidationExceptionLog()
            {
                IsFatal = false,
                CreatedException = false
            })));
        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        _callFunctionMock.Verify(call => call.SendPost("DSaddParticipant", It.IsAny<string>()), Times.Once());
    }
}
