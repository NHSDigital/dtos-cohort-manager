namespace NHS.CohortManger.Tests.ParticipantManagementService;

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
using addParticipant;

[TestClass]
public class AddNewParticipantTestClass
{
    private readonly Mock<ILogger<AddParticipantFunction>> _loggerMock;
    private readonly Mock<ICallFunction> _callFunctionMock;
    private readonly ServiceCollection _serviceCollection;
    private readonly Mock<FunctionContext> _context;
    private readonly Mock<HttpRequestData> _request;
    private readonly Mock<ICreateResponse> _createResponse;
    private readonly Mock<HttpWebResponse> _webResponse;

    Participant participant;
    public AddNewParticipantTestClass()
    {
        Environment.SetEnvironmentVariable("DSaddParticipant", "DSaddParticipant");
        Environment.SetEnvironmentVariable("DSmarkParticipantAsEligible", "DSmarkParticipantAsEligible");

        _loggerMock = new Mock<ILogger<AddParticipantFunction>>();
        _createResponse = new Mock<ICreateResponse>();
        _callFunctionMock = new Mock<ICallFunction>();
        _context = new Mock<FunctionContext>();
        _request = new Mock<HttpRequestData>(_context.Object);
        _webResponse = new Mock<HttpWebResponse>();

        _serviceCollection = new ServiceCollection();
        var serviceProvider = _serviceCollection.BuildServiceProvider();

        _context.SetupProperty(c => c.InstanceServices, serviceProvider);

        participant = new Participant()
        {
            FirstName = "Joe",
            Surname = "Bloggs",
            NHSId = "1",
            Action = "ADD"
        };
    }

    [TestMethod]
    public async Task Run_ValidParticipant_ParticipantCreated()
    {
        // Arrange
        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.Created);
        var json = JsonSerializer.Serialize(participant);

        _callFunctionMock.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("DSaddParticipant")), It.IsAny<string>()))
                        .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        SetupRequest(json);

        // Act
        var sut = new AddParticipantFunction(_loggerMock.Object, _callFunctionMock.Object, _createResponse.Object);

        var result = await sut.Run(_request.Object);
        System.Console.WriteLine(result.StatusCode);
        System.Console.WriteLine("hello");

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

    [Ignore]
    [TestMethod]
    public async Task Run_Should_Log_Participant_Marked_As_Eligible()
    {

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.Created);
        var json = JsonSerializer.Serialize(participant);

        _callFunctionMock.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("DSmarkParticipantAsEligible")), It.IsAny<string>()))
                        .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        SetupRequest(json);
        var sut = new AddParticipantFunction(_loggerMock.Object, _callFunctionMock.Object, _createResponse.Object);

        var result = await sut.Run(_request.Object);

        _loggerMock.Verify(log =>
            log.Log(
            LogLevel.Information,
            0,
            It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("participant created, marked as eligible")),
            null,
            (Func<object, Exception, string>)It.IsAny<object>()
            ));
    }

    [Ignore]
    [TestMethod]
    public async Task Run_Should_Log_Participant_Log_Error()
    {

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.Created);
        var json = JsonSerializer.Serialize(participant);

        _callFunctionMock.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("DSmarkParticipantAsEligible")), It.IsAny<string>()));

        SetupRequest(json);
        var sut = new AddParticipantFunction(_loggerMock.Object, _callFunctionMock.Object, _createResponse.Object);

        var result = await sut.Run(_request.Object);

        _loggerMock.Verify(log =>
            log.Log(
            LogLevel.Information,
            0,
            It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("Unable to call function")),
            null,
            (Func<object, Exception, string>)It.IsAny<object>()
            ));
    }

    [Ignore]
    [TestMethod]
    public async Task Run_Should_Marked_As_Eligible_Log_Error()
    {

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.Created);
        var json = JsonSerializer.Serialize(participant);

        _callFunctionMock.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("DSaddParticipant")), It.IsAny<string>()));

        SetupRequest(json);
        var sut = new AddParticipantFunction(_loggerMock.Object, _callFunctionMock.Object, _createResponse.Object);

        var result = await sut.Run(_request.Object);

        _loggerMock.Verify(log =>
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
