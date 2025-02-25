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
using System.Collections.Specialized;

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

        var participant = new ParticipantDemographic
        {
            ParticipantId = 123456789,
            NhsNumber = 987654321,
            CurrentPosting = "A8008",
            PreferredLanguage = "en"
        };

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string responseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });

        _participantDemographic
            .Setup(x => x.GetSingleByFilter(It.IsAny<System.Linq.Expressions.Expression<Func<ParticipantDemographic, bool>>>()))
            .ReturnsAsync(participant);

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task Run_ValidRequest_ReturnOk()
    {
        // Arrange
        _request = _setupRequest.Setup("987654321");
        _request
            .Setup(x => x.Query)
            .Returns(new NameValueCollection() { { "Id", "987654321" } });

        var sut = new DemographicDataFunction(_logger.Object, _createResponse.Object, _participantDemographic.Object);

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task RunExternal_ValidRequest_ReturnFilteredData()
    {
        // Arrange
        _request = _setupRequest.Setup("987654321");
        _request
            .Setup(x => x.Query)
            .Returns(new NameValueCollection() { { "Id", "987654321" } });

        var sut = new DemographicDataFunction(_logger.Object, _createResponse.Object, _participantDemographic.Object);

        // Act
        var result = await sut.RunExternal(_request.Object);

        // Assert
        string json = await AssertionHelper.ReadResponseBodyAsync(result);

        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        StringAssert.Contains("A8008", json);
        StringAssert.Contains("en", json);
    }

    [TestMethod]
    public async Task Run_InvalidRequest_ReturnBadRequest()
    {
        // Arrange
        _request = _setupRequest.Setup("blorg");
        _request
            .Setup(x => x.Query)
            .Returns(new NameValueCollection() { { "Id", "blorg" } });

        var sut = new DemographicDataFunction(_logger.Object, _createResponse.Object, _participantDemographic.Object);

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_DataServiceReturnsException_ReturnInternalServerError()
    {
        // Arrange
        _request = _setupRequest.Setup("987654321");
        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.InternalServerError);
        _request.Setup(x => x.Query).Returns(new NameValueCollection() { { "Id", "987654321" } });

        _participantDemographic
            .Setup(x => x.GetSingleByFilter(It.IsAny<System.Linq.Expressions.Expression<Func<ParticipantDemographic, bool>>>()))
            .Throws(new Exception());

        var sut = new DemographicDataFunction(_logger.Object, _createResponse.Object, _participantDemographic.Object);

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

}
