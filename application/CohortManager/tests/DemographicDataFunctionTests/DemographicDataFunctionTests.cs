
using DemographicDataManagementFunction;
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
using Microsoft.Extensions.Primitives;

namespace DemographicDataFunctionTests;

[TestClass]
public class DemographicDataFunctionTests
{
    private readonly Mock<ILogger<DemographicDataFunction>> _logger = new();
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly Mock<FunctionContext> context = new();
    private readonly Mock<HttpRequestData> request;
    private readonly Mock<HttpWebResponse> webResponse = new();
    private readonly ServiceCollection serviceCollection = new();
    private readonly Participant participant;

    public DemographicDataFunctionTests()
    {
        request = new Mock<HttpRequestData>(context.Object);
        var serviceProvider = serviceCollection.BuildServiceProvider();
        context.SetupProperty(c => c.InstanceServices, serviceProvider);

        Environment.SetEnvironmentVariable("DemographicDataFunctionURI", "DemographicDataFunctionURI");

        participant = new Participant()
        {
            FirstName = "Joe",
            Surname = "Bloggs",
            NHSId = "1",
            RecordType = Actions.New
        };

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), ""))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string responseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string responseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });


        webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        _callFunction.Setup(call => call.SendPost(It.IsAny<string>(), It.IsAny<string>()))
                            .Returns(Task.FromResult<HttpWebResponse>(webResponse.Object));

        webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        _callFunction.Setup(call => call.SendGet(It.IsAny<string>()))
                        .Returns(Task.FromResult<string>(""));
    }

    [TestMethod]
    public async Task Run_return_DemographicDataSavedPostRequest_OK()
    {

        var json = JsonSerializer.Serialize(participant);
        var sut = new DemographicDataFunction(_logger.Object, _createResponse.Object, _callFunction.Object);

        setupRequest(json);

        //Act
        request.Setup(r => r.Method).Returns("POST");
        var result = await sut.Run(request.Object);

        //Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_return_DemographicDataSavedPostRequest_InternalServerEver()
    {

        var json = JsonSerializer.Serialize(participant);
        var sut = new DemographicDataFunction(_logger.Object, _createResponse.Object, _callFunction.Object);

        setupRequest(json);
        webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.InternalServerError);
        _callFunction.Setup(call => call.SendPost(It.IsAny<string>(), It.IsAny<string>()))
                            .Returns(Task.FromResult<HttpWebResponse>(webResponse.Object));

        //Act
        request.Setup(r => r.Method).Returns("POST");
        var result = await sut.Run(request.Object);

        //Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_return_DemographicDataGetRequest_OK()
    {

        var json = JsonSerializer.Serialize(participant);
        var sut = new DemographicDataFunction(_logger.Object, _createResponse.Object, _callFunction.Object);

        setupRequest(json);

        //Act
        request.Setup(x => x.Query).Returns(new System.Collections.Specialized.NameValueCollection() { { "Id", "1" } });

        _callFunction.Setup(call => call.SendGet(It.IsAny<string>()))
                            .Returns(Task.FromResult<string>("data"));


        request.Setup(r => r.Method).Returns("GET");
        var result = await sut.Run(request.Object);

        //Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_return_DemographicDataNotSaved_InternalServerError()
    {

        var json = JsonSerializer.Serialize(participant);
        var sut = new DemographicDataFunction(_logger.Object, _createResponse.Object, _callFunction.Object);

        setupRequest(json);

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), ""))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string ResponseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });


        webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.InternalServerError);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("DemographicDataFunctionURI")), It.IsAny<string>()))
                            .Returns(Task.FromResult<HttpWebResponse>(webResponse.Object));

        //Act
        var result = await sut.Run(request.Object);

        //Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_Return_DemographicFunctionThrows_InternalServerError()
    {

        var json = JsonSerializer.Serialize(participant);
        var sut = new DemographicDataFunction(_logger.Object, _createResponse.Object, _callFunction.Object);

        setupRequest(json);

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), ""))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string ResponseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });


        webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.InternalServerError);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("DemographicDataFunctionURI")), It.IsAny<string>()))
                            .ThrowsAsync(new Exception("there was an error"));

        //Act
        request.Setup(r => r.Method).Returns("POST");
        var result = await sut.Run(request.Object);

        //Assert

        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
        _logger.Verify(log =>
        log.Log(
            LogLevel.Error,
            0,
            It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("there has been an error saving demographic data:")),
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
