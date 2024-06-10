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
using screeningDataServices;

[TestClass]
public class DemographicDataServiceTests
{
    private readonly Mock<ILogger<DemographicDataService>> _logger = new();
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<FunctionContext> context = new();
    private readonly Mock<HttpRequestData> request;
    private readonly ServiceCollection serviceCollection = new();
    private readonly Participant participant;
    private readonly Mock<ICreateDemographicData> _createDemographicData = new();

    public DemographicDataServiceTests()
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
        var sut = new DemographicDataService(_logger.Object, _createResponse.Object, _createDemographicData.Object);

        setupRequest(json);

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), ""))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string ResponseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });
        request.Setup(x => x.Method).Returns("POST");
        _createDemographicData.Setup(x => x.InsertDemographicData(It.IsAny<Participant>())).Returns(true);

        //Act
        var result = await sut.Run(request.Object);

        //Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_return_POST_DemographicNotSaved_InternalServerError()
    {

        var json = JsonSerializer.Serialize(participant);
        var sut = new DemographicDataService(_logger.Object, _createResponse.Object, _createDemographicData.Object);

        setupRequest(json);

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), ""))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string ResponseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });

        _createDemographicData.Setup(x => x.InsertDemographicData(It.IsAny<Participant>())).Returns(false);

        //Act
        request.Setup(x => x.Method).Returns("POST");
        var result = await sut.Run(request.Object);

        //Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_return_DemographicNotSavedThrows_InternalServerError()
    {

        var json = JsonSerializer.Serialize(participant);
        var sut = new DemographicDataService(_logger.Object, _createResponse.Object, _createDemographicData.Object);

        setupRequest(json);

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), ""))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string ResponseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });

        _createDemographicData.Setup(x => x.InsertDemographicData(It.IsAny<Participant>())).Throws(new Exception("there has been an error"));

        //Act
        request.Setup(x => x.Method).Returns("POST");
        var result = await sut.Run(request.Object);

        //Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Get_return_DemographicData_OK()
    {
        // Arrange
        var json = JsonSerializer.Serialize(participant);
        var sut = new DemographicDataService(_logger.Object, _createResponse.Object, _createDemographicData.Object);

        setupRequest(json);

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string ResponseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                response.WriteString(ResponseBody);
                return response;
            });

        _createDemographicData.Setup(x => x.GetDemographicData(It.IsAny<string>())).Returns(new Demographic()
        {
            NhsNumber = "1"
        });

        // Act
        request.Setup(x => x.Method).Returns("GET");
        var result = await sut.Run(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Get_return_DemographicData_NotFound()
    {
        // Arrange
        var sut = new DemographicDataService(_logger.Object, _createResponse.Object, _createDemographicData.Object);
        var json = JsonSerializer.Serialize(participant);
        setupRequest(json);

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
        request.Setup(x => x.Method).Returns("GET");
        var result = await sut.Run(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
    }

    [TestMethod]
    public async Task Get_return_DemographicData_InternalServerError()
    {
        // Arrange
        var sut = new DemographicDataService(_logger.Object, _createResponse.Object, _createDemographicData.Object);
        var json = JsonSerializer.Serialize(participant);
        setupRequest(json);

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
        request.Setup(x => x.Method).Returns("GET");
        var result = await sut.Run(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
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