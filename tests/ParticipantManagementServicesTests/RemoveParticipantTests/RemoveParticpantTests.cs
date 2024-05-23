namespace NHS.CohortManager.Tests.ParticipantManagementService;

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
using RemoveParticipant;

[TestClass]
public class RemoveParticipantTests
{
    private readonly Mock<ILogger<RemoveParticipantFunction>> _logger = new();
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly Mock<FunctionContext> context = new();
    private readonly Mock<HttpRequestData> request;
    private readonly Mock<HttpWebResponse> webResponse = new();


    Participant participant;
    ServiceCollection serviceCollection = new();
    RemoveParticipantFunction removeParticipant;

    public RemoveParticipantTests()
    {
        Environment.SetEnvironmentVariable("markParticipantAsIneligible", "markParticipantAsIneligible");
        request = new Mock<HttpRequestData>(context.Object);
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
    }

    [TestMethod]
    public async Task Run_return_ParticipantRemovedSuccessfully_OK()
    {
        //Arrange
        var json = JsonSerializer.Serialize(participant);

        setupRequest(json);

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), null))
               .Returns((HttpStatusCode statusCode, HttpRequestData req) =>
               {
                   var response = req.CreateResponse(statusCode);
                   response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                   return response;
               });


        webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>()))
                            .Returns(Task.FromResult<HttpWebResponse>(webResponse.Object));

        //Act
        var result = await removeParticipant.Run(request.Object);

        //Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_BadRequestReturnedFromRemoveDataService_InternalServerError()
    {

        //Arrange
        var json = JsonSerializer.Serialize(participant);

        setupRequest(json);

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), null))
               .Returns((HttpStatusCode statusCode, HttpRequestData req) =>
               {
                   var response = req.CreateResponse(statusCode);
                   response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                   return response;
               });


        webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.BadRequest);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>()))
                            .Returns(Task.FromResult<HttpWebResponse>(webResponse.Object));

        //Act
        var result = await removeParticipant.Run(request.Object);

        //Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_AnErrorIsThrown_BadRequest()
    {
        var json = JsonSerializer.Serialize(participant);

        setupRequest(json);

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), null))
               .Returns((HttpStatusCode statusCode, HttpRequestData req) =>
               {
                   var response = req.CreateResponse(statusCode);
                   response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                   return response;
               });

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>()))
        .Throws(new Exception("there has been a problem"));

        var result = await removeParticipant.Run(request.Object);

        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
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
