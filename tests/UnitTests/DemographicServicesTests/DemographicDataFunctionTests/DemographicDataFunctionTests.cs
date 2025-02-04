namespace NHS.CohortManager.Tests.UnitTests.DemographicServicesTests;

using System.Net;
using System.Text.Json;
using Common;
using Data.Database;
using DataServices.Client;
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
    private readonly Mock<FunctionContext> _context = new();
    private Mock<HttpRequestData> _request;
    private readonly Mock<HttpWebResponse> _webResponse = new();
    private readonly ServiceCollection _serviceCollection = new();
    private readonly Participant _participant;
    private readonly SetupRequest _setupRequest = new();
    private readonly Mock<IDataServiceClient<ParticipantDemographic>> _participantDemographic = new();

    public DemographicDataFunctionTests()
    {
        _request = new Mock<HttpRequestData>(_context.Object);
        var serviceProvider = _serviceCollection.BuildServiceProvider();
        _context.SetupProperty(c => c.InstanceServices, serviceProvider);

        Environment.SetEnvironmentVariable("DemographicDataFunctionURI", "DemographicDataFunctionURI");

        _participant = new Participant()
        {
            FirstName = "Joe",
            FamilyName = "Bloggs",
            NhsNumber = "11111111111111111",
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
    }


    [TestMethod]
    public async Task Run_return_DemographicDataSavedPostRequest_InternalServerEver()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participant);
        var sut = new DemographicDataFunction(_logger.Object, _createResponse.Object, _participantDemographic.Object);
        _request = _setupRequest.Setup(json);
        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.InternalServerError);

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
        var participant = new ParticipantDemographic
        {
            ParticipantId = 123456789,
            NhsNumber = 987654321
        };

        var json = JsonSerializer.Serialize(_participant);
        var sut = new DemographicDataFunction(_logger.Object, _createResponse.Object, _participantDemographic.Object);


        _request = _setupRequest.Setup("987654321");

        _participantDemographic.Setup(x => x.GetSingleByFilter(It.IsAny<System.Linq.Expressions.Expression<Func<ParticipantDemographic, bool>>>())).ReturnsAsync(participant);

        // Act
        _request.Setup(x => x.Query).Returns(new System.Collections.Specialized.NameValueCollection() { { "Id", "987654321" } });

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
        var sut = new DemographicDataFunction(_logger.Object, _createResponse.Object, _participantDemographic.Object);

        _request = _setupRequest.Setup("11");

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), ""))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string ResponseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });


        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.InternalServerError);

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
        var sut = new DemographicDataFunction(_logger.Object, _createResponse.Object, _participantDemographic.Object);
        _request = _setupRequest.Setup(json);

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), ""))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string ResponseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.InternalServerError);

        // Act
        _request.Setup(r => r.Method).Returns("POST");
        var result = await sut.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
        _logger.Verify(log =>
        log.Log(
            LogLevel.Error,
            0,
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()
        ), Times.AtLeastOnce(), "There has been an error saving demographic data:");

    }
}
