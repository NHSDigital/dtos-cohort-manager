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

[TestClass]
public class RetrievePdsDemographicTests : DatabaseTestBaseSetup<RetrievePdsDemographic>
{
    private static readonly Mock<IHttpClientFunction> _mockHttpClientFunction = new();
    private static readonly Mock<IOptions<RetrievePDSDemographicConfig>> _mockConfig = new();
    private static readonly Mock<IFhirPatientDemographicMapper> _mockFhirPatientDemographicMapper = new();
    private static readonly Mock<IDataServiceClient<ParticipantDemographic>> _mockParticipantDemographicClient = new();
    private const string _validNhsNumber = "3112728165";
    private const long _validNhsNumberLong = 3112728165;

    public RetrievePdsDemographicTests() : base((conn, logger, transaction, command, response) =>
    new RetrievePdsDemographic(
        logger,
        response,
        _mockHttpClientFunction.Object,
        _mockFhirPatientDemographicMapper.Object,
        _mockConfig.Object,
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

        _mockConfig.Setup(c => c.Value).Returns(testConfig);

        _mockHttpClientFunction.Reset();
        _mockFhirPatientDemographicMapper.Reset();
        _mockParticipantDemographicClient.Reset();

        _service = new RetrievePdsDemographic(
            _loggerMock.Object,
            _createResponseMock.Object,
            _mockHttpClientFunction.Object,
            _mockFhirPatientDemographicMapper.Object,
            _mockConfig.Object,
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
        _mockHttpClientFunction.Verify(x => x.SendPdsGet(It.IsAny<string>()), Times.Never());
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ValidNhsNumberExistsInPdsAndInDatabase_UpdatesDatabaseAndReturnsOK()
    {
        // Arrange
        const string expectedResponseContent = "{\"NhsNumber\":\"3112728165\",\"Gender\":2,\"DeathStatus\":2,\"IsInterpreterRequired\":\"True\"}";

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

        _mockHttpClientFunction.Setup(x => x.SendPdsGet($"{_mockConfig.Object.Value.RetrievePdsParticipantURL}/{_validNhsNumber}"))
            .ReturnsAsync(httpResponse);

        _mockHttpClientFunction.Setup(x => x.GetResponseText(httpResponse))
            .ReturnsAsync(expectedResponseContent);

        _mockFhirPatientDemographicMapper.Setup(x => x.ParseFhirJson(expectedResponseContent))
            .Returns(expectedDemographic);

        _mockParticipantDemographicClient.Setup(x =>
            x.GetSingleByFilter(It.Is<Expression<Func<ParticipantDemographic, bool>>>(f =>
                    f.Compile()(new ParticipantDemographic { NhsNumber = _validNhsNumberLong }))))
            .ReturnsAsync(new ParticipantDemographic { NhsNumber = _validNhsNumberLong });

        _mockParticipantDemographicClient.Setup(x =>
                x.Update(It.Is<ParticipantDemographic>(p => p.NhsNumber == _validNhsNumberLong)))
            .ReturnsAsync(true);

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        // Verify HTTP call was made with correct URL
        _mockHttpClientFunction.Verify(x =>
            x.SendPdsGet($"{_mockConfig.Object.Value.RetrievePdsParticipantURL}/{_validNhsNumber}"),
            Times.Once());

        // Verify response was processed
        _mockHttpClientFunction.Verify(x => x.GetResponseText(httpResponse), Times.Once());

        // Verify mapping was called with correct content
        _mockFhirPatientDemographicMapper.Verify(x => x.ParseFhirJson(expectedResponseContent), Times.Once());

        // Verify database operations
        _mockParticipantDemographicClient.Verify(x =>
            x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantDemographic, bool>>>()), Times.Once());
        _mockParticipantDemographicClient.Verify(x =>
            x.Update(It.Is<ParticipantDemographic>(p => p.NhsNumber == _validNhsNumberLong)), Times.Once());
        _mockParticipantDemographicClient.Verify(x => x.Add(It.IsAny<ParticipantDemographic>()), Times.Never());

        // Verify response
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ValidNhsNumberDoesNotExistInPdsDeletesOldRecord_ReturnsNotFound()
    {
        // Arrange
        SetupRequestWithQueryParams(new Dictionary<string, string> { { "nhsNumber", _validNhsNumber } });
        _mockHttpClientFunction.Setup(x => x.SendPdsGet($"{_mockConfig.Object.Value.RetrievePdsParticipantURL}/{_validNhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        _mockParticipantDemographicClient.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantDemographic, bool>>>()))
        .ReturnsAsync(new ParticipantDemographic() { ParticipantId = 1 });

        _mockParticipantDemographicClient.Setup(x => x.Delete(It.IsAny<string>())).ReturnsAsync(true);

        // Act
        var result = await _service.Run(_request.Object);

        // Assert

        _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Successfully deleted Participant Demographic.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),

        Times.Once);
        _mockHttpClientFunction.Verify(x => x.SendPdsGet($"{_mockConfig.Object.Value.RetrievePdsParticipantURL}/{_validNhsNumber}"), Times.Once());
        _mockFhirPatientDemographicMapper.Verify(x => x.ParseFhirJson(It.IsAny<string>()), Times.Never());
        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ValidNhsNumberDoesNotExistInPdsCannotDeleteOldRecord_ReturnsNotFound()
    {
        // Arrange
        SetupRequestWithQueryParams(new Dictionary<string, string> { { "nhsNumber", _validNhsNumber } });
        _mockHttpClientFunction.Setup(x => x.SendPdsGet($"{_mockConfig.Object.Value.RetrievePdsParticipantURL}/{_validNhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));


        _mockParticipantDemographicClient.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantDemographic, bool>>>()))
        .ReturnsAsync(new ParticipantDemographic() { ParticipantId = 1 });

        _mockParticipantDemographicClient.Setup(x => x.Delete(It.IsAny<string>())).ReturnsAsync(false);


        // Act
        var result = await _service.Run(_request.Object);

        // Assert

        _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Failed to delete Participant Demographic.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),

        Times.Once);
        _mockHttpClientFunction.Verify(x => x.SendPdsGet($"{_mockConfig.Object.Value.RetrievePdsParticipantURL}/{_validNhsNumber}"), Times.Once());
        _mockFhirPatientDemographicMapper.Verify(x => x.ParseFhirJson(It.IsAny<string>()), Times.Never());
        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ValidNhsNumberDoesNotExistInPdsFindsRecordButFailsToDelete_ReturnsNotFound()
    {
        // Arrange
        SetupRequestWithQueryParams(new Dictionary<string, string> { { "nhsNumber", _validNhsNumber } });
        _mockHttpClientFunction.Setup(x => x.SendPdsGet($"{_mockConfig.Object.Value.RetrievePdsParticipantURL}/{_validNhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act
        var result = await _service.Run(_request.Object);

        // Assert

        _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Warning),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Failed to delete Participant Demographic as record did not exist in database.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),

        Times.Once);
        _mockHttpClientFunction.Verify(x => x.SendPdsGet($"{_mockConfig.Object.Value.RetrievePdsParticipantURL}/{_validNhsNumber}"), Times.Once());
        _mockFhirPatientDemographicMapper.Verify(x => x.ParseFhirJson(It.IsAny<string>()), Times.Never());
        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
    }

    //There has been an error retrieving PDS participant data.

    [TestMethod]
    public async Task Run_ValidNhsNumberDoesNotExistInPdsFindsRecordToDeleteThrowsError_InternalServerError()
    {
        // Arrange
        SetupRequestWithQueryParams(new Dictionary<string, string> { { "nhsNumber", _validNhsNumber } });
        _mockHttpClientFunction.Setup(x => x.SendPdsGet($"{_mockConfig.Object.Value.RetrievePdsParticipantURL}/{_validNhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        _mockParticipantDemographicClient.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantDemographic, bool>>>()))
        .ReturnsAsync(new ParticipantDemographic() { ParticipantId = 1 });

        _mockParticipantDemographicClient.Setup(x => x.Delete(It.IsAny<string>())).Throws(new Exception());

        // Act
        var result = await _service.Run(_request.Object);

        // Assert

        _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"There has been an error retrieving PDS participant data.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),

        Times.Once);
        _mockHttpClientFunction.Verify(x => x.SendPdsGet($"{_mockConfig.Object.Value.RetrievePdsParticipantURL}/{_validNhsNumber}"), Times.Once());
        _mockFhirPatientDemographicMapper.Verify(x => x.ParseFhirJson(It.IsAny<string>()), Times.Never());
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    [DataRow(HttpStatusCode.BadRequest)]
    [DataRow(HttpStatusCode.Unauthorized)]
    [DataRow(HttpStatusCode.Forbidden)]
    [DataRow(HttpStatusCode.RequestTimeout)]
    [DataRow(HttpStatusCode.TooManyRequests)]
    [DataRow(HttpStatusCode.InternalServerError)]
    public async Task Run_ValidNhsNumberOtherPdsError_ReturnsInternalServerError(HttpStatusCode pdsStatusCodeError)
    {
        // Arrange
        SetupRequestWithQueryParams(new Dictionary<string, string> { { "nhsNumber", _validNhsNumber } });
        _mockHttpClientFunction.Setup(x => x.SendPdsGet($"{_mockConfig.Object.Value.RetrievePdsParticipantURL}/{_validNhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(pdsStatusCodeError));

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        _mockHttpClientFunction.Verify(x => x.SendPdsGet($"{_mockConfig.Object.Value.RetrievePdsParticipantURL}/{_validNhsNumber}"), Times.Once());
        _mockFhirPatientDemographicMapper.Verify(x => x.ParseFhirJson(It.IsAny<string>()), Times.Never());
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ValidNhsNumberPdsRequestThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        SetupRequestWithQueryParams(new Dictionary<string, string> { { "nhsNumber", _validNhsNumber } });
        _mockHttpClientFunction
            .Setup(x => x.SendPdsGet($"{_mockConfig.Object.Value.RetrievePdsParticipantURL}/{_validNhsNumber}"))
            .Throws(new Exception("There was an error"));

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        _mockHttpClientFunction.Verify(x => x.SendPdsGet($"{_mockConfig.Object.Value.RetrievePdsParticipantURL}/{_validNhsNumber}"), Times.Once());
        _mockFhirPatientDemographicMapper.Verify(x => x.ParseFhirJson(It.IsAny<string>()), Times.Never());
        _loggerMock.Verify(x =>
            x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == "There has been an error retrieving PDS participant data."),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ValidNhsNumberExistsInPdsButNotInDatabase_AddsToDatabaseAndReturnsOK()
    {
        // Arrange
        const string expectedResponseContent = "{\"NhsNumber\":\"3112728165\",\"Gender\":2,\"DeathStatus\":2,\"IsInterpreterRequired\":\"True\"}";

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

        _mockHttpClientFunction.Setup(x => x.SendPdsGet($"{_mockConfig.Object.Value.RetrievePdsParticipantURL}/{_validNhsNumber}"))
            .ReturnsAsync(httpResponse);

        _mockHttpClientFunction.Setup(x => x.GetResponseText(httpResponse))
            .ReturnsAsync(expectedResponseContent);

        _mockFhirPatientDemographicMapper.Setup(x => x.ParseFhirJson(expectedResponseContent))
            .Returns(expectedDemographic);

        // Mock participant not found in database
        _mockParticipantDemographicClient.Setup(x =>
                x.GetSingleByFilter(It.Is<Expression<Func<ParticipantDemographic, bool>>>(f =>
                    f.Compile()(new ParticipantDemographic { NhsNumber = _validNhsNumberLong }))))
            .ReturnsAsync((ParticipantDemographic)null);

        _mockParticipantDemographicClient
            .Setup(x => x.Add(It.Is<ParticipantDemographic>(p => p.NhsNumber == _validNhsNumberLong)))
            .ReturnsAsync(true);

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        _mockHttpClientFunction.Verify(x =>
            x.SendPdsGet($"{_mockConfig.Object.Value.RetrievePdsParticipantURL}/{_validNhsNumber}"),
            Times.Once());
        _mockHttpClientFunction.Verify(x => x.GetResponseText(httpResponse), Times.Once());
        _mockFhirPatientDemographicMapper.Verify(x => x.ParseFhirJson(expectedResponseContent), Times.Once());
        _mockParticipantDemographicClient.Verify(x =>
            x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantDemographic, bool>>>()), Times.Once());
        _mockParticipantDemographicClient.Verify(x =>
            x.Add(It.Is<ParticipantDemographic>(p => p.NhsNumber == _validNhsNumberLong)), Times.Once());
        _mockParticipantDemographicClient.Verify(x => x.Update(It.IsAny<ParticipantDemographic>()), Times.Never());

        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ValidNhsNumberExistsInPdsDatabaseUpdateFails_ReturnsInternalServerError()
    {
        // Arrange
        const string expectedResponseContent = "{\"NhsNumber\":\"3112728165\",\"Gender\":2,\"DeathStatus\":2,\"IsInterpreterRequired\":\"True\"}";

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

        _mockHttpClientFunction.Setup(x => x.SendPdsGet($"{_mockConfig.Object.Value.RetrievePdsParticipantURL}/{_validNhsNumber}"))
            .ReturnsAsync(httpResponse);

        _mockHttpClientFunction.Setup(x => x.GetResponseText(httpResponse))
            .ReturnsAsync(expectedResponseContent);

        _mockFhirPatientDemographicMapper.Setup(x => x.ParseFhirJson(expectedResponseContent))
            .Returns(expectedDemographic);

        // Mock participant found but update fails
        _mockParticipantDemographicClient.Setup(x =>
                x.GetSingleByFilter(It.Is<Expression<Func<ParticipantDemographic, bool>>>(f =>
                    f.Compile()(new ParticipantDemographic { NhsNumber = _validNhsNumberLong }))))
            .ReturnsAsync(new ParticipantDemographic { NhsNumber = _validNhsNumberLong });

        _mockParticipantDemographicClient.Setup(x =>
                x.Update(It.Is<ParticipantDemographic>(p => p.NhsNumber == _validNhsNumberLong)))
            .ReturnsAsync(false); // Update fails

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        _mockHttpClientFunction.Verify(x =>
            x.SendPdsGet($"{_mockConfig.Object.Value.RetrievePdsParticipantURL}/{_validNhsNumber}"),
            Times.Once());
        _mockHttpClientFunction.Verify(x => x.GetResponseText(httpResponse), Times.Once());
        _mockFhirPatientDemographicMapper.Verify(x => x.ParseFhirJson(expectedResponseContent), Times.Once());
        _mockParticipantDemographicClient.Verify(x =>
            x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantDemographic, bool>>>()),
            Times.Once());
        _mockParticipantDemographicClient.Verify(x =>
            x.Update(It.Is<ParticipantDemographic>(p => p.NhsNumber == _validNhsNumberLong)),
            Times.Once());

        _loggerMock.Verify(x =>
            x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == "Failed to update Participant Demographic."),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verify response
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ValidNhsNumberExistsInPdsDatabaseAddFails_ReturnsInternalServerError()
    {
        // Arrange
        const string expectedResponseContent = "{\"NhsNumber\":\"3112728165\",\"Gender\":2,\"DeathStatus\":2,\"IsInterpreterRequired\":\"True\"}";

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

        _mockHttpClientFunction.Setup(x => x.SendPdsGet($"{_mockConfig.Object.Value.RetrievePdsParticipantURL}/{_validNhsNumber}"))
            .ReturnsAsync(httpResponse);

        _mockHttpClientFunction.Setup(x => x.GetResponseText(httpResponse))
            .ReturnsAsync(expectedResponseContent);

        _mockFhirPatientDemographicMapper.Setup(x => x.ParseFhirJson(expectedResponseContent))
            .Returns(expectedDemographic);

        // Mock participant not found in database and add fails
        _mockParticipantDemographicClient.Setup(x =>
                x.GetSingleByFilter(It.Is<Expression<Func<ParticipantDemographic, bool>>>(f =>
                    f.Compile()(new ParticipantDemographic { NhsNumber = _validNhsNumberLong }))))
            .ReturnsAsync((ParticipantDemographic)null);

        _mockParticipantDemographicClient.Setup(x =>
                x.Add(It.Is<ParticipantDemographic>(p => p.NhsNumber == _validNhsNumberLong)))
            .ReturnsAsync(false);

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        _mockHttpClientFunction.Verify(x =>
            x.SendPdsGet($"{_mockConfig.Object.Value.RetrievePdsParticipantURL}/{_validNhsNumber}"),
            Times.Once());
        _mockHttpClientFunction.Verify(x => x.GetResponseText(httpResponse), Times.Once());
        _mockFhirPatientDemographicMapper.Verify(x => x.ParseFhirJson(expectedResponseContent), Times.Once());
        _mockParticipantDemographicClient.Verify(x =>
            x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantDemographic, bool>>>()),
            Times.Once());
        _mockParticipantDemographicClient.Verify(x =>
            x.Add(It.Is<ParticipantDemographic>(p => p.NhsNumber == _validNhsNumberLong)),
            Times.Once());

        _loggerMock.Verify(x =>
            x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == "Failed to add Participant Demographic."),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verify response
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ValidNhsNumberExistsInPdsAndDatabaseErrorOccurs_ReturnsInternalServerError()
    {
        // Arrange
        const string expectedResponseContent = "{\"NhsNumber\":\"3112728165\",\"Gender\":2,\"DeathStatus\":2,\"IsInterpreterRequired\":\"True\"}";

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

        _mockHttpClientFunction.Setup(x => x.SendPdsGet($"{_mockConfig.Object.Value.RetrievePdsParticipantURL}/{_validNhsNumber}"))
            .ReturnsAsync(httpResponse);

        _mockHttpClientFunction.Setup(x => x.GetResponseText(httpResponse))
            .ReturnsAsync(expectedResponseContent);

        _mockFhirPatientDemographicMapper.Setup(x => x.ParseFhirJson(expectedResponseContent))
            .Returns(expectedDemographic);

        _mockParticipantDemographicClient.Setup(x =>
                x.GetSingleByFilter(It.Is<Expression<Func<ParticipantDemographic, bool>>>(f =>
                    f.Compile()(new ParticipantDemographic { NhsNumber = _validNhsNumberLong }))))
            .Throws(new Exception("Unexpected database error"));

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        _mockHttpClientFunction.Verify(x =>
            x.SendPdsGet($"{_mockConfig.Object.Value.RetrievePdsParticipantURL}/{_validNhsNumber}"),
            Times.Once());
        _mockHttpClientFunction.Verify(x => x.GetResponseText(httpResponse), Times.Once());
        _mockFhirPatientDemographicMapper.Verify(x => x.ParseFhirJson(expectedResponseContent), Times.Once());
        _mockParticipantDemographicClient.Verify(x =>
            x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantDemographic, bool>>>()),
            Times.Once());

        _mockParticipantDemographicClient.VerifyNoOtherCalls();

        // Verify response
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }
}
