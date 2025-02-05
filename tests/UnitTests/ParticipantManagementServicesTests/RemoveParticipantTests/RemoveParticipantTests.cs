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
    private static readonly Mock<ICheckDemographic> _checkDemographic = new();
    private static readonly Mock<ICreateParticipant> _createParticipant = new();
    private static readonly Mock<IExceptionHandler> _handleException = new();
    private static readonly Mock<ICohortDistributionHandler> _cohortDistributionHandler = new();
    private static BasicParticipantCsvRecord _participantCsvRecord = new();

    public RemoveParticipantTests() : base((conn, logger, transaction, command, response) =>
    new RemoveParticipant(
        logger,
        response,
        _callFunction.Object,
        _checkDemographic.Object,
        _createParticipant.Object,
        _handleException.Object,
        _cohortDistributionHandler.Object))
    {
        Environment.SetEnvironmentVariable("DemographicURIGet", "DemographicURIGet");
        Environment.SetEnvironmentVariable("markParticipantAsIneligible", "markParticipantAsIneligible");
        CreateHttpResponseMock();
    }

    [TestInitialize]
    public void TestInitialize()
    {
        _callFunction.Reset();
        _service = new RemoveParticipant(
            _loggerMock.Object,
            _createResponseMock.Object,
            _callFunction.Object,
            _checkDemographic.Object,
            _createParticipant.Object,
            _handleException.Object,
            _cohortDistributionHandler.Object);
        _participantCsvRecord.FileName = "TestFile";
        _participantCsvRecord.Participant = new BasicParticipantData() { NhsNumber = "1234567890" };
        _participantCsvRecord.participant = new Participant() { NhsNumber = "1234567890" };
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
    public async Task Run_GetDemographicAsyncReturnsNull_ReturnsInternalServerError()
    {
        // Arrange
        SetupValidRequest();
        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), It.Is<string>(s => s.Contains("DemographicURIGet")))).Returns(Task.FromResult<Demographic>(null));

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        _checkDemographic.Verify(x => x.GetDemographicAsync(It.IsAny<string>(), It.Is<string>(s => s.Contains("DemographicURIGet"))), Times.Once);
        _callFunction.Verify(x => x.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>()), Times.Never);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    private void SetupValidRequest()
    {
        var participantRecord = JsonSerializer.Serialize(_participantCsvRecord);
        _request = SetupRequest(participantRecord);
    }
}
