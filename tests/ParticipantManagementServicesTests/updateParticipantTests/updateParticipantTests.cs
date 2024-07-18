namespace NHS.CohortManager.Tests.ParticipantManagementServiceTests;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Model;
using Moq;
using NHS.CohortManager.Tests.TestUtils;
using updateParticipant;
using RulesEngine.Models;



[TestClass]
public class UpdateParticipantTests
{
    private readonly Mock<ILogger<UpdateParticipantFunction>> _logger = new();
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly CreateResponse _createResponse = new();
    private readonly Mock<HttpWebResponse> _webResponse = new();
    private readonly Mock<HttpWebResponse> _webResponseSuccess = new();
    private readonly Mock<ICheckDemographic> _checkDemographic = new();
    private readonly CreateParticipant _createParticipant = new();
    private readonly Mock<HttpWebResponse> _validationWebResponse = new();
    private readonly Mock<HttpWebResponse> _updateParticipantWebResponse = new();
    private readonly Mock<IExceptionHandler> _handleException = new();
    private readonly SetupRequest _setupRequest = new();
    private readonly ParticipantCsvRecord _participantCsvRecord;
    private Mock<HttpRequestData> _request;

    public UpdateParticipantTests()
    {
        Environment.SetEnvironmentVariable("UpdateParticipant", "UpdateParticipant");
        Environment.SetEnvironmentVariable("DemographicURIGet", "DemographicURIGet");
        Environment.SetEnvironmentVariable("StaticValidationURL", "StaticValidationURL");

        _handleException.Setup(x => x.CreateValidationExceptionLog(It.IsAny<IEnumerable<RuleResultTree>>(), It.IsAny<ParticipantCsvRecord>()))
            .Returns(Task.FromResult(true)).Verifiable();

        _participantCsvRecord = new ParticipantCsvRecord
        {
            FileName = "test.csv",
            Participant = new Participant()
            {
                NhsNumber = "1",
            }
        };
    }

    [TestMethod]
    public async Task Run_Should_Return_BadRequest_And_Not_Update_Participant_When_Validation_Fails()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        _request = _setupRequest.Setup(json);


        var sut = new UpdateParticipantFunction(_logger.Object, _createResponse, _callFunction.Object, _checkDemographic.Object, _createParticipant, _handleException.Object);

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.BadRequest);
        _webResponseSuccess.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("StaticValidationURL")), It.IsAny<string>()))
                        .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("UpdateParticipant")), It.IsAny<string>()))
                .Returns(Task.FromResult<HttpWebResponse>(_webResponseSuccess.Object));

        _checkDemographic.Setup(call => call.GetDemographicAsync(It.IsAny<string>(), It.Is<string>(s => s.Contains("DemographicURIGet"))))
                        .Returns(Task.FromResult<Demographic>(new Demographic()));

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _callFunction.Verify(call => call.SendPost(It.Is<string>(s => s == "UpdateParticipant"), It.IsAny<string>()), Times.Once());
    }

    [TestMethod]
    public async Task Run_Should_Return_Ok_When_Participant_Update_Succeeds()
    {
        // Arrange
        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        var json = JsonSerializer.Serialize(_participantCsvRecord);

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("UpdateParticipant")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("StaticValidationURL")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunction.Setup(call => call.SendGet(It.IsAny<string>()))
            .Returns(Task.FromResult<string>(""));

        _callFunction.Setup(x => x.GetResponseText(It.IsAny<HttpWebResponse>())).Returns(Task.FromResult<string>(json));

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), It.Is<string>(s => s.Contains("DemographicURIGet"))))
            .Returns(Task.FromResult<Demographic>(new Demographic()));



        _request = _setupRequest.Setup(json);

        var sut = new UpdateParticipantFunction(_logger.Object, _createResponse, _callFunction.Object, _checkDemographic.Object, _createParticipant, _handleException.Object);

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

        _logger.Verify(log =>
            log.Log(
                LogLevel.Information,
                0,
                It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("Participant updated.")),
                null,
                (Func<object, Exception, string>)It.IsAny<object>()
            ));
    }

    [TestMethod]
    public async Task Run_Should_Return_BadRequest_When_Participant_Update_Fails()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        _request = _setupRequest.Setup(json);

        _validationWebResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("StaticValidationURL")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));


        _updateParticipantWebResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.BadRequest);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("UpdateParticipant")), It.IsAny<string>()))
                        .Returns(Task.FromResult<HttpWebResponse>(_updateParticipantWebResponse.Object));

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), It.Is<string>(s => s.Contains("DemographicURIGet"))))
        .Returns(Task.FromResult<Demographic>(new Demographic()));

        var sut = new UpdateParticipantFunction(_logger.Object, _createResponse, _callFunction.Object, _checkDemographic.Object, _createParticipant, _handleException.Object);

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_Should_Return_InternalServerError_When_Participant_Update_Throws_Exception()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        _request = _setupRequest.Setup(json);

        _validationWebResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s == "StaticValidationURL"), json))
            .Returns(Task.FromResult<HttpWebResponse>(_validationWebResponse.Object));

        _updateParticipantWebResponse.Setup(x => x.StatusCode).Throws(new Exception("an error occurred"));
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("UpdateParticipant")), It.IsAny<string>()))
                        .Returns(Task.FromResult<HttpWebResponse>(_updateParticipantWebResponse.Object));

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), It.Is<string>(s => s.Contains("DemographicURIGet"))))
                        .Returns(Task.FromResult<Demographic>(new Demographic()));


        var sut = new UpdateParticipantFunction(_logger.Object, _createResponse, _callFunction.Object, _checkDemographic.Object, _createParticipant, _handleException.Object);

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
        _logger.Verify(log =>
            log.Log(
                LogLevel.Information,
                0,
                It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("Update participant failed")),
                null,
                (Func<object, Exception, string>)It.IsAny<object>()
            ));
    }
}
