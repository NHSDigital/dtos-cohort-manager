namespace NHS.CohortManager.Tests.UnitTests.ParticipantManagementServiceTests;

using System.Net;
using System.Text.Json;
using Common;
using DataServices.Client;
using Microsoft.Extensions.Options;
using Model;
using Moq;
using NHS.CohortManager.ParticipantManagementService;
using NHS.CohortManager.Tests.TestUtils;
using NHS.Screening.RemoveParticipant;

[TestClass]
public class RemoveParticipantTests : DatabaseTestBaseSetup<RemoveParticipant>
{
    private static readonly Mock<IExceptionHandler> _handleException = new();
    private static readonly Mock<ICohortDistributionHandler> _cohortDistributionHandler = new();
    private static readonly Mock<IDataServiceClient<ParticipantManagement>> _participantManagementClient = new();
    private static readonly BasicParticipantCsvRecord _participantCsvRecord = new();
    private static readonly Mock<IOptions<RemoveParticipantConfig>> _config = new();

    public RemoveParticipantTests() : base((conn, logger, transaction, command, response) =>
    new RemoveParticipant(
        logger,
        response,
        _handleException.Object,
        _cohortDistributionHandler.Object,
        _participantManagementClient.Object,
        _config.Object))
    {
        Environment.SetEnvironmentVariable("markParticipantAsIneligible", "markParticipantAsIneligible");
        CreateHttpResponseMock();
    }

    [TestInitialize]
    public void TestInitialize()
    {
        var testConfig = new RemoveParticipantConfig
        {
            UpdateParticipant = "UpdateParticipant",
            ExceptionFunctionURL = "ExceptionUrl",
            ParticipantManagementUrl = "ParticipantManagementUrl"
        };

        _config.Setup(c => c.Value).Returns(testConfig);

        _cohortDistributionHandler.Reset();
        _service = new RemoveParticipant(
            _loggerMock.Object,
            _createResponseMock.Object,
            _handleException.Object,
            _cohortDistributionHandler.Object,
            _participantManagementClient.Object,
            _config.Object);
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
    public async Task Run_SendToCohortDistributionServiceReturnsFalse_ReturnsInternalServerError()
    {
        // Arrange
        SetupValidRequest();
        _cohortDistributionHandler.Setup(x => x.SendToCohortDistributionService(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Participant>())).Returns(Task.FromResult(false));

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        _cohortDistributionHandler.Verify(x => x.SendToCohortDistributionService(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Participant>()), Times.Never);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    private void SetupValidRequest()
    {
        var participantRecord = JsonSerializer.Serialize(_participantCsvRecord);
        _request = SetupRequest(participantRecord);
    }
}
