namespace NHS.CohortManager.Tests.UnitTests.DemographicServicesTests;

using System.Net;
using Common;
using Common.Interfaces;
using Microsoft.Extensions.Options;
using Model;
using Moq;
using NHS.CohortManager.DemographicServices;
using NHS.CohortManager.Tests.TestUtils;

[TestClass]
public class RetrievePdsDemographicTests : DatabaseTestBaseSetup<RetrievePdsDemographic>
{
    private static readonly Mock<IOptions<RetrievePDSDemographicConfig>> _mockConfig = new();
    private static readonly Mock<IFhirPatientDemographicMapper> _mockFhirPatientDemographicMapper = new();
    private static readonly Mock<IBearerTokenService> _bearerTokenService = new();
    private static readonly Mock<IHttpClientFunction> _httpClientFunction = new();
    private static readonly Mock<IPdsProcessor> _mockPdsProcessor = new();
    private const string _validNhsNumber = "9000000009";

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
    [DataRow(null)]
    [DataRow("nems-file-123.xml")]
    public async Task Run_WhenPdsReturnsNotFound_TheNotFoundResponseIsProcessedAndReturned(string? sourceFileName)
    {
        // Arrange
        _bearerTokenService.Setup(x => x.GetBearerToken()).ReturnsAsync("token");
        _mockFhirPatientDemographicMapper.Setup(x => x.ParseFhirJson(It.IsAny<string>()))
            .Returns(new PdsDemographic { ConfidentialityCode = "R" });

        var notFoundResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("{}")
        };
        _httpClientFunction.Setup(x => x.SendPdsGet(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(notFoundResponse);
        _httpClientFunction.Setup(x => x.GetResponseText(It.IsAny<HttpResponseMessage>()))
            .ReturnsAsync("");

        SetupRequest("{}");
        SetupRequestWithQueryParams(new Dictionary<string, string>
        {
            {"nhsNumber", _validNhsNumber }
        });

        var url = $"http://localhost/api/RetrievePdsDemographic?nhsNumber={_validNhsNumber}";
        if (sourceFileName != null)
        {
            url += $"&sourceFileName={Uri.EscapeDataString(sourceFileName)}";
        }
        _request.Setup(r => r.Url).Returns(new Uri(url));

        // Act
        var response = await _service.Run(_request.Object);

        // Assert
        _mockPdsProcessor.Verify(p => p.ProcessPdsNotFoundResponse(
            notFoundResponse,
            _validNhsNumber,
            It.Is<string?>(s => s == sourceFileName)
        ), Times.Once);
        _mockPdsProcessor.VerifyNoOtherCalls();
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
