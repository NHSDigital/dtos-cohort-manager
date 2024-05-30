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
    private readonly ServiceCollection _serviceCollection;
    private readonly Mock<FunctionContext> _context;
    private readonly Mock<HttpRequestData> _request;
    private readonly CreateResponse _createResponse;
    private readonly CallFunction _callFunction;
    private readonly Participant _participant;

    public AddNewParticipantTestClass()
    {
        _callFunction = new Common.CallFunction();
        _createResponse = new Common.CreateResponse();
        _loggerMock = new Mock<ILogger<AddParticipantFunction>>();
        _context = new Mock<FunctionContext>();
        _request = new Mock<HttpRequestData>(_context.Object);

        _serviceCollection = new ServiceCollection();
        var serviceProvider = _serviceCollection.BuildServiceProvider();

        _context.SetupProperty(c => c.InstanceServices, serviceProvider);

        _participant = new Participant()
        {
            FirstName = "Joe",
            Surname = "Bloggs",
            NHSId = "1",
            Action = "ADD"
        };
    }

    [TestMethod]
    public async Task Run_ValidParticipant_ReturnsOk()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participant);
        SetupRequest(json);

        // Act
        var sut = new AddParticipantFunction(_loggerMock.Object, _callFunction, _createResponse);
        var result = await sut.Run(_request.Object);
        System.Console.WriteLine(result.StatusCode);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_NullParticipant_ReturnsBadRequest()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participant);
        SetupRequest(json);

        // Act
        var sut = new AddParticipantFunction(_loggerMock.Object, _callFunction, _createResponse);
        var result = await sut.Run(null);
        System.Console.WriteLine(result.StatusCode);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);

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
