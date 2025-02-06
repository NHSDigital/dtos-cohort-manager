namespace NHS.CohortManager.Tests.UnitTests.ParticipantManagementServiceTests;

using System.Net;
using System.Text.Json;
using Common;
using Model;
using Moq;
using NHS.CohortManager.ParticipantManagementService;
using NHS.CohortManager.Tests.TestUtils;

[TestClass]
public class RemoveParticipantTests : DatabaseTestBaseSetup<RemoveParticipant>
{
    private static readonly Mock<ICallFunction> _callFunction = new();
    private static readonly Mock<IExceptionHandler> _handleException = new();
    private static readonly Mock<ICohortDistributionHandler> _cohortDistributionHandler = new();
    private static readonly BasicParticipantCsvRecord _participantCsvRecord = new();

    public RemoveParticipantTests() : base((conn, logger, transaction, command, response) =>
    new RemoveParticipant(
        logger,
        response,
        _callFunction.Object,
        _handleException.Object,
        _cohortDistributionHandler.Object))
    {
        Environment.SetEnvironmentVariable("markParticipantAsIneligible", "markParticipantAsIneligible");
        CreateHttpResponseMock();
    }

    [TestInitialize]
    public void TestInitialize()
    {
        _callFunction.Reset();
        _cohortDistributionHandler.Reset();
        _service = new RemoveParticipant(
            _loggerMock.Object,
            _createResponseMock.Object,
            _callFunction.Object,
            _handleException.Object,
            _cohortDistributionHandler.Object);
        _participantCsvRecord.FileName = "TestFile";
        _participantCsvRecord.participant = new Participant() { NhsNumber = "1234567890", ScreeningId = "1", RecordType = Actions.Removed };
    }

    [DataRow("")]
    [DataRow("Invalid request body")]
    [TestMethod]
    public async Task Run_BadRequest_ReturnsBadRequest(string badRequest)
    {
        // Arrange
        _request = SetupRequest(badRequest);

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_MarkParticipantAsIneligibleReturnsNonOkStatus_ReturnsInternalServerError()
    {
        // Arrange
        SetupValidRequest();
        var response = SetupResponse(HttpStatusCode.BadRequest);
        _callFunction.Setup(x => x.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>())).ReturnsAsync(response.Object);

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        _callFunction.Verify(x => x.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>()), Times.Once);
        _cohortDistributionHandler.Verify(x => x.SendToCohortDistributionService(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Participant>()), Times.Never);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_MarkParticipantAsIneligibleErrors_ReturnsInternalServerError()
    {
        // Arrange
        SetupValidRequest();
        _callFunction.Setup(x => x.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>())).Throws(new Exception("There was an error"));

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        _callFunction.Verify(x => x.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>()), Times.Once);
        _cohortDistributionHandler.Verify(x => x.SendToCohortDistributionService(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Participant>()), Times.Never);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_SendToCohortDistributionServiceReturnsFalse_ReturnsInternalServerError()
    {
        // Arrange
        SetupValidRequest();
        var response = SetupResponse(HttpStatusCode.OK);
        _callFunction.Setup(x => x.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>())).ReturnsAsync(response.Object);
        _cohortDistributionHandler.Setup(x => x.SendToCohortDistributionService(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Participant>())).Returns(Task.FromResult(false));

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        _callFunction.Verify(x => x.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>()), Times.Once);
        _cohortDistributionHandler.Verify(x => x.SendToCohortDistributionService(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Participant>()), Times.Once);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_MarkParticipantAsIneligibleAndSendToCohortDistributionServiceBothSucceed_ReturnsOk()
    {
        // Arrange
        SetupValidRequest();
        var response = SetupResponse(HttpStatusCode.OK);
        _callFunction.Setup(x => x.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>())).ReturnsAsync(response.Object);
        _cohortDistributionHandler.Setup(x => x.SendToCohortDistributionService(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Participant>())).Returns(Task.FromResult(true));

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        _callFunction.Verify(x => x.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>()), Times.Once);
        _cohortDistributionHandler.Verify(x => x.SendToCohortDistributionService(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Participant>()), Times.Once);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    private void SetupValidRequest()
    {
        var participantRecord = JsonSerializer.Serialize(_participantCsvRecord);
        _request = SetupRequest(participantRecord);
    }

    private static Mock<HttpWebResponse> SetupResponse(HttpStatusCode statusCode)
    {
        var response = new Mock<HttpWebResponse>();
        response.Setup(r => r.StatusCode).Returns(statusCode);
        return response;
    }
}
