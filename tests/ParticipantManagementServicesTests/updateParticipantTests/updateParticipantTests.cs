namespace updateParticipant;

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
using Model;

[TestClass]
public class UpdateParticipantTests
{
    Mock<ILogger<UpdateParticipantFunction>> _logger = new();
    Mock<ICallFunction> _callFunction = new();
    ServiceCollection _serviceCollection = new();
    Mock<FunctionContext> _context = new();
    Mock<HttpRequestData> _request;
    Mock<ICreateResponse> _createResponse = new();
    Mock<HttpWebResponse> _validationWebResponse = new();
    Mock<HttpWebResponse> _updateParticipantWebResponse = new();
    Participant _participant = new();

    public UpdateParticipantTests()
    {
        Environment.SetEnvironmentVariable("UpdateParticipant", "UpdateParticipant");
        Environment.SetEnvironmentVariable("StaticValidationURL", "StaticValidationURL");

        _request = new Mock<HttpRequestData>(_context.Object);

        var serviceProvider = _serviceCollection.BuildServiceProvider();
        _context.SetupProperty(c => c.InstanceServices, serviceProvider);
    }

    [TestMethod]
    public async Task Run_Should_Return_BadRequest_And_Not_Update_Participant_When_Validation_Fails()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participant);
        _request = _setupRequest.Setup(json);

        _validationWebResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.BadRequest);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s == "StaticValidationURL"), json))
            .Returns(Task.FromResult<HttpWebResponse>(_validationWebResponse.Object));

        var sut = new UpdateParticipantFunction(_logger.Object, _createResponse.Object, _callFunction.Object);

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        _createResponse.Verify(x => x.CreateHttpResponse(HttpStatusCode.BadRequest, _request.Object, ""), Times.Once());
        _callFunction.Verify(call => call.SendPost(It.Is<string>(s => s == "UpdateParticipant"), It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    public async Task Run_Should_Update_Participant_When_Validation_Succeeds()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participant);
        _request = _setupRequest.Setup(json);

        _validationWebResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s == "StaticValidationURL"), json))
            .Returns(Task.FromResult<HttpWebResponse>(_validationWebResponse.Object));

        var sut = new UpdateParticipantFunction(_logger.Object, _createResponse.Object, _callFunction.Object);

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        _callFunction.Verify(call => call.SendPost(It.Is<string>(s => s == "UpdateParticipant"), json), Times.Once());
    }

    [TestMethod]
    public async Task Run_Should_Return_Ok_When_Participant_Update_Succeeds()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participant);
        _request = _setupRequest.Setup(json);

        _validationWebResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s == "StaticValidationURL"), json))
            .Returns(Task.FromResult<HttpWebResponse>(_validationWebResponse.Object));

        _updateParticipantWebResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s == "UpdateParticipant"), json))
            .Returns(Task.FromResult<HttpWebResponse>(_updateParticipantWebResponse.Object));

        var sut = new UpdateParticipantFunction(_logger.Object, _createResponse.Object, _callFunction.Object);

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        _createResponse.Verify(x => x.CreateHttpResponse(HttpStatusCode.OK, _request.Object, ""), Times.Once());
    }

    [TestMethod]
    public async Task Run_Should_Return_BadRequest_When_Participant_Update_Fails()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participant);
        _request = _setupRequest.Setup(json);

        _validationWebResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s == "StaticValidationURL"), json))
            .Returns(Task.FromResult<HttpWebResponse>(_validationWebResponse.Object));

        _updateParticipantWebResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.BadRequest);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s == "UpdateParticipant"), json))
            .Returns(Task.FromResult<HttpWebResponse>(_updateParticipantWebResponse.Object));

        var sut = new UpdateParticipantFunction(_logger.Object, _createResponse.Object, _callFunction.Object);

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        _createResponse.Verify(x => x.CreateHttpResponse(HttpStatusCode.BadRequest, _request.Object, ""), Times.Once());
    }

    [TestMethod]
    public async Task Run_Should_Return_InternalServerError_When_Participant_Update_Throws_Exception()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participant);
        _request = _setupRequest.Setup(json);

        _validationWebResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s == "StaticValidationURL"), json))
            .Returns(Task.FromResult<HttpWebResponse>(_validationWebResponse.Object));

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s == "UpdateParticipant"), json))
            .Throws(new Exception());

        var sut = new UpdateParticipantFunction(_logger.Object, _createResponse.Object, _callFunction.Object);

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        _createResponse.Verify(x => x.CreateHttpResponse(HttpStatusCode.InternalServerError, _request.Object, ""), Times.Once());
    }
}
