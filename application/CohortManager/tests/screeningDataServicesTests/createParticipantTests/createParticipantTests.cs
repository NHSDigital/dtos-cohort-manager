namespace NHS.CohortManager.Tests.ScreeningDataServicesTests;

using System.Net;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

using Moq;

using Common;
using Data.Database;
using Model;
using NHS.CohortManager.ScreeningDataServices;
using NHS.CohortManager.Tests.TestUtils;

[TestClass]
public class CreateParticipantTests
{

    private readonly Mock<ILogger<CreateParticipant>> _mockLogger;
    private readonly Mock<ICreateResponse> _mockCreateResponse;
    private readonly Mock<ICreateParticipantData> _mockCreateParticipantData;

    readonly Mock<FunctionContext> mockContext;
    public CreateParticipantTests()
    {
        _mockLogger = new Mock<ILogger<CreateParticipant>>();
        _mockCreateResponse = new Mock<ICreateResponse>();
        _mockCreateParticipantData = new Mock<ICreateParticipantData>();
        mockContext = new Mock<FunctionContext>();
    }

    [TestMethod]
    public async Task Run_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        string requestBody = @"{
            ""nhsnumber"": ""1234567890"",
            ""supersededByNhsNumber"": ""0987654321""
            }";
        var mockRequest = MockHelpers.CreateMockHttpRequestData(requestBody);

        var sut = new CreateParticipant(_mockLogger.Object, _mockCreateResponse.Object, _mockCreateParticipantData.Object);
        _mockCreateParticipantData.Setup(data => data.CreateParticipantEntryAsync(It.IsAny<Participant>(), It.IsAny<string>())).Returns(true);

        // Act
        var response = await sut.Run(mockRequest);

        // Assert
        _mockCreateResponse.Verify(response => response.CreateHttpResponse(HttpStatusCode.OK, It.IsAny<HttpRequestData>()), Times.Once);
        _mockCreateResponse.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_InvalidRequest_Returns404()
    {
        // Arrange
        var mockRequest = new Mock<HttpRequestData>(mockContext.Object);
        var sut = new CreateParticipant(_mockLogger.Object, _mockCreateResponse.Object, _mockCreateParticipantData.Object);
        _mockCreateParticipantData.Setup(data => data.CreateParticipantEntryAsync(It.IsAny<Participant>(), It.IsAny<string>())).Returns(false);

        // Act
        var response = await sut.Run(mockRequest.Object);

        // Assert
        _mockCreateResponse.Verify(response => response.CreateHttpResponse(HttpStatusCode.InternalServerError, It.IsAny<HttpRequestData>()), Times.Once);
        _mockCreateResponse.VerifyNoOtherCalls();
    }
}
