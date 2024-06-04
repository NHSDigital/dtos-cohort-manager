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
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly Mock<FunctionContext> context = new();
    private readonly Mock<HttpRequestData> request;
    private readonly Mock<HttpWebResponse> webResponse = new();
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

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>()))
            .Returns((HttpStatusCode statusCode, HttpRequestData req) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });

        _createDemographicData.Setup(x => x.InsertDemographicData(It.IsAny<Participant>())).Returns(true);

        //Act
        var result = await sut.Run(request.Object);

        //Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_return_DemographicNotSaved_InternalServerError()
    {

        var json = JsonSerializer.Serialize(participant);
        var sut = new DemographicDataService(_logger.Object, _createResponse.Object, _createDemographicData.Object);

        setupRequest(json);

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>()))
            .Returns((HttpStatusCode statusCode, HttpRequestData req) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });

        _createDemographicData.Setup(x => x.InsertDemographicData(It.IsAny<Participant>())).Returns(false);

        //Act
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

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>()))
            .Returns((HttpStatusCode statusCode, HttpRequestData req) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });

        _createDemographicData.Setup(x => x.InsertDemographicData(It.IsAny<Participant>())).Throws(new Exception("there has been an error"));

        //Act
        var result = await sut.Run(request.Object);

        //Assert 
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