namespace NHS.CohortManager.Tests.UnitTests.DemographicServicesTests;

using System.Net;
using Common;
using Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using Moq;
using NHS.CohortManager.DemographicServices;
using DataServices.Client;
using NHS.CohortManager.Tests.TestUtils;
using System.Linq.Expressions;
using Microsoft.Extensions.Caching.Memory;
using System.ComponentModel;

[TestClass]
public class RetrievePdsDemographicTests : DatabaseTestBaseSetup<RetrievePdsDemographic>
{
    private static readonly Mock<IHttpClientFunction> _mockHttpClientFunction = new();
    private static readonly Mock<IOptions<RetrievePDSDemographicConfig>> _mockConfig = new();
    private static readonly Mock<IFhirPatientDemographicMapper> _mockFhirPatientDemographicMapper = new();
    private static Mock<IBearerTokenService> _bearerTokenService = new();

    private static Mock<IHttpClientFunction> _httpClientFunction = new();

    private static readonly Mock<IPdsProcessor> _mockPdsProcessor = new();


    private const string _validNhsNumber = "9000000009";
    private const long _validNhsNumberLong = 9000000009;

    public RetrievePdsDemographicTests() : base((conn, logger, transaction, command, response) =>
    new RetrievePdsDemographic(
        logger,
        response,
        _httpClientFunction.Object,
        _mockFhirPatientDemographicMapper.Object,
        _mockConfig.Object,
        _bearerTokenService.Object,
        _mockPdsProcessor.Object
        ))
    {
        CreateHttpResponseMock();
        // Provide required config to avoid null access in function
        var cfg = new RetrievePDSDemographicConfig
        {
            RetrievePdsParticipantURL = "http://example.org/pds",
            DemographicDataServiceURL = "http://example.org/ds",
            Audience = "aud",
            KId = "kid",
            AuthTokenURL = "http://example.org/auth",
            ParticipantManagementTopic = "topic",
            ServiceBusConnectionString_client_internal = "Endpoint=sb://test/",
            UseFakePDSServices = false
        };
        _mockConfig.Setup(x => x.Value).Returns(cfg);
        // Recreate service so it captures configured options Value
        _service = new RetrievePdsDemographic(
            _loggerMock.Object,
            _createResponseMock.Object,
            _httpClientFunction.Object,
            _mockFhirPatientDemographicMapper.Object,
            _mockConfig.Object,
            _bearerTokenService.Object,
            _mockPdsProcessor.Object
        );
    }

    [TestMethod]
    public async Task Run_NotFound_WithoutSourceFileName_ForwardsNull()
    {
        // Arrange
        _bearerTokenService.Setup(x => x.GetBearerToken()).ReturnsAsync("token");
        _mockFhirPatientDemographicMapper.Setup(x => x.ParseFhirJson(It.IsAny<string>()))
            .Returns(new PdsDemographic { ConfidentialityCode = "R" });

        var okResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        };
        _httpClientFunction.Setup(x => x.SendPdsGet(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(okResponse);
        _httpClientFunction.Setup(x => x.GetResponseText(It.IsAny<HttpResponseMessage>()))
            .ReturnsAsync("{}");

        // Build request with nhsNumber only
        SetupRequest("{}");
        SetupRequestWithQueryParams(new Dictionary<string, string>
        {
            {"nhsNumber", _validNhsNumber }
        });
        _request.Setup(r => r.Url).Returns(new Uri($"http://localhost/api/RetrievePdsDemographic?nhsNumber={_validNhsNumber}"));

        // Act
        await _service.Run(_request.Object);

        // Assert
        _mockPdsProcessor.Verify(p => p.ProcessPdsNotFoundResponse(
            It.IsAny<HttpResponseMessage>(),
            _validNhsNumber,
            It.Is<string?>(s => s == null)
        ), Times.Once);
    }

    [TestMethod]
    public async Task Run_NotFound_WithSourceFileName_ForwardsFilename()
    {
        // Arrange
        _bearerTokenService.Setup(x => x.GetBearerToken()).ReturnsAsync("token");
        _mockFhirPatientDemographicMapper.Setup(x => x.ParseFhirJson(It.IsAny<string>()))
            .Returns(new PdsDemographic { ConfidentialityCode = "R" });

        var okResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        };
        _httpClientFunction.Setup(x => x.SendPdsGet(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(okResponse);
        _httpClientFunction.Setup(x => x.GetResponseText(It.IsAny<HttpResponseMessage>()))
            .ReturnsAsync("{}");

        var fileName = "nems-file-123.xml";

        // Build request with nhsNumber and sourceFileName
        SetupRequest("{}");
        SetupRequestWithQueryParams(new Dictionary<string, string>
        {
            {"nhsNumber", _validNhsNumber }
        });
        _request.Setup(r => r.Url).Returns(new Uri($"http://localhost/api/RetrievePdsDemographic?nhsNumber={_validNhsNumber}&sourceFileName={Uri.EscapeDataString(fileName)}"));

        // Act
        await _service.Run(_request.Object);

        // Assert
        _mockPdsProcessor.Verify(p => p.ProcessPdsNotFoundResponse(
            It.IsAny<HttpResponseMessage>(),
            _validNhsNumber,
            It.Is<string?>(s => s == fileName)
        ), Times.Once);
    }
}
