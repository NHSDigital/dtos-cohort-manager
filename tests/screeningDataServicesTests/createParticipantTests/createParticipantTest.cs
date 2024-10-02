namespace NHS.CohortManager.Tests.ScreeningDataServicesTests;

using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Common;
using Data.Database;
using Model;
using NHS.CohortManager.Tests.TestUtils;
using System.Text.Json;

[TestClass]
public class CreateParticipantTests
{
    private readonly Mock<ILogger<ScreeningDataServices.CreateParticipant>> _mockLogger = new();
    private readonly Mock<ICreateResponse> _mockCreateResponse = new();
    private readonly Mock<ICreateParticipantData> _mockCreateParticipantData = new();
    private readonly Mock<IExceptionHandler> _handleException = new();
    private Mock<IParticipantManagerData> _participantManagerData = new();

    private Mock<ICallFunction> _callFunction = new();

    [TestMethod]
    public async Task Run_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var participantCsvRecord = new ParticipantCsvRecord
        {
            Participant = new Participant
            {
                NhsNumber = "1234567890"
            }
        };
        var json = JsonSerializer.Serialize(participantCsvRecord);
        var mockRequest = MockHelpers.CreateMockHttpRequestData(json);

        var sut = new ScreeningDataServices.CreateParticipant(_mockLogger.Object, _mockCreateResponse.Object, _mockCreateParticipantData.Object, _handleException.Object, _participantManagerData.Object, _callFunction.Object);
        _callFunction.Setup(x => x.GetResponseText(It.IsAny<HttpWebResponse>())).Returns(Task.FromResult<string>(
            JsonSerializer.Serialize<ValidationExceptionLog>(new ValidationExceptionLog()
            {
                IsFatal = false,
                CreatedException = false
            })));
        _mockCreateParticipantData.Setup(data => data.CreateParticipantEntry(It.IsAny<ParticipantCsvRecord>())).ReturnsAsync(true);

        // Act
        await sut.Run(mockRequest);

        // Assert
        _mockCreateResponse.Verify(response => response.CreateHttpResponse(HttpStatusCode.OK, It.IsAny<HttpRequestData>(), ""), Times.Once);
        _mockCreateResponse.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_InvalidRequest_Returns500()
    {
        // Arrange
        var participantCsvRecord = new ParticipantCsvRecord
        {
            Participant = new Participant
            {
                NhsNumber = "1234567890"
            }
        };
        var json = JsonSerializer.Serialize(participantCsvRecord);
        var mockRequest = MockHelpers.CreateMockHttpRequestData(json);

        var sut = new ScreeningDataServices.CreateParticipant(_mockLogger.Object, _mockCreateResponse.Object, _mockCreateParticipantData.Object, _handleException.Object, _participantManagerData.Object, _callFunction.Object);
        _callFunction.Setup(x => x.GetResponseText(It.IsAny<HttpWebResponse>())).Returns(Task.FromResult<string>(
            JsonSerializer.Serialize<ValidationExceptionLog>(new ValidationExceptionLog()
            {
                IsFatal = false,
                CreatedException = false
            })));
        _mockCreateParticipantData.Setup(data => data.CreateParticipantEntry(It.IsAny<ParticipantCsvRecord>())).ReturnsAsync(false);

        // Act
        await sut.Run(mockRequest);

        // Assert
        _mockCreateResponse.Verify(response => response.CreateHttpResponse(HttpStatusCode.InternalServerError, It.IsAny<HttpRequestData>(), ""), Times.Once);
        _mockCreateResponse.VerifyNoOtherCalls();
    }
}
