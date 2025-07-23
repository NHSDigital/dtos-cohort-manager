namespace NHS.CohortManager.Tests.UnitTests.DeleteParticipantTests;

using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;
using Common;
using Microsoft.Extensions.Logging;
using NHS.CohortManager.ParticipantManagementService;
using Model;
using NHS.CohortManager.Tests.TestUtils;
using DataServices.Client;
using System.Linq.Expressions;
using Microsoft.Extensions.Options;
using NHS.Screening.DeleteParticipant;

[TestClass]
public class DeleteParticipantTests
{
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<ILogger<DeleteParticipant>> _logger = new();
    private readonly DeleteParticipant _sut;
    private readonly Mock<FunctionContext> _context = new();
    private Mock<HttpRequestData> _request;
    private readonly SetupRequest _setupRequest = new();
    private readonly DeleteParticipantRequestBody _requestBody;
    private readonly Mock<IExceptionHandler> _exceptionHandler = new();
    private Mock<IDataServiceClient<CohortDistribution>> _cohortDistributionClientMock = new();

    public DeleteParticipantTests()
    {
        _request = new Mock<HttpRequestData>(_context.Object);
        _requestBody = new DeleteParticipantRequestBody()
        {
            NhsNumber = "1234567890",
            FamilyName = "Smith",
            DateOfBirth = new DateTime(1990, 1, 1)
        };

        _request = _setupRequest.Setup(JsonSerializer.Serialize(_requestBody));

        _sut = new DeleteParticipant(_createResponse.Object, _logger.Object, _cohortDistributionClientMock.Object,
                                            _exceptionHandler.Object);

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string ResponseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                response.WriteString(ResponseBody);
                return response;
            });
    }

    [TestMethod]
    public async Task Run_RequestBodyInvalid_ReturnBadRequest()
    {
        // Arrange
        _request = _setupRequest.Setup(JsonSerializer.Serialize("Invalid"));

        // Act
        var result = await _sut.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_GetParticipantFails_ReturnInternalServerError()
    {
        // Arrange
        _request = _setupRequest.Setup(JsonSerializer.Serialize(_requestBody));

        _cohortDistributionClientMock
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()))
            .Throws(new Exception("there has been an error"));

        // Act
        var result = await _sut.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_NoParticipantsFound_ReturnNotFound()
    {
        // Arrange
        _request = _setupRequest.Setup(JsonSerializer.Serialize(_requestBody));

        _cohortDistributionClientMock
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()))
            .ReturnsAsync(new List<CohortDistribution>());

        // Act
        var result = await _sut.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
        _logger.Verify(
        x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No participants found with the specified parameters")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        Times.Once);
    }

    [TestMethod]
    public async Task Run_ValidRequest_DeletesMultipleParticipants_ReturnOk()
    {
        // Arrange
        _request = _setupRequest.Setup(JsonSerializer.Serialize(_requestBody));

        var participantData = new List<CohortDistribution>
        {
            new CohortDistribution { CohortDistributionId = 1, FamilyName = _requestBody.FamilyName, NHSNumber = long.Parse(_requestBody.NhsNumber), DateOfBirth = _requestBody.DateOfBirth.Value },
            new CohortDistribution { CohortDistributionId = 2, FamilyName = _requestBody.FamilyName, NHSNumber = long.Parse(_requestBody.NhsNumber), DateOfBirth = _requestBody.DateOfBirth.Value },
            new CohortDistribution { CohortDistributionId = 3, FamilyName = _requestBody.FamilyName, NHSNumber = long.Parse(_requestBody.NhsNumber), DateOfBirth = _requestBody.DateOfBirth.Value }
        };

        _cohortDistributionClientMock
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()))
            .ReturnsAsync(participantData);

        _cohortDistributionClientMock
            .Setup(x => x.Delete(It.IsAny<string>()))
            .Verifiable();

        // Act
        var result = await _sut.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _cohortDistributionClientMock.Verify(x => x.Delete(It.IsAny<string>()), Times.Exactly(participantData.Count));
    }

    [TestMethod]
    public async Task Run_DeleteFailsForAllParticipants_LogsErrorAndReturnsInternalServerError()
    {
        // Arrange
        _request = _setupRequest.Setup(JsonSerializer.Serialize(_requestBody));

        var participantData = new List<CohortDistribution>
        {
            new CohortDistribution { CohortDistributionId = 1, FamilyName = _requestBody.FamilyName, NHSNumber = long.Parse(_requestBody.NhsNumber), DateOfBirth = _requestBody.DateOfBirth.Value },
            new CohortDistribution { CohortDistributionId = 2, FamilyName = _requestBody.FamilyName, NHSNumber = long.Parse(_requestBody.NhsNumber), DateOfBirth = _requestBody.DateOfBirth.Value },
            new CohortDistribution { CohortDistributionId = 3, FamilyName = _requestBody.FamilyName, NHSNumber = long.Parse(_requestBody.NhsNumber), DateOfBirth = _requestBody.DateOfBirth.Value }
        };

        _cohortDistributionClientMock
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()))
            .ReturnsAsync(participantData);

        _cohortDistributionClientMock
            .Setup(x => x.Delete(It.IsAny<string>()))
            .Throws(new Exception("Delete participant function failed."));

        // Act
        var result = await _sut.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);

        _logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Delete participant function failed.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _cohortDistributionClientMock.Verify(x => x.Delete(It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task Preview_InvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        _request = _setupRequest.Setup(JsonSerializer.Serialize("invalid"));

        // Act
        var result = await _sut.PreviewAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Preview_GetByFilterThrows_ReturnsInternalServerError()
    {
        // Arrange
        _request = _setupRequest.Setup(JsonSerializer.Serialize(_requestBody));

        _cohortDistributionClientMock
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()))
            .ThrowsAsync(new Exception("the function failed"));

        // Act
        var result = await _sut.PreviewAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Preview_NoMatchingParticipants_ReturnsNotFound()
    {
        // Arrange
        _request = _setupRequest.Setup(JsonSerializer.Serialize(_requestBody));

        var returnedParticipants = new List<CohortDistribution>
        {
            new CohortDistribution
            {
                NHSNumber = long.Parse(_requestBody.NhsNumber ?? "0"),
                FamilyName = _requestBody.FamilyName,
                DateOfBirth = _requestBody.DateOfBirth.Value.AddDays(-1)
            }
        };

        _cohortDistributionClientMock
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()))
            .ReturnsAsync(returnedParticipants);

        // Act
        var result = await _sut.PreviewAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
    }

    [TestMethod]
    public async Task Preview_MatchingParticipantsFound_ReturnsOkWithData()
    {
        // Arrange
        _request = _setupRequest.Setup(JsonSerializer.Serialize(_requestBody));

        var participantData = new List<CohortDistribution>
        {
            new CohortDistribution
            {
                CohortDistributionId = 1,
                FamilyName = _requestBody.FamilyName,
                NHSNumber = long.Parse(_requestBody.NhsNumber),
                DateOfBirth = _requestBody.DateOfBirth.Value
            },
            new CohortDistribution
            {
                CohortDistributionId = 2,
                FamilyName = _requestBody.FamilyName,
                NHSNumber = long.Parse(_requestBody.NhsNumber),
                DateOfBirth = _requestBody.DateOfBirth.Value
            }
        };

        _cohortDistributionClientMock
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()))
            .ReturnsAsync(participantData);

        string responseContent = string.Empty;

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string body) =>
            {
                responseContent = body;
                var response = req.CreateResponse(statusCode);
                response.WriteString(body);
                return response;
            });

        // Act
        var result = await _sut.PreviewAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        Assert.IsFalse(string.IsNullOrWhiteSpace(responseContent));

        var returnedParticipants = JsonSerializer.Deserialize<List<CohortDistribution>>(responseContent);
        Assert.IsNotNull(returnedParticipants);
        Assert.AreEqual(2, returnedParticipants.Count);
        Assert.IsTrue(returnedParticipants.All(p => p.FamilyName == _requestBody.FamilyName));
    }
}
