
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
    }


    [TestMethod]
    public async Task Run_return_DemographicDataSaved_OK()
    {

        var json = JsonSerializer.Serialize(participant);
        var sut = new DemographicDataFunction(_logger.Object, _createResponse.Object, _callFunction.Object);

        setupRequest(json);

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>()))
            .Returns((HttpStatusCode statusCode, HttpRequestData req) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });


        webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("DemographicDataFunctionURI")), It.IsAny<string>()))
                            .Returns(Task.FromResult<HttpWebResponse>(webResponse.Object));

        //Act
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

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>()))
            .Returns((HttpStatusCode statusCode, HttpRequestData req) =>
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

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>()))
            .Returns((HttpStatusCode statusCode, HttpRequestData req) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });


        webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.InternalServerError);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("DemographicDataFunctionURI")), It.IsAny<string>()))
                            .ThrowsAsync(new Exception("there was an error"));

        //Act
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