namespace updateParticipant;
using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Data.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    public async Task Run_Should_log_Participant_updated()
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

        var result = await sut.Run(request.Object);

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
    public async Task Run_Should_log_Participant_Throw_Error()
    {
        webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.InternalServerError);

        var json = JsonSerializer.Serialize(participant);
        var exception = new Exception("Unable to call function");

        callFunctionMock.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("UpdateParticipant")), It.IsAny<string>()))
        .ThrowsAsync(exception);

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

        request.Setup(r => r.Body).Returns(bodyStream);
        request.Setup(r => r.CreateResponse()).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(context.Object);
            response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
            response.SetupProperty(r => r.StatusCode);
            response.SetupProperty(r => r.Body, new MemoryStream());
            return response.Object;
        });

    }
}
