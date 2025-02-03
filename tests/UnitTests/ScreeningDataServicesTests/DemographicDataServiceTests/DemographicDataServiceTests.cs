namespace NHS.CohortManager.Tests.UnitTests.DemographicDataServiceTests;

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
    private readonly Mock<IExceptionHandler> _exceptionHandler = new();

    public DemographicDataServiceTests()
    {
        _request = new Mock<HttpRequestData>(_context.Object);
        var serviceProvider = _serviceCollection.BuildServiceProvider();
        _context.SetupProperty(c => c.InstanceServices, serviceProvider);

        Environment.SetEnvironmentVariable("DemographicDataFunctionURI", "DemographicDataFunctionURI");

        _participant = new Participant()
        {
            FirstName = "Joe",
            FamilyName = "Bloggs",
            NhsNumber = "1",
            RecordType = Actions.New
        };
    }

    [TestMethod]
    public async Task Run_ReturnDemographicData_OK()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participant);
        var sut = new DemographicDataService(_createResponse.Object);

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

        _createDemographicData.Setup(x => x.GetDemographicData(It.IsAny<string>())).ReturnsAsync(new Demographic()
        {
            ParticipantId = "1"
        });

        // Act
        _request.Setup(x => x.Method).Returns("GET");
        var result = await sut.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Gone, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ReturnDemographicData_NotFound()
    {
        // Arrange
        var sut = new DemographicDataService(_createResponse.Object);
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

        _createDemographicData.Setup(x => x.GetDemographicData(It.IsAny<string>())).ReturnsAsync((Demographic)null);
        // Act
        _request.Setup(x => x.Method).Returns("GET");
        var result = await sut.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Gone, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_DemographicData_InternalServerError()
    {
        // Arrange
        var sut = new DemographicDataService(_createResponse.Object);
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
        Assert.AreEqual(HttpStatusCode.Gone, result.StatusCode);
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
