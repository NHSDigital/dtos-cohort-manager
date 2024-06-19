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
    private readonly Mock<ICheckDemographic> _checkDemographic = new();
    private readonly Mock<ICreateParticipant> _createParticipant = new();
    private readonly ParticipantCsvRecord _participantCsvRecord;
    private readonly SetupRequest _setupRequest = new();
    private Mock<HttpRequestData> _request;

    public AddNewParticipantTestClass()
    {
        Environment.SetEnvironmentVariable("DSaddParticipant", "DSaddParticipant");
        Environment.SetEnvironmentVariable("DSmarkParticipantAsEligible", "DSmarkParticipantAsEligible");
        Environment.SetEnvironmentVariable("DemographicURIGet", "DemographicURIGet");

        _participantCsvRecord = new ParticipantCsvRecord
        {
            FileName = "test.csv",
            Participant = new Participant()
            {
                FirstName = "Joe",
                Surname = "Bloggs",
                NHSId = "1",
                RecordType = Actions.New
            }
        };
    }

    [TestMethod]
    public async Task Run_Should_log_Participant_Created()
    {
        // Arrange
        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.Created);
        var json = JsonSerializer.Serialize(_participantCsvRecord);

        _callFunctionMock.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("DSaddParticipant")), It.IsAny<string>()))
                        .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), It.Is<string>(s => s.Contains("DemographicURIGet"))))
            .Returns(Task.FromResult<Demographic>(new Demographic()));

        _request = _setupRequest.Setup(json);
        var sut = new AddParticipantFunction(_loggerMock.Object, _callFunctionMock.Object, _createResponse.Object, _checkDemographic.Object, _createParticipant.Object);

        // Act
        var result = await sut.Run(_request.Object);

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
        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.Created);
        var json = JsonSerializer.Serialize(_participantCsvRecord);

        _callFunctionMock.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("DSmarkParticipantAsEligible")), It.IsAny<string>()))
                        .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), It.Is<string>(s => s.Contains("DemographicURIGet"))))
                        .Returns(Task.FromResult<Demographic>(new Demographic()));

        _request = _setupRequest.Setup(json);
        var sut = new AddParticipantFunction(_loggerMock.Object, _callFunctionMock.Object, _createResponse.Object, _checkDemographic.Object, _createParticipant.Object);

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
        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.Created);
        var json = JsonSerializer.Serialize(_participantCsvRecord);

        _callFunctionMock.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("DSmarkParticipantAsEligible")), It.IsAny<string>()));

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), It.Is<string>(s => s.Contains("DemographicURIGet"))))
            .Returns(Task.FromResult<Demographic>(new Demographic()));

        _request = _setupRequest.Setup(json);
        var sut = new AddParticipantFunction(_loggerMock.Object, _callFunctionMock.Object, _createResponse.Object, _checkDemographic.Object, _createParticipant.Object);

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
        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.Created);
        var json = JsonSerializer.Serialize(_participantCsvRecord);

        _callFunctionMock.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("DSaddParticipant")), It.IsAny<string>()));

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), It.Is<string>(s => s.Contains("DemographicURIGet"))))
            .Returns(Task.FromResult<Demographic>(new Demographic()));

        _request = _setupRequest.Setup(json);
        var sut = new AddParticipantFunction(_loggerMock.Object, _callFunctionMock.Object, _createResponse.Object, _checkDemographic.Object, _createParticipant.Object);

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
}
