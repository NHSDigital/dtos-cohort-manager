namespace NHS.CohortManager.Tests.ParticipantManagementServiceTests;

using System.Net;
using System.Text;
using System.Text.Json;
using System;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Model;

using NHS.CohortManager.ParticipantManagementService;
using NHS.CohortManager.Tests.TestUtils;

[TestClass]
public class UpdateParticipantTests
{
    Mock<ILogger<UpdateParticipantFunction>> loggerMock;
    Mock<ICallFunction> callFunctionMock;
    ServiceCollection serviceCollection;
    Mock<FunctionContext> context;
    private Mock<HttpRequestData> _request;
    Mock<ICreateResponse> createResponse;
    Mock<HttpWebResponse> webResponse;
    Participant participant;
    private readonly SetupRequest _setupRequest;

    public UpdateParticipantTests()
    {
        Environment.SetEnvironmentVariable("UpdateParticipant", "UpdateParticipant");

        _setupRequest = new SetupRequest();
        loggerMock = new Mock<ILogger<UpdateParticipantFunction>>();
        createResponse = new Mock<ICreateResponse>();
        callFunctionMock = new Mock<ICallFunction>();
        context = new Mock<FunctionContext>();
        _request = new Mock<HttpRequestData>(context.Object);
        webResponse = new Mock<HttpWebResponse>();

        serviceCollection = new ServiceCollection();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        context.SetupProperty(c => c.InstanceServices, serviceProvider);
    }

    [TestMethod]
    public async Task Run_Should_log_Participant_updated()
    {
        webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        var json = JsonSerializer.Serialize(participant);

        callFunctionMock.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("UpdateParticipant")), It.IsAny<string>()))
                        .Returns(Task.FromResult<HttpWebResponse>(webResponse.Object));
        _request = _setupRequest.Setup(json);

        var sut = new UpdateParticipantFunction(loggerMock.Object, createResponse.Object, callFunctionMock.Object);

        var result = await sut.Run(_request.Object);

        loggerMock.Verify(log =>
            log.Log(
            LogLevel.Information,
            0,
            It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("participant updated")),
            null,
            (Func<object, Exception, string>)It.IsAny<object>()
            ));
    }

    [TestMethod]
    public async Task Run_Should_log_Participant_bad_request()
    {
        webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.InternalServerError);
        var json = JsonSerializer.Serialize(participant);

        callFunctionMock.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("UpdateParticipant")), It.IsAny<string>()))
                        .Returns(Task.FromResult<HttpWebResponse>(webResponse.Object));

        _request = _setupRequest.Setup(json);
        var sut = new UpdateParticipantFunction(loggerMock.Object, createResponse.Object, callFunctionMock.Object);

        var result = await sut.Run(_request.Object);

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
    public async Task Run_Should_log_Participant_Throw_Error()
    {
        webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.InternalServerError);

        var json = JsonSerializer.Serialize(participant);
        var exception = new Exception("Unable to call function");

        callFunctionMock.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("UpdateParticipant")), It.IsAny<string>()))
        .ThrowsAsync(exception);

        _request = _setupRequest.Setup(json);
        var sut = new UpdateParticipantFunction(loggerMock.Object, createResponse.Object, callFunctionMock.Object);

        var result = await sut.Run(_request.Object);

        loggerMock.Verify(log =>
            log.Log(
            LogLevel.Information,
            0,
            It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("Unable to call function")),
            null,
            (Func<object, Exception, string>)It.IsAny<object>()
            ));
    }
}
