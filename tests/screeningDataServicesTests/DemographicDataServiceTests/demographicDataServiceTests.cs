namespace DemographicDataServiceTests;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Data.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Model;
using Moq;
using ScreeningDataServices;

[TestClass]
public class DemographicDataServiceTests
{
    private readonly Mock<ILogger<DemographicDataService>> _logger = new();
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<FunctionContext> _context = new();
    private readonly Mock<HttpRequestData> _request;
    private readonly ServiceCollection _serviceCollection = new();
    private readonly Participant _participant;
    private readonly Mock<ICreateDemographicData> _createDemographicData = new();

    public DemographicDataServiceTests()
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
    }

    [TestMethod]
    public async Task Run_return_DemographicDataSaved_OK()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participant);
        var sut = new DemographicDataService(_logger.Object, _createResponse.Object, _createDemographicData.Object);

        SetupRequest(json);

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), ""))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string ResponseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });
        _request.Setup(x => x.Method).Returns("POST");
        _createDemographicData.Setup(x => x.InsertDemographicData(It.IsAny<Demographic>())).Returns(true);

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_return_POST_DemographicNotSaved_InternalServerError()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participant);
        var sut = new DemographicDataService(_logger.Object, _createResponse.Object, _createDemographicData.Object);

        SetupRequest(json);

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), ""))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string ResponseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });

        _createDemographicData.Setup(x => x.InsertDemographicData(It.IsAny<Demographic>())).Returns(false);

        // Act
        _request.Setup(x => x.Method).Returns("POST");
        var result = await sut.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_return_DemographicNotSavedThrows_InternalServerError()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participant);
        var sut = new DemographicDataService(_logger.Object, _createResponse.Object, _createDemographicData.Object);

        SetupRequest(json);

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), ""))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string ResponseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });

        _createDemographicData.Setup(x => x.InsertDemographicData(It.IsAny<Demographic>())).Throws(new Exception("there has been an error"));

        // Act
        _request.Setup(x => x.Method).Returns("POST");
        var result = await sut.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Get_return_DemographicData_OK()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participant);
        var sut = new DemographicDataService(_logger.Object, _createResponse.Object, _createDemographicData.Object);

        SetupRequest(json);

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string ResponseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                response.WriteString(ResponseBody);
                return response;
            });
        _request.Setup(x => x.Query).Returns(new System.Collections.Specialized.NameValueCollection() { { "Id", "1" } });

        _createDemographicData.Setup(x => x.GetDemographicData(It.IsAny<string>())).Returns(new Demographic()
        {
            NhsNumber = "1"
        });

        // Act
        _request.Setup(x => x.Method).Returns("GET");
        var result = await sut.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Get_return_DemographicData_NotFound()
    {
        // Arrange
        var sut = new DemographicDataService(_logger.Object, _createResponse.Object, _createDemographicData.Object);
        var json = JsonSerializer.Serialize(_participant);
        SetupRequest(json);


        _request.Setup(x => x.Query).Returns(new System.Collections.Specialized.NameValueCollection() { { "Id", "1" } });
        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string ResponseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                response.WriteString(ResponseBody);
                return response;
            });

        _createDemographicData.Setup(x => x.GetDemographicData(It.IsAny<string>())).Returns((Demographic)null);

        // Act
        _request.Setup(x => x.Method).Returns("GET");
        var result = await sut.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
    }

    [TestMethod]
    public async Task Get_return_DemographicData_InternalServerError()
    {
        // Arrange
        var sut = new DemographicDataService(_logger.Object, _createResponse.Object, _createDemographicData.Object);
        var json = JsonSerializer.Serialize(_participant);
        SetupRequest(json);

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string ResponseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                response.WriteString(ResponseBody);
                return response;
            });

        _createDemographicData.Setup(x => x.GetDemographicData(It.IsAny<string>())).Throws(new Exception("there has been an error"));

        // Act
        _request.Setup(x => x.Method).Returns("GET");
        var result = await sut.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
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
