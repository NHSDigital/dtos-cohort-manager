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
    private readonly Mock<ICallFunction> _callFunctionMock = new();
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<HttpWebResponse> _webResponse = new();
    private readonly Mock<HttpWebResponse> _validationResponse = new();
    private readonly Mock<ICheckDemographic> _checkDemographic = new();
    private readonly Mock<ICreateParticipant> _createParticipant = new();
    private readonly Mock<IExceptionHandler> _handleException = new();
    private readonly SetupRequest _setupRequest = new();
    private Mock<HttpRequestData> _request;

    public AddNewParticipantTestClass()
    {
        Environment.SetEnvironmentVariable("DSaddParticipant", "DSaddParticipant");
        Environment.SetEnvironmentVariable("DSmarkParticipantAsEligible", "DSmarkParticipantAsEligible");
        Environment.SetEnvironmentVariable("DemographicURIGet", "DemographicURIGet");
        Environment.SetEnvironmentVariable("StaticValidationURL", "StaticValidationURL");

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

        _callFunctionMock.Setup(call => call.SendPost("StaticValidationURL", It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_validationResponse.Object));
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

        _createParticipant.Setup(x => x.CreateResponseParticipantModel(It.IsAny<BasicParticipantData>(), It.IsAny<Demographic>())).Returns(participantCsvRecord.Participant);
        _validationResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.Created);

        _callFunctionMock.Setup(call => call.SendPost("DSaddParticipant", It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunctionMock.Setup(call => call.GetResponseText(It.IsAny<HttpWebResponse>()))
            .ReturnsAsync(validateResonseJson);
        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), "DemographicURIGet"))
            .Returns(Task.FromResult<Demographic>(new Demographic()));

        var sut = new AddParticipantFunction(_loggerMock.Object, _callFunctionMock.Object, _createResponse.Object, _checkDemographic.Object, _createParticipant.Object, _handleException.Object);


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

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.Created);
        _callFunctionMock.Setup(call => call.SendPost("DSmarkParticipantAsEligible", It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), "DemographicURIGet"))
            .Returns(Task.FromResult<Demographic>(new Demographic()));

        var sut = new AddParticipantFunction(_loggerMock.Object, _callFunctionMock.Object, _createResponse.Object, _checkDemographic.Object, _createParticipant.Object, _handleException.Object);

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
    public async Task Run_Should_Log_Participant_Log_Error()
    {
        // Arrange
        _validationResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.Created);
        _callFunctionMock.Setup(call => call.SendPost("DSmarkParticipantAsEligible", It.IsAny<string>()));

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), "DemographicURIGet"))
            .Returns(Task.FromResult<Demographic>(new Demographic()));

        var sut = new AddParticipantFunction(_loggerMock.Object, _callFunctionMock.Object, _createResponse.Object, _checkDemographic.Object, _createParticipant.Object, _handleException.Object);

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

        var sut = new AddParticipantFunction(_loggerMock.Object, _callFunctionMock.Object, _createResponse.Object, _checkDemographic.Object, _createParticipant.Object, _handleException.Object);

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
    public async Task Run_Should_Not_Add_Participant_When_Validation_Fails()
    {
        // Arrange
        _validationResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.BadRequest);

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), "DemographicURIGet"))
            .Returns(Task.FromResult<Demographic>(new Demographic()));

        var sut = new AddParticipantFunction(_loggerMock.Object, _callFunctionMock.Object, _createResponse.Object, _checkDemographic.Object, _createParticipant.Object, _handleException.Object);

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        _callFunctionMock.Verify(call => call.SendPost("DSaddParticipant", It.IsAny<string>()), Times.Never());
    }
}
