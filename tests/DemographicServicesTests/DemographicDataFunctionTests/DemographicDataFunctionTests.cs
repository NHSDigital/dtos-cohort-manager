namespace NHS.CohortManager.Tests.DemographicServicesTests;

using System.Net;
using System.Text.Json;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Model;
using Moq;
using NHS.CohortManager.DemographicServices;
using NHS.CohortManager.Tests.TestUtils;

[TestClass]
public class DemographicDataFunctionTests
{
    private readonly Mock<ILogger<DemographicDataFunction>> _logger = new();
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly Mock<FunctionContext> _context = new();
    private Mock<HttpRequestData> _request;
    private readonly Mock<HttpWebResponse> _webResponse = new();
    private readonly ServiceCollection _serviceCollection = new();
    private readonly Participant _participant;
    private readonly SetupRequest _setupRequest = new();

    public DemographicDataFunctionTests()
    {
        _request = new Mock<HttpRequestData>(_context.Object);
        var serviceProvider = _serviceCollection.BuildServiceProvider();
        _context.SetupProperty(c => c.InstanceServices, serviceProvider);

        Environment.SetEnvironmentVariable("DemographicDataFunctionURI", "DemographicDataFunctionURI");

        _participant = new Participant()
        {
            FirstName = "Joe",
            Surname = "Bloggs",
            NhsNumber = "1",
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


        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        _callFunction.Setup(call => call.SendPost(It.IsAny<string>(), It.IsAny<string>()))
                            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        _callFunction.Setup(call => call.SendGet(It.IsAny<string>()))
                        .Returns(Task.FromResult<string>(""));
    }

    [TestMethod]
    public async Task Run_return_DemographicDataSavedPostRequest_OK()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participant);
        var sut = new DemographicDataFunction(_logger.Object, _createResponse.Object, _callFunction.Object);

        _request = _setupRequest.Setup(json);

        // Act
        _request.Setup(r => r.Method).Returns("POST");
        var result = await sut.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_return_DemographicDataSavedPostRequest_InternalServerEver()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participant);
        var sut = new DemographicDataFunction(_logger.Object, _createResponse.Object, _callFunction.Object);

        _request = _setupRequest.Setup(json);
        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.InternalServerError);
        _callFunction.Setup(call => call.SendPost(It.IsAny<string>(), It.IsAny<string>()))
                            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        // Act
        _request.Setup(r => r.Method).Returns("POST");
        var result = await sut.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_return_DemographicDataGetRequest_OK()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participant);
        var sut = new DemographicDataFunction(_logger.Object, _createResponse.Object, _callFunction.Object);

        _request = _setupRequest.Setup(json);

        // Act
        _request.Setup(x => x.Query).Returns(new System.Collections.Specialized.NameValueCollection() { { "Id", "1" } });

        _callFunction.Setup(call => call.SendGet(It.IsAny<string>()))
                            .Returns(Task.FromResult<string>("data"));


        _request.Setup(r => r.Method).Returns("GET");
        var result = await sut.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_return_DemographicDataNotSaved_InternalServerError()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participant);
        var sut = new DemographicDataFunction(_logger.Object, _createResponse.Object, _callFunction.Object);

        _request = _setupRequest.Setup(json);

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), ""))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string ResponseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });


        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.InternalServerError);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("DemographicDataFunctionURI")), It.IsAny<string>()))
                            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_Return_DemographicFunctionThrows_InternalServerError()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participant);
        var sut = new DemographicDataFunction(_logger.Object, _createResponse.Object, _callFunction.Object);

        _request = _setupRequest.Setup(json);

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), ""))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string ResponseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });


        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.InternalServerError);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("DemographicDataFunctionURI")), It.IsAny<string>()))
                            .ThrowsAsync(new Exception("there was an error"));

        // Act
        _request.Setup(r => r.Method).Returns("POST");
        var result = await sut.Run(_request.Object);

        // Assert
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
}
