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
    Mock<ILogger<UpdateParticipantFunction>> loggerMock;
    Mock<ICallFunction> callFunctionMock;
    ServiceCollection serviceCollection;
    Mock<FunctionContext> context;
    Mock<HttpRequestData> request;
    Mock<ICreateResponse> createResponse;

    Mock<HttpWebResponse> webResponse;

    Mock<ICheckDemographic> checkDemographic = new();

    Mock<ICreateParticipant> createParticipant = new();

    Participant participant;
    public UpdateParticipantTests()
    {
        Environment.SetEnvironmentVariable("UpdateParticipant", "UpdateParticipant");
        Environment.SetEnvironmentVariable("DemographicURI", "DemographicURI");

        loggerMock = new Mock<ILogger<UpdateParticipantFunction>>();
        createResponse = new Mock<ICreateResponse>();
        callFunctionMock = new Mock<ICallFunction>();
        context = new Mock<FunctionContext>();
        request = new Mock<HttpRequestData>(context.Object);
        webResponse = new Mock<HttpWebResponse>();

        serviceCollection = new ServiceCollection();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        context.SetupProperty(c => c.InstanceServices, serviceProvider);

        participant = new Participant()
        {
            NHSId = "1",
        };
    }

    [TestMethod]
    public async Task Run_Should_Return_BadRequest_And_Not_Update_Participant_When_Validation_Fails()
    {
        webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        var json = JsonSerializer.Serialize(participant);

        callFunctionMock.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("UpdateParticipant")), It.IsAny<string>()))
                        .Returns(Task.FromResult<HttpWebResponse>(webResponse.Object));

        callFunctionMock.Setup(call => call.SendGet(It.IsAny<string>()))
                        .Returns(Task.FromResult<string>(""));

        checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), It.Is<string>(s => s.Contains("DemographicURI"))))
                        .Returns(Task.FromResult<Demographic>(new Demographic()));

        setupRequest(json);

        var sut = new UpdateParticipantFunction(loggerMock.Object, createResponse.Object, callFunctionMock.Object, checkDemographic.Object, createParticipant.Object);

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
        setupRequest(json);
        var sut = new UpdateParticipantFunction(loggerMock.Object, createResponse.Object, callFunctionMock.Object, checkDemographic.Object, createParticipant.Object);

        var result = await sut.Run(request.Object);

        loggerMock.Verify(log =>
            log.Log(
            LogLevel.Information,
            0,
            It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("the user has not been updated due to a bad request")),
            null,
            (Func<object, Exception, string>)It.IsAny<object>()
            ));
    }

    [TestMethod]
    public async Task Run_Should_Return_BadRequest_When_Participant_Update_Fails()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participant);
        setupRequest(json);

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
        setupRequest(json);
        var sut = new UpdateParticipantFunction(loggerMock.Object, createResponse.Object, callFunctionMock.Object, checkDemographic.Object, createParticipant.Object);

        var result = await sut.Run(request.Object);

        loggerMock.Verify(log =>
            log.Log(
            LogLevel.Information,
            0,
            It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("Unable to call function")),
            null,
            (Func<object, Exception, string>)It.IsAny<object>()
            ));
    }

    private void setupRequest(string json)
    {
        var byteArray = Encoding.ASCII.GetBytes(json);
        var bodyStream = new MemoryStream(byteArray);

        _request.Setup(r => r.Body).Returns(bodyStream);
        _request.Setup(r => r.CreateResponse()).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(_context.Object);
            response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
            response.SetupProperty(r => r.StatusCode);
            response.SetupProperty(r => r.Body, new MemoryStream());
            return response.Object;
        });
    }
}
