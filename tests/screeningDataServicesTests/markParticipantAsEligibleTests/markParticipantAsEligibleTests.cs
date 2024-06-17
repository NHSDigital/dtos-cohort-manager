namespace NHS.CohortManager.Tests.ScreeningDataServicesTests;

using System.Net;
using Common;
using Data.Database;
using markParticipantAsEligible;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;
using Moq;
using screeningDataServices;
using NHS.CohortManager.Tests.TestUtils;

[TestClass]
public class MarkParticipantAsEligibleTests
{
    private readonly Mock<ILogger<MarkParticipantAsEligible>> mockLogger = new();
    private readonly Mock<ICreateResponse> mockCreateResponse = new();
    private readonly Mock<IUpdateParticipantData> mockUpdateParticipantData = new();

    [TestMethod]
    public async Task Run_MarkParticipantAsEligible_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        string requestBody = @"{
            ""isActive"": ""Y""
        }";
        var mockRequest = MockHelpers.CreateMockHttpRequestData(requestBody);
        var markParticipantAsEligible = new MarkParticipantAsEligible(mockLogger.Object, mockCreateResponse.Object, mockUpdateParticipantData.Object);
        mockUpdateParticipantData.Setup(x => x.UpdateParticipantAsEligible(It.IsAny<Participant>(), It.IsAny<char>())).Returns(true);

        // Act
        await markParticipantAsEligible.Run(mockRequest);

        // Assert
        mockCreateResponse.Verify(response => response.CreateHttpResponse(HttpStatusCode.OK, It.IsAny<HttpRequestData>(), ""), Times.Once);
        mockCreateResponse.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_MarkParticipantAsEligible_InvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        string requestBody = @"{
            ""isActive"": ""Y""
        }";
        var mockRequest = MockHelpers.CreateMockHttpRequestData(requestBody);
        var markParticipantAsEligible = new MarkParticipantAsEligible(mockLogger.Object, mockCreateResponse.Object, mockUpdateParticipantData.Object);
        mockUpdateParticipantData.Setup(x => x.UpdateParticipantAsEligible(It.IsAny<Participant>(), It.IsAny<char>())).Returns(false);

        // Act
        await markParticipantAsEligible.Run(mockRequest);

        // Assert
        mockCreateResponse.Verify(response => response.CreateHttpResponse(HttpStatusCode.BadRequest, It.IsAny<HttpRequestData>(), ""), Times.Once);
        mockCreateResponse.VerifyNoOtherCalls();
    }
}
