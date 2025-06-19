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
using NHS.Screening.RetrievePDSDemographic;
using System.Linq.Expressions;
using Microsoft.Azure.Functions.Worker.Http;

[TestClass]
public class RetrievePdsDemographicTests : DatabaseTestBaseSetup<RetrievePdsDemographic>
{
    private static readonly Mock<IHttpClientFunction> _httpClientFunction = new();
    private static readonly Mock<IOptions<RetrievePDSDemographicConfig>> _config = new();
    private static readonly Mock<IFhirPatientDemographicMapper> _fhirPatientDemographicMapperMock = new();
    private static readonly Mock<IDataServiceClient<ParticipantDemographic>> _mockParticipantDemographicClient = new();
    private readonly string _validNhsNumber = "3112728165";

    public RetrievePdsDemographicTests() : base((conn, logger, transaction, command, response) =>
    new RetrievePdsDemographic(
        logger,
        response,
        _httpClientFunction.Object,
        _fhirPatientDemographicMapperMock.Object,
        _config.Object,
        _mockParticipantDemographicClient.Object))
    {
        CreateHttpResponseMock();
    }

    [TestInitialize]
    public void TestInitialize()
    {
        var testConfig = new RetrievePDSDemographicConfig
        {
            RetrievePdsParticipantURL = "RetrievePdsParticipantURL",
            DemographicDataServiceURL = "DemographicDataServiceURL"
        };

        _config.Setup(c => c.Value).Returns(testConfig);

        _httpClientFunction.Reset();
        _fhirPatientDemographicMapperMock.Reset();

        _service = new RetrievePdsDemographic(
            _loggerMock.Object,
            _createResponseMock.Object,
            _httpClientFunction.Object,
            _fhirPatientDemographicMapperMock.Object,
            _config.Object,
            _mockParticipantDemographicClient.Object);

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
        _httpClientFunction.Verify(x => x.SendPdsGet(It.IsAny<string>()), Times.Never());
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ValidNhsNumberExistsInPds_ReturnsOk_UpdateDatabase()
    {
        // Arrange
        const string expectedResponseContent = "{\"NhsNumber\":\"3112728165\",\"Gender\":2,\"DeathStatus\":2,\"IsInterpreterRequired\":\"True\"}";
        long validNhsNumberLong = long.Parse(_validNhsNumber);

        var expectedDemographic = new PdsDemographic
        {
            NhsNumber = _validNhsNumber,
            Gender = (Model.Enums.Gender?)1,
            DeathStatus = 0,
            IsInterpreterRequired = "true"
        };

        SetupRequestWithQueryParams(new Dictionary<string, string> { { "nhsNumber", _validNhsNumber } });

        // Mock HTTP response
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(expectedResponseContent)
        };

        _httpClientFunction.Setup(x => x.SendPdsGet(It.Is<string>(url => url.Contains(_validNhsNumber))))
            .ReturnsAsync(httpResponse);

        _httpClientFunction.Setup(x => x.GetResponseText(httpResponse))
            .ReturnsAsync(expectedResponseContent);

        _fhirPatientDemographicMapperMock.Setup(x => x.ParseFhirJson(expectedResponseContent))
            .Returns(expectedDemographic);

        _mockParticipantDemographicClient.Setup(x =>
                x.GetSingleByFilter(It.Is<Expression<Func<ParticipantDemographic, bool>>>(f =>
                    f.Compile()(new ParticipantDemographic { NhsNumber = validNhsNumberLong }))))
            .ReturnsAsync(new ParticipantDemographic { NhsNumber = validNhsNumberLong });

        _mockParticipantDemographicClient.Setup(x =>
                x.Update(It.Is<ParticipantDemographic>(p => p.NhsNumber == validNhsNumberLong)))
            .ReturnsAsync(true);

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        // Verify HTTP call was made with correct URL
        _httpClientFunction.Verify(x =>
            x.SendPdsGet(It.Is<string>(url => url.Contains(_validNhsNumber))),
            Times.Once());

        // Verify response was processed
        _httpClientFunction.Verify(x => x.GetResponseText(httpResponse), Times.Once());

        // Verify mapping was called with correct content
        _fhirPatientDemographicMapperMock.Verify(x => x.ParseFhirJson(expectedResponseContent), Times.Once());

        // Verify database operations
        _mockParticipantDemographicClient.Verify(x =>
            x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantDemographic, bool>>>()), Times.AtLeastOnce());

        _mockParticipantDemographicClient.Verify(x => x.Update(It.Is<ParticipantDemographic>(p => p.NhsNumber == validNhsNumberLong)), Times.AtLeastOnce());

        // Verify response
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ValidNhsNumberDoesNotExistInPds_ReturnsNotFound()
    {
        // Arrange
        SetupRequestWithQueryParams(new Dictionary<string, string> { { "nhsNumber", _validNhsNumber } });
        _httpClientFunction.Setup(x => x.SendPdsGet(It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        _httpClientFunction.Verify(x => x.SendPdsGet(It.IsAny<string>()), Times.Once());
        _fhirPatientDemographicMapperMock.Verify(x => x.ParseFhirJson(It.IsAny<string>()), Times.Never());
        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ValidNhsNumberOtherRequestError_ReturnsInternalServerError()
    {
        // Arrange
        SetupRequestWithQueryParams(new Dictionary<string, string> { { "nhsNumber", _validNhsNumber } });
        _httpClientFunction.Setup(x => x.SendPdsGet(It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        _httpClientFunction.Verify(x => x.SendPdsGet(It.IsAny<string>()), Times.Once());
        _fhirPatientDemographicMapperMock.Verify(x => x.ParseFhirJson(It.IsAny<string>()), Times.Never());
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ValidNhsNumberRequestThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        SetupRequestWithQueryParams(new Dictionary<string, string> { { "nhsNumber", _validNhsNumber } });
        _httpClientFunction.Setup(x => x.SendPdsGet(It.IsAny<string>())).Throws(new Exception("There was an error"));

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        _httpClientFunction.Verify(x => x.SendPdsGet(It.IsAny<string>()), Times.Once());
        _fhirPatientDemographicMapperMock.Verify(x => x.ParseFhirJson(It.IsAny<string>()), Times.Never());
        _loggerMock.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("There has been an error fetching PDS participant data")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ValidNhsNumberExistsInPds_ReturnsNotFound_WhenParticipantNotFound()
    {
        // Arrange
        const string expectedResponseContent = "{\"NhsNumber\":\"3112728165\",\"Gender\":2,\"DeathStatus\":2,\"IsInterpreterRequired\":\"True\"}";
        long validNhsNumberLong = long.Parse(_validNhsNumber);

        var expectedDemographic = new PdsDemographic
        {
            NhsNumber = _validNhsNumber,
            Gender = (Model.Enums.Gender?)1,
            DeathStatus = 0,
            IsInterpreterRequired = "true"
        };

        SetupRequestWithQueryParams(new Dictionary<string, string> { { "nhsNumber", _validNhsNumber } });
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(expectedResponseContent)
        };

        _httpClientFunction.Setup(x => x.SendPdsGet(It.Is<string>(url => url.Contains(_validNhsNumber))))
            .ReturnsAsync(httpResponse);

        _httpClientFunction.Setup(x => x.GetResponseText(httpResponse))
            .ReturnsAsync(expectedResponseContent);

        _fhirPatientDemographicMapperMock.Setup(x => x.ParseFhirJson(expectedResponseContent))
            .Returns(expectedDemographic);

        // Mock participant not found in database
        _mockParticipantDemographicClient.Setup(x =>
                x.GetSingleByFilter(It.Is<Expression<Func<ParticipantDemographic, bool>>>(f =>
                    f.Compile()(new ParticipantDemographic { NhsNumber = validNhsNumberLong }))))
            .ReturnsAsync((ParticipantDemographic)null);

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        _httpClientFunction.Verify(x =>
            x.SendPdsGet(It.Is<string>(url => url.Contains(_validNhsNumber))),
            Times.Once());
        _httpClientFunction.Verify(x => x.GetResponseText(httpResponse), Times.Once());
        _fhirPatientDemographicMapperMock.Verify(x => x.ParseFhirJson(expectedResponseContent), Times.Once());
        _mockParticipantDemographicClient.Verify(x =>
            x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantDemographic, bool>>>()),
            Times.AtLeastOnce());
        _mockParticipantDemographicClient.Verify(x =>
            x.Update(It.IsAny<ParticipantDemographic>()),
            Times.AtLeastOnce());

        // Verify response
        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ValidNhsNumberExistsInPds_ReturnsInternalServerError_WhenDatabaseUpdateFails()
    {
        // Arrange
        const string expectedResponseContent = "{\"NhsNumber\":\"3112728165\",\"Gender\":2,\"DeathStatus\":2,\"IsInterpreterRequired\":\"True\"}";
        long validNhsNumberLong = long.Parse(_validNhsNumber);

        var expectedDemographic = new PdsDemographic
        {
            NhsNumber = _validNhsNumber,
            Gender = (Model.Enums.Gender?)1,
            DeathStatus = 0,
            IsInterpreterRequired = "true"
        };

        SetupRequestWithQueryParams(new Dictionary<string, string> { { "nhsNumber", _validNhsNumber } });

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(expectedResponseContent)
        };

        _httpClientFunction.Setup(x => x.SendPdsGet(It.Is<string>(url => url.Contains(_validNhsNumber))))
            .ReturnsAsync(httpResponse);

        _httpClientFunction.Setup(x => x.GetResponseText(httpResponse))
            .ReturnsAsync(expectedResponseContent);

        _fhirPatientDemographicMapperMock.Setup(x => x.ParseFhirJson(expectedResponseContent))
            .Returns(expectedDemographic);

        // Mock participant found but update fails
        _mockParticipantDemographicClient.Setup(x =>
                x.GetSingleByFilter(It.Is<Expression<Func<ParticipantDemographic, bool>>>(f =>
                    f.Compile()(new ParticipantDemographic { NhsNumber = validNhsNumberLong }))))
            .ReturnsAsync(new ParticipantDemographic { NhsNumber = validNhsNumberLong });

        _mockParticipantDemographicClient.Setup(x =>
                x.Update(It.Is<ParticipantDemographic>(p => p.NhsNumber == validNhsNumberLong)))
            .ReturnsAsync(false); // Update fails

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        _httpClientFunction.Verify(x =>
            x.SendPdsGet(It.Is<string>(url => url.Contains(_validNhsNumber))),
            Times.Once());
        _httpClientFunction.Verify(x => x.GetResponseText(httpResponse), Times.Once());
        _fhirPatientDemographicMapperMock.Verify(x => x.ParseFhirJson(expectedResponseContent), Times.Once());
        _mockParticipantDemographicClient.Verify(x =>
            x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantDemographic, bool>>>()),
            Times.Once());
        _mockParticipantDemographicClient.Verify(x =>
            x.Update(It.Is<ParticipantDemographic>(p => p.NhsNumber == validNhsNumberLong)),
            Times.Once());

        // Verify response
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ValidNhsNumberExistsInPds_ReturnsInternalServerError_WhenUnexpectedErrorOccurs()
    {
        // Arrange
        const string expectedResponseContent = "{\"NhsNumber\":\"3112728165\",\"Gender\":2,\"DeathStatus\":2,\"IsInterpreterRequired\":\"True\"}";
        long validNhsNumberLong = long.Parse(_validNhsNumber);

        var expectedDemographic = new PdsDemographic
        {
            NhsNumber = _validNhsNumber,
            Gender = (Model.Enums.Gender?)1,
            DeathStatus = 0,
            IsInterpreterRequired = "true"
        };

        SetupRequestWithQueryParams(new Dictionary<string, string> { { "nhsNumber", _validNhsNumber } });

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(expectedResponseContent)
        };

        _httpClientFunction.Setup(x => x.SendPdsGet(It.Is<string>(url => url.Contains(_validNhsNumber))))
            .ReturnsAsync(httpResponse);

        _httpClientFunction.Setup(x => x.GetResponseText(httpResponse))
            .ReturnsAsync(expectedResponseContent);

        _fhirPatientDemographicMapperMock.Setup(x => x.ParseFhirJson(expectedResponseContent))
            .Returns(expectedDemographic);

        _mockParticipantDemographicClient.Setup(x =>
                x.GetSingleByFilter(It.Is<Expression<Func<ParticipantDemographic, bool>>>(f =>
                    f.Compile()(new ParticipantDemographic { NhsNumber = validNhsNumberLong }))))
            .Throws(new Exception("Unexpected database error"));

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        _httpClientFunction.Verify(x =>
            x.SendPdsGet(It.Is<string>(url => url.Contains(_validNhsNumber))),
            Times.Once());
        _httpClientFunction.Verify(x => x.GetResponseText(httpResponse), Times.Once());
        _fhirPatientDemographicMapperMock.Verify(x => x.ParseFhirJson(expectedResponseContent), Times.Once());
        _mockParticipantDemographicClient.Verify(x =>
            x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantDemographic, bool>>>()),
            Times.AtLeastOnce());
        _mockParticipantDemographicClient.Verify(x =>
            x.Update(It.IsAny<ParticipantDemographic>()),
            Times.AtLeastOnce());

        // Verify response
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

}
