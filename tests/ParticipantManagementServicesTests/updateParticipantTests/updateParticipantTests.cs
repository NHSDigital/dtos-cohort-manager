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
    Mock<ILogger<UpdateParticipantFunction>> _logger;
    Mock<ICallFunction> _callFunction;
    ServiceCollection serviceCollection;
    Mock<FunctionContext> _context;
    Mock<HttpRequestData> _request;
    Mock<ICreateResponse> _createResponse;

    Mock<HttpWebResponse> _webResponse;

    Mock<ICheckDemographic> _checkDemographic = new();

    Mock<ICreateParticipant> createParticipant = new();

    Mock<HttpWebResponse> _validationWebResponse = new();

    Mock<HttpWebResponse> _updateParticipantWebResponse = new();

    Participant _participant;
    public UpdateParticipantTests()
    {
        Environment.SetEnvironmentVariable("UpdateParticipant", "UpdateParticipant");
        Environment.SetEnvironmentVariable("DemographicURIGet", "DemographicURIGet");
        Environment.SetEnvironmentVariable("StaticValidationURL", "StaticValidationURL");

        _logger = new Mock<ILogger<UpdateParticipantFunction>>();
        _createResponse = new Mock<ICreateResponse>();
        _callFunction = new Mock<ICallFunction>();
        _context = new Mock<FunctionContext>();
        _request = new Mock<HttpRequestData>(_context.Object);
        _webResponse = new Mock<HttpWebResponse>();

        serviceCollection = new ServiceCollection();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        _context.SetupProperty(c => c.InstanceServices, serviceProvider);

        _participant = new Participant()
        {
            NHSId = "1",
        };
    }

    [TestMethod]
    public async Task Run_Should_Return_BadRequest_And_Not_Update_Participant_When_Validation_Fails()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participant);
        setupRequest(json);
        var sut = new UpdateParticipantFunction(_logger.Object, _createResponse.Object, _callFunction.Object, _checkDemographic.Object, createParticipant.Object);

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.BadRequest);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("StaticValidationURL")), It.IsAny<string>()))
                        .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        var result = await sut.Run(_request.Object);

        _logger.Verify(log =>
            log.Log(
            LogLevel.Information,
            0,
            It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("The participant has not been updated due to a bad request.")),
            null,
            (Func<object, Exception, string>)It.IsAny<object>()
            ));
    }

    [TestMethod]
    public async Task Run_Should_Return_Ok_When_Participant_Update_Succeeds()
    {

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        var json = JsonSerializer.Serialize(_participant);

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("UpdateParticipant")), It.IsAny<string>()))
                        .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("StaticValidationURL")), It.IsAny<string>()))
                        .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunction.Setup(call => call.SendGet(It.IsAny<string>()))
                        .Returns(Task.FromResult<string>(""));

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), It.Is<string>(s => s.Contains("DemographicURIGet"))))
                        .Returns(Task.FromResult<Demographic>(new Demographic()));

        setupRequest(json);

        var sut = new UpdateParticipantFunction(_logger.Object, _createResponse.Object, _callFunction.Object, _checkDemographic.Object, createParticipant.Object);

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        _logger.Verify(log =>
           log.Log(
           LogLevel.Information,
           0,
           It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("Participant updated.")),
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
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("UpdateParticipant")), It.IsAny<string>()))
                        .Returns(Task.FromResult<HttpWebResponse>(_updateParticipantWebResponse.Object));

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), ""))
               .Returns((HttpStatusCode statusCode, HttpRequestData req, string responseBody) =>
               {
                   var response = req.CreateResponse(statusCode);
                   response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                   return response;
               });


        var sut = new UpdateParticipantFunction(_logger.Object, _createResponse.Object, _callFunction.Object, _checkDemographic.Object, createParticipant.Object);

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

        _validationWebResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s == "StaticValidationURL"), json))
            .Returns(Task.FromResult<HttpWebResponse>(_validationWebResponse.Object));


        _updateParticipantWebResponse.Setup(x => x.StatusCode).Throws(new Exception("an error occurred"));
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("UpdateParticipant")), It.IsAny<string>()))
                        .Returns(Task.FromResult<HttpWebResponse>(_updateParticipantWebResponse.Object));

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), ""))
               .Returns((HttpStatusCode statusCode, HttpRequestData req, string responseBody) =>
               {
                   var response = req.CreateResponse(statusCode);
                   response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                   return response;
               });

        var sut = new UpdateParticipantFunction(_logger.Object, _createResponse.Object, _callFunction.Object, _checkDemographic.Object, createParticipant.Object);

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        _createResponse.Verify(x => x.CreateHttpResponse(HttpStatusCode.InternalServerError, _request.Object, ""), Times.Once());

        _logger.Verify(log =>
          log.Log(
          LogLevel.Information,
          0,
          It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("Update participant failed")),
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
