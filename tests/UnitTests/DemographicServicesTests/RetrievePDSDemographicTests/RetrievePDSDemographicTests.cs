namespace NHS.CohortManager.Tests.UnitTests.DemographicServicesTests;

using System.Net;
using Common;
using Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using Moq;
using NHS.CohortManager.DemographicServices;
using NHS.CohortManager.Tests.TestUtils;
using NHS.Screening.RetrievePDSDemographic;

[TestClass]
public class RetrievePdsDemographicTests : DatabaseTestBaseSetup<RetrievePdsDemographic>
{
    private static readonly Mock<IHttpClientFunction> _httpClientFunction = new();
    private static readonly Mock<IOptions<RetrievePDSDemographicConfig>> _config = new();
    private static readonly Mock<IFhirParserHelper> _fhirParserHelperMock = new();
    private readonly string _validNhsNumber = "3112728165";

    public RetrievePdsDemographicTests() : base((conn, logger, transaction, command, response) =>
    new RetrievePdsDemographic(
        logger,
        response,
        _httpClientFunction.Object,
        _fhirParserHelperMock.Object,
        _config.Object))
    {
        CreateHttpResponseMock();
    }

    [TestInitialize]
    public void TestInitialize()
    {
        var testConfig = new RetrievePDSDemographicConfig
        {
            RetrievePdsParticipantURL = "RetrievePdsParticipantURL"
        };

        _config.Setup(c => c.Value).Returns(testConfig);

        _httpClientFunction.Reset();
        _fhirParserHelperMock.Reset();

        _service = new RetrievePdsDemographic(
            _loggerMock.Object,
            _createResponseMock.Object,
            _httpClientFunction.Object,
            _fhirParserHelperMock.Object,
            _config.Object);

        _request = SetupRequest(string.Empty);
    }

    [DataRow("")]
    [DataRow(null)]
    [DataRow("0000000000")]
    [TestMethod]
    public async Task Run_InvalidNhsNumber_ReturnsBadRequest(string invalidNhsNumber)
    {
        // Arrange
        SetupRequestWithQueryParams(new Dictionary<string, string> { { "nhsNumber", invalidNhsNumber } });

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        _httpClientFunction.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()), Times.Never());
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ValidNhsNumberExistsInPds_ReturnsOk()
    {
        // Arrange
        SetupRequestWithQueryParams(new Dictionary<string, string> { { "nhsNumber", _validNhsNumber } });
        _httpClientFunction.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        _fhirParserHelperMock.Setup(x => x.ParseFhirJson(It.IsAny<string>())).Returns(new PDSDemographic());

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        _httpClientFunction.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()), Times.Once());
        _fhirParserHelperMock.Verify(x => x.ParseFhirJson(It.IsAny<string>()), Times.Once());
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ValidNhsNumberDoesNotExistInPds_ReturnsNotFound()
    {
        // Arrange
        SetupRequestWithQueryParams(new Dictionary<string, string> { { "nhsNumber", _validNhsNumber } });
        _httpClientFunction.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        _httpClientFunction.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()), Times.Once());
        _fhirParserHelperMock.Verify(x => x.ParseFhirJson(It.IsAny<string>()), Times.Never());
        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ValidNhsNumberOtherRequestError_ReturnsInternalServerError()
    {
        // Arrange
        SetupRequestWithQueryParams(new Dictionary<string, string> { { "nhsNumber", _validNhsNumber } });
        _httpClientFunction.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        _httpClientFunction.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()), Times.Once());
        _fhirParserHelperMock.Verify(x => x.ParseFhirJson(It.IsAny<string>()), Times.Never());
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ValidNhsNumberRequestThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        SetupRequestWithQueryParams(new Dictionary<string, string> { { "nhsNumber", _validNhsNumber } });
        _httpClientFunction.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>())).Throws(new Exception("There was an error"));

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        _httpClientFunction.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()), Times.Once());
        _fhirParserHelperMock.Verify(x => x.ParseFhirJson(It.IsAny<string>()), Times.Never());
        _loggerMock.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("There has been an error fetching PDS participant data")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }
}
