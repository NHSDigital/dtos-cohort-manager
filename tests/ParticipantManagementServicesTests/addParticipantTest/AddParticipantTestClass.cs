namespace addParticipant;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using Moq;
using System.Text;
using System.Text.Json;
using Model;

[TestClass]
public class AddNewParticipantTestClass
{
    private readonly Mock<ILogger<AddParticipantFunction>> loggerMock;
    private readonly Mock<ICallFunction> callFunctionMock;
    private readonly ServiceCollection serviceCollection;
    private readonly Mock<FunctionContext> context;
    private readonly Mock<HttpRequestData> request;
    private readonly Mock<ICreateResponse> createResponse;
    private readonly Mock<HttpWebResponse> webResponse;

    Participant participant;
    public AddNewParticipantTestClass()
    {
        Environment.SetEnvironmentVariable("DSaddParticipant", "DSaddParticipant");
        Environment.SetEnvironmentVariable("DSmarkParticipantAsEligible", "DSmarkParticipantAsEligible");

        loggerMock = new Mock<ILogger<AddParticipantFunction>>();
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
            FirstName = "Joe",
            Surname = "Bloggs",
            NHSId = "1",
            RecordType = Actions.New
        };
    }

    [TestMethod]
    public async Task Run_Should_log_Participant_Created()
    {
        webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.Created);
        var json = JsonSerializer.Serialize(participant);

        callFunctionMock.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("DSaddParticipant")), It.IsAny<string>()))
                        .Returns(Task.FromResult<HttpWebResponse>(webResponse.Object));

        _request = _setupRequest.Setup(json);
        var sut = new AddParticipantFunction(loggerMock.Object, callFunctionMock.Object, createResponse.Object);

        var result = await sut.Run(request.Object);

        loggerMock.Verify(log =>
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

        webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.Created);
        var json = JsonSerializer.Serialize(participant);

        callFunctionMock.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("DSmarkParticipantAsEligible")), It.IsAny<string>()))
                        .Returns(Task.FromResult<HttpWebResponse>(webResponse.Object));

        _request = _setupRequest.Setup(json);
        var sut = new AddParticipantFunction(loggerMock.Object, callFunctionMock.Object, createResponse.Object);

        var result = await sut.Run(request.Object);

        loggerMock.Verify(log =>
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

        webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.Created);
        var json = JsonSerializer.Serialize(participant);

        callFunctionMock.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("DSmarkParticipantAsEligible")), It.IsAny<string>()));

        _request = _setupRequest.Setup(json);
        var sut = new AddParticipantFunction(loggerMock.Object, callFunctionMock.Object, createResponse.Object);

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

    [TestMethod]
    public async Task Run_Should_Marked_As_Eligible_Log_Error()
    {

        webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.Created);
        var json = JsonSerializer.Serialize(participant);

        callFunctionMock.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("DSaddParticipant")), It.IsAny<string>()));

        _request = _setupRequest.Setup(json);
        var sut = new AddParticipantFunction(loggerMock.Object, callFunctionMock.Object, createResponse.Object);

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

    private void SetupRequest(string json)
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
