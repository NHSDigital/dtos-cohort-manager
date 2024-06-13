namespace NHS.CohortManger.Tests.ParticipantManagementServiceTests;

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
using NHS.CohortManager.Tests.TestUtils;

[TestClass]
public class AddParticipantTests
{
    private readonly Mock<ILogger<AddParticipantFunction>> _loggerMock;
    private readonly ServiceCollection _serviceCollection;
    private readonly Mock<FunctionContext> _context;
    private Mock<HttpRequestData> _request;
    private readonly Mock<ICreateResponse> _createResponse;
    private readonly Mock<ICallFunction> _callFunction;
    private readonly Participant _participant;
    private readonly SetupRequest _setupRequest;
    private readonly Mock<HttpWebResponse> _webResponse;


    public AddParticipantTests()
    {
        _callFunction = new Mock<ICallFunction>();
        _createResponse = new Mock<ICreateResponse>();
        _loggerMock = new Mock<ILogger<AddParticipantFunction>>();
        _context = new Mock<FunctionContext>();
        _request = new Mock<HttpRequestData>(_context.Object);
        _setupRequest = new SetupRequest();
        _webResponse = new Mock<HttpWebResponse>();

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

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>()))
            .Returns((HttpStatusCode statusCode, HttpRequestData req) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });
    }

    [TestMethod]
    public async Task Run_ValidParticipant_ReturnsOk()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participant);
        _request = _setupRequest.Setup(json);

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.Created);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("DSaddParticipant")), It.IsAny<string>()))
                        .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        // Act
        var sut = new AddParticipantFunction(_loggerMock.Object, _callFunction.Object, _createResponse.Object);
        var result = await sut.Run(_request.Object);

        // Assert
        System.Console.WriteLine();
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_NullParticipant_ReturnsBadRequest()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participant);
        _request = _setupRequest.Setup(json);

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.Created);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>()))
                            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        // Act
        var sut = new AddParticipantFunction(_loggerMock.Object, _callFunction.Object, _createResponse.Object);
        var result = await sut.Run(null);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);

    }

    // [TestMethod]
    // public async Task Run_Should_Marked_As_Eligible_Log_Error()
    // {

    //     _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.Created);
    //     var json = JsonSerializer.Serialize(_participant);

    //     _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("DSaddParticipant")), It.IsAny<string>()));

    //     _request = _setupRequest.Setup(json);
    //     var sut = new AddParticipantFunction(_loggerMock.Object, _callFunction.Object, _createResponse.Object);

    //     var result = await sut.Run(_request.Object);

    //     _loggerMock.Verify(log =>
    //         log.Log(
    //         LogLevel.Information,
    //         0,
    //         It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("Unable to call function")),
    //         null,
    //         (Func<object, Exception, string>)It.IsAny<object>()
    //         ));
    // }
}
