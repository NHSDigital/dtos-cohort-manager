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
using NHS.CohortManager.ParticipantManagementService;
using NHS.CohortManager.Tests.TestUtils;

[TestClass]
public class RemoveParticipantTests
{
    private readonly Mock<ILogger<RemoveParticipantFunction>> _logger = new();
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly Mock<FunctionContext> context = new();
    private Mock<HttpRequestData> _request;
    private readonly Mock<HttpWebResponse> _webResponse = new();
    private readonly SetupRequest _setupRequest = new();

    Participant participant;
    ServiceCollection serviceCollection = new();
    RemoveParticipantFunction removeParticipant;

    public RemoveParticipantTests()
    {
        Environment.SetEnvironmentVariable("markParticipantAsIneligible", "markParticipantAsIneligible");
        _request = new Mock<HttpRequestData>(context.Object);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        context.SetupProperty(c => c.InstanceServices, serviceProvider);
        participant = new Participant()
        {
            FirstName = "Joe",
            Surname = "Bloggs",
            NHSId = "1",
            Action = "ADD"
        };
        removeParticipant = new RemoveParticipantFunction(_logger.Object, _createResponse.Object, _callFunction.Object);

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>()))
            .Returns((HttpStatusCode statusCode, HttpRequestData req) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });
    }

    [TestMethod]
    public async Task Run_return_ParticipantRemovedSuccessfully_OK()
    {
        //Arrange
        var json = JsonSerializer.Serialize(participant);
        _request = _setupRequest.Setup(json);

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>()))
                            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        //Act
        var result = await removeParticipant.Run(_request.Object);

        //Assert
        System.Console.WriteLine(result.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_BadRequestReturnedFromRemoveDataService_InternalServerError()
    {

        //Arrange
        var json = JsonSerializer.Serialize(participant);
        _request = _setupRequest.Setup(json);

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.BadRequest);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>()))
                            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        //Act
        var result = await removeParticipant.Run(_request.Object);

        //Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_AnErrorIsThrown_BadRequest()
    {
        var json = JsonSerializer.Serialize(participant);
        _request = _setupRequest.Setup(json);

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>()))
        .Throws(new Exception("there has been a problem"));

        var result = await removeParticipant.Run(_request.Object);

        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }
}
