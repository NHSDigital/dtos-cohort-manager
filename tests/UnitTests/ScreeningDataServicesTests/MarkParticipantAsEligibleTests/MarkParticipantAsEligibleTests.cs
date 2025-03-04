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

    [TestMethod]
    public async Task Run_MarkParticipantAsEligible_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var requestBody = new Participant
        {
            NhsNumber = "1234567890",
            ParticipantId = "123",
            ScreeningId = "1"
        };
        var json = JsonSerializer.Serialize(requestBody);
        var mockRequest = MockHelpers.CreateMockHttpRequestData(json);
        var markParticipantAsEligible = new MarkParticipantAsEligible(_mockLogger.Object, _mockCreateResponse.Object, _mockParticipantManagementClient.Object, _handleException.Object);

        var mockParticipantManagement = new ParticipantManagement { NHSNumber = 1234567890, EligibilityFlag = 0 , ScreeningId= 1 };
        _mockParticipantManagementClient.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>())).ReturnsAsync(mockParticipantManagement);
        _mockParticipantManagementClient.Setup(x => x.Update(It.IsAny<ParticipantManagement>())).ReturnsAsync(true);

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
         var requestBody = new Participant
        {
            NhsNumber = "1234567890",
            ParticipantId = "123",
            ScreeningId = "1"
        };
        var json = JsonSerializer.Serialize(requestBody);
        var mockRequest = MockHelpers.CreateMockHttpRequestData(json);
        var markParticipantAsEligible = new MarkParticipantAsEligible(_mockLogger.Object, _mockCreateResponse.Object, _mockParticipantManagementClient.Object, _handleException.Object);

        var mockParticipantManagement = new ParticipantManagement { NHSNumber = 1234567890, EligibilityFlag = 0 , ScreeningId =1};
        _mockParticipantManagementClient.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>())).ReturnsAsync(mockParticipantManagement);
        _mockParticipantManagementClient.Setup(x => x.Update(It.IsAny<ParticipantManagement>())).ReturnsAsync(false);

        // Act
        await markParticipantAsEligible.Run(mockRequest);

        // Assert
        _mockCreateResponse.Verify(response => response.CreateHttpResponse(HttpStatusCode.BadRequest, It.IsAny<HttpRequestData>(), ""), Times.Once);
        _mockCreateResponse.VerifyNoOtherCalls();
    }
}
