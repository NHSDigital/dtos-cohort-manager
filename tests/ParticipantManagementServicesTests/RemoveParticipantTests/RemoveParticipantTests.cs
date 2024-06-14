namespace NHS.CohortManager.Tests.ParticipantManagementService;

using System.Net;
using System.Runtime.CompilerServices;
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
    private readonly Mock<ICheckDemographic> checkDemographic = new();
    private readonly Mock<ICreateParticipant> createParticipant = new();
    private readonly Participant participant;
    private readonly ServiceCollection serviceCollection = new();

    public RemoveParticipantTests()
    {
        Environment.SetEnvironmentVariable("markParticipantAsIneligible", "markParticipantAsIneligible");
        Environment.SetEnvironmentVariable("DemographicURIGet", "DemographicURIGet");
        request = new Mock<HttpRequestData>(context.Object);
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
    public async Task Run_return_ParticipantRemovedSuccessfully_OK()
    {
        //Arrange
        var json = JsonSerializer.Serialize(participant);
        var sut = new RemoveParticipantFunction(_logger.Object, _createResponse.Object, _callFunction.Object, checkDemographic.Object, createParticipant.Object);

        setupRequest(json);

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), ""))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string ResponseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });

        webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(webResponse.Object));

        checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), It.Is<string>(s => s.Contains("DemographicURIGet"))))
            .Returns(Task.FromResult<Demographic>(new Demographic()));

        //Act
        var result = await sut.Run(request.Object);

        //Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_BadRequestReturnedFromRemoveDataService_InternalServerError()
    {
        //Arrange
        var json = JsonSerializer.Serialize(participant);
        var sut = new RemoveParticipantFunction(_logger.Object, _createResponse.Object, _callFunction.Object, checkDemographic.Object, createParticipant.Object);

        setupRequest(json);

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), ""))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string responseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });

        webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.BadRequest);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(webResponse.Object));

        //Act
        var result = await sut.Run(request.Object);

        //Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_AnErrorIsThrown_BadRequest()
    {
        var json = JsonSerializer.Serialize(participant);
        var sut = new RemoveParticipantFunction(_logger.Object, _createResponse.Object, _callFunction.Object, checkDemographic.Object, createParticipant.Object);

        setupRequest(json);

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), ""))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string responseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });

        checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), It.Is<string>(s => s.Contains("DemographicURIGet"))))
            .Returns(Task.FromResult<Demographic>(new Demographic()));

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>()))
        .Throws(new Exception("there has been a problem"));

        var result = await sut.Run(request.Object);

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
