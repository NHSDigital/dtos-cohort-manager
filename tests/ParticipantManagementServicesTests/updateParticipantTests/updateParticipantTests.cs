namespace NHS.CohortManager.Tests.ParticipantManagementSericeTests;

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

[TestClass]
public class UpdateParticipantTests
{
    private readonly Mock<ILogger<UpdateParticipantFunction>> _logger = new();
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<HttpWebResponse> _webResponse = new();
    private readonly Mock<ICheckDemographic> _checkDemographic = new();
    private readonly Mock<ICreateParticipant> _createParticipant = new();
    private readonly Mock<HttpWebResponse> _validationWebResponse = new();
    private readonly Mock<HttpWebResponse> _updateParticipantWebResponse = new();
    private readonly SetupRequest _setupRequest = new();
    private readonly ParticipantCsvRecord _participantCsvRecord;
    private Mock<HttpRequestData> _request;

    public UpdateParticipantTests()
    {
        Environment.SetEnvironmentVariable("UpdateParticipant", "UpdateParticipant");
        Environment.SetEnvironmentVariable("DemographicURIGet", "DemographicURIGet");
        Environment.SetEnvironmentVariable("StaticValidationURL", "StaticValidationURL");

        _participantCsvRecord = new ParticipantCsvRecord
        {
            FileName = "test.csv",
            Participant = new Participant()
            {
                NHSId = "1",
            }
        };
    }

    [TestMethod]
    public async Task Run_Should_Return_BadRequest_And_Not_Update_Participant_When_Validation_Fails()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        _request = _setupRequest.Setup(json);
        var sut = new UpdateParticipantFunction(_logger.Object, _createResponse.Object, _callFunction.Object, _checkDemographic.Object, _createParticipant.Object);

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.BadRequest);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("StaticValidationURL")), It.IsAny<string>()))
                        .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _checkDemographic.Setup(call => call.GetDemographicAsync(It.IsAny<string>(), It.Is<string>(s => s.Contains("DemographicURIGet"))))
                        .Returns(Task.FromResult<Demographic>(new Demographic()));

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        _createResponse.Verify(x => x.CreateHttpResponse(HttpStatusCode.BadRequest, _request.Object, ""), Times.Once());
        _callFunction.Verify(call => call.SendPost(It.Is<string>(s => s == "UpdateParticipant"), It.IsAny<string>()), Times.Never());

        _logger.Verify(log =>
            log.Log(
            LogLevel.Information,
            0,
            It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("The participant has not been updated due to a bad request.")),
            null,
            (Func<object, Exception, string>)It.IsAny<object>()
            ));
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

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), It.Is<string>(s => s.Contains("DemographicURIGet"))))
            .Returns(Task.FromResult<Demographic>(new Demographic()));

        _request = _setupRequest.Setup(json);

        var sut = new UpdateParticipantFunction(_logger.Object, _createResponse.Object, _callFunction.Object, _checkDemographic.Object, _createParticipant.Object);

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
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
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s == "StaticValidationURL"), json))
            .Returns(Task.FromResult<HttpWebResponse>(_validationWebResponse.Object));


        _updateParticipantWebResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.BadRequest);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("UpdateParticipant")), It.IsAny<string>()))
                        .Returns(Task.FromResult<HttpWebResponse>(_updateParticipantWebResponse.Object));

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), It.Is<string>(s => s.Contains("DemographicURIGet"))))
        .Returns(Task.FromResult<Demographic>(new Demographic()));

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), ""))
        .Returns((HttpStatusCode statusCode, HttpRequestData req, string responseBody) =>
        {
            var response = req.CreateResponse(statusCode);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            return response;
        });

        var sut = new UpdateParticipantFunction(_logger.Object, _createResponse.Object, _callFunction.Object, _checkDemographic.Object, _createParticipant.Object);

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        _createResponse.Verify(x => x.CreateHttpResponse(HttpStatusCode.BadRequest, _request.Object, ""), Times.Once());
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

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), ""))
        .Returns((HttpStatusCode statusCode, HttpRequestData req, string responseBody) =>
        {
            var response = req.CreateResponse(statusCode);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            return response;
        });

        _createParticipant.Setup(x => x.CreateResponseParticipantModel(It.IsAny<BasicParticipantData>(), It.IsAny<Demographic>()))
        .Returns(_participantCsvRecord.Participant);

        var sut = new UpdateParticipantFunction(_logger.Object, _createResponse.Object, _callFunction.Object, _checkDemographic.Object, _createParticipant.Object);

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        _createResponse.Verify(x => x.CreateHttpResponse(HttpStatusCode.InternalServerError, _request.Object, ""), Times.Once());

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
