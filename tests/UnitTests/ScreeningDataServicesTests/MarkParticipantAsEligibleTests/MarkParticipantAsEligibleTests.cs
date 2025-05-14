namespace NHS.CohortManager.Tests.UnitTests.ScreeningDataServicesTests;

using System.Net;
using Common;
using markParticipantAsEligible;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;
using Moq;
using NHS.CohortManager.Tests.TestUtils;
using DataServices.Client;
using System.Text.Json;
using System.Linq.Expressions;

[TestClass]
public class MarkParticipantAsEligibleTests
{
    private readonly Mock<ILogger<MarkParticipantAsEligible>> _mockLogger = new();
    private readonly Mock<ICreateResponse> _mockCreateResponse = new();
    private readonly Mock<IDataServiceClient<ParticipantManagement>> _mockParticipantManagementClient = new();
    private readonly Mock<IExceptionHandler> _handleException = new();
    private readonly Participant _requestBody;
    private readonly MarkParticipantAsEligible _sut;

    public MarkParticipantAsEligibleTests()
    {
        _requestBody = new Participant
        {
            NhsNumber = "1234567890",
            ParticipantId = "123",
            ScreeningId = "1"
        };
        _sut = new MarkParticipantAsEligible(_mockLogger.Object, _mockCreateResponse.Object, _mockParticipantManagementClient.Object, _handleException.Object);
    }

    [TestMethod]
    public async Task Run_MarkParticipantAsEligible_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var mockRequest = SetupRequest(_requestBody);

        var mockParticipantManagement = new ParticipantManagement { NHSNumber = 1234567890, EligibilityFlag = 0, ScreeningId = 1 };
        _mockParticipantManagementClient.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>())).ReturnsAsync(mockParticipantManagement);
        _mockParticipantManagementClient.Setup(x => x.Update(It.IsAny<ParticipantManagement>())).ReturnsAsync(true);

        // Act
        await _sut.Run(mockRequest);

        // Assert
        _mockCreateResponse.Verify(response => response.CreateHttpResponse(HttpStatusCode.OK, It.IsAny<HttpRequestData>(), ""), Times.Once);
        _mockCreateResponse.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_MarkParticipantAsEligible_InvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var mockRequest = SetupRequest(_requestBody);

        var mockParticipantManagement = new ParticipantManagement { NHSNumber = 1234567890, EligibilityFlag = 0, ScreeningId = 1 };
        _mockParticipantManagementClient.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>())).ReturnsAsync(mockParticipantManagement);
        _mockParticipantManagementClient.Setup(x => x.Update(It.IsAny<ParticipantManagement>())).ReturnsAsync(false);

        // Act
        await _sut.Run(mockRequest);

        // Assert
        _mockCreateResponse.Verify(response => response.CreateHttpResponse(HttpStatusCode.BadRequest, It.IsAny<HttpRequestData>(), ""), Times.Once);
        _mockCreateResponse.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_MarkParticipantAsEligible_InvalidNhsNumber_ReturnsBadRequestAndCreatesException()
    {
        // Arrange
        _requestBody.NhsNumber = string.Empty;
        var mockRequest = SetupRequest(_requestBody);

        // Act
        await _sut.Run(mockRequest);

        // Assert
        _mockCreateResponse.Verify(response => response.CreateHttpResponse(HttpStatusCode.BadRequest, It.IsAny<HttpRequestData>(), ""), Times.Once);
        _mockCreateResponse.VerifyNoOtherCalls();
        _handleException.Verify(i => i.CreateSystemExceptionLog(
            It.Is<Exception>((v, t) => v.ToString().Contains("Could not parse NhsNumber")),
            It.IsAny<Participant>(),
            It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task Run_MarkParticipantAsEligible_InvalidScreeningId_ReturnsBadRequestAndCreatesException()
    {
        // Arrange
        _requestBody.ScreeningId = string.Empty;
        var mockRequest = SetupRequest(_requestBody);

        // Act
        await _sut.Run(mockRequest);

        // Assert
        _mockCreateResponse.Verify(response => response.CreateHttpResponse(HttpStatusCode.BadRequest, It.IsAny<HttpRequestData>(), ""), Times.Once);
        _mockCreateResponse.VerifyNoOtherCalls();
        _handleException.Verify(i => i.CreateSystemExceptionLog(
            It.Is<Exception>((v, t) => v.ToString().Contains("Could not parse ScreeningId")),
            It.IsAny<Participant>(),
            It.IsAny<string>()), Times.Once);
    }

    private static HttpRequestData SetupRequest(Participant participant)
    {
        var json = JsonSerializer.Serialize(participant);
        return MockHelpers.CreateMockHttpRequestData(json);
    }
}
