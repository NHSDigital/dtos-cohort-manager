namespace NHS.CohortManager.Tests.UnitTests.RetrieveParticipantDataTests;

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;
using Common;
using Microsoft.Extensions.Logging;
using NHS.CohortManager.CohortDistributionService;
using NHS.CohortManager.CohortDistribution;
using Model;
using Data.Database;
using NHS.CohortManager.Tests.TestUtils;
using DataServices.Client;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

[TestClass]
public class RetrieveParticipantDataTests
{
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<ILogger<RetrieveParticipantData>> _logger = new();
    private readonly RetrieveParticipantData _sut;
    private readonly Mock<FunctionContext> _context = new();
    private Mock<HttpRequestData> _request;
    private readonly SetupRequest _setupRequest = new();
    private readonly RetrieveParticipantRequestBody _requestBody;
    private readonly CreateParticipant _createParticipant = new();
    private readonly Mock<IExceptionHandler> _exceptionHandler = new();
    private Mock<IDataServiceClient<ParticipantManagement>> _participantManagementClientMock = new();
    private readonly Mock<ICallFunction> _callFunction = new();

    public RetrieveParticipantDataTests()
    {
        _request = new Mock<HttpRequestData>(_context.Object);
        _requestBody = new RetrieveParticipantRequestBody()
        {
            NhsNumber = "1234567890",
            ScreeningService = "BSS"
        };
        _request = _setupRequest.Setup(JsonSerializer.Serialize(_requestBody));

        _sut = new RetrieveParticipantData(_createResponse.Object, _logger.Object, _participantManagementClientMock.Object,
                                            _createParticipant, _exceptionHandler.Object, _callFunction.Object);

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

        _participantManagementClientMock
            .Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
            .Throws(new Exception("there has been an error"));

        // Act
        var result = await _sut.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_GetDemographicDataFails_ReturnInternalServerError()
    {
        // Arrange
        _request = _setupRequest.Setup(JsonSerializer.Serialize(_requestBody));

        _participantManagementClientMock
            .Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
            .ReturnsAsync(new ParticipantManagement());

        _callFunction
            .Setup(x => x.SendGet(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ThrowsAsync(new Exception("there has been an error")).Verifiable();

        // Act
        var result = await _sut.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ValidRequest_ReturnOk()
    {
        // Arrange
        _request = _setupRequest.Setup(JsonSerializer.Serialize(_requestBody));

        var participant = new Participant
        {
            NhsNumber = "1234567890",
        };

        var demographic = new Demographic
        {
            FirstName = "John"
        };

        var expectedResponse = new CohortDistributionParticipant
        {
            NhsNumber = participant.NhsNumber,
            FirstName = demographic.FirstName
        };

        _participantManagementClientMock
            .Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
            .ReturnsAsync(new ParticipantManagement {NHSNumber = 1234567890, ScreeningId = 1});
         _callFunction.Setup(x => x.SendGet(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(JsonSerializer.Serialize(demographic))
            .Verifiable();

        // Act
        var result = await _sut.RunAsync(_request.Object);
        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        System.Console.WriteLine("response body: " + responseBody);
        var response = JsonSerializer.Deserialize<CohortDistributionParticipant>(responseBody);

        // Assert
        _participantManagementClientMock
            .Verify(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()), Times.Once);

        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        Assert.IsNotNull(response);
        Assert.AreEqual(expectedResponse.NhsNumber, response.NhsNumber);
        Assert.AreEqual(expectedResponse.FirstName, response.FirstName);
    }
}
