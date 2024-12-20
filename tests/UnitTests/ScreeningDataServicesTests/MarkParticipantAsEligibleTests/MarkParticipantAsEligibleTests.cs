namespace NHS.CohortManager.Tests.UnitTests.ScreeningDataServicesTests;

using System.Net;
using Common;
using Data.Database;
using markParticipantAsEligible;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;
using Moq;
using NHS.CohortManager.Tests.TestUtils;

[TestClass]
public class MarkParticipantAsEligibleTests
{
    private readonly Mock<ILogger<MarkParticipantAsEligible>> _mockLogger = new();
    private readonly Mock<ICreateResponse> _mockCreateResponse = new();
    private readonly Mock<IParticipantManagerData> _mockUpdateParticipantData = new();
    private readonly Mock<IExceptionHandler> _handleException = new();

    [TestMethod]
    public async Task Run_MarkParticipantAsEligible_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        string requestBody = @"{
            ""isActive"": 1
        }";
        var mockRequest = MockHelpers.CreateMockHttpRequestData(requestBody);
        var markParticipantAsEligible = new MarkParticipantAsEligible(_mockLogger.Object, _mockCreateResponse.Object, _mockUpdateParticipantData.Object, _handleException.Object);
        _mockUpdateParticipantData.Setup(x => x.UpdateParticipantAsEligible(It.IsAny<Participant>())).Returns(true);

        // Act
        await markParticipantAsEligible.Run(mockRequest);

        // Assert
        _mockCreateResponse.Verify(response => response.CreateHttpResponse(HttpStatusCode.OK, It.IsAny<HttpRequestData>(), ""), Times.Once);
        _mockCreateResponse.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_MarkParticipantAsEligible_InvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        string requestBody = @"{
            ""isActive"": 0
        }";
        var mockRequest = MockHelpers.CreateMockHttpRequestData(requestBody);
        var markParticipantAsEligible = new MarkParticipantAsEligible(_mockLogger.Object, _mockCreateResponse.Object, _mockUpdateParticipantData.Object, _handleException.Object);
        _mockUpdateParticipantData.Setup(x => x.UpdateParticipantAsEligible(It.IsAny<Participant>())).Returns(false);

        // Act
        await markParticipantAsEligible.Run(mockRequest);

        // Assert
        _mockCreateResponse.Verify(response => response.CreateHttpResponse(HttpStatusCode.BadRequest, It.IsAny<HttpRequestData>(), ""), Times.Once);
        _mockCreateResponse.VerifyNoOtherCalls();
    }
}
