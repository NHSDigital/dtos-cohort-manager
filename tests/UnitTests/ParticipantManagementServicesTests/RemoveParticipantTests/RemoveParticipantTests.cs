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

[TestClass]
public class RemoveParticipantTests : DatabaseTestBaseSetup<RemoveParticipant>
{
    private static readonly Mock<IDataServiceClient<ParticipantManagement>> _participantManagementClient = new();
    private static readonly Mock<IHttpClientFunction> _httpClientFunction = new();
    private static readonly Mock<IExceptionHandler> _handleException = new();
    private static readonly Mock<ICohortDistributionHandler> _cohortDistributionHandler = new();
    private static readonly BasicParticipantCsvRecord _participantCsvRecord = new();

    public RemoveParticipantTests() : base((conn, logger, transaction, command, response) =>
    new RemoveParticipant(
        logger,
        response,
        _handleException.Object,
        _cohortDistributionHandler.Object,
        _participantManagementClient.Object))
    {
        CreateHttpResponseMock();
    }

    [TestInitialize]
    public void TestInitialize()
    {
        _httpClientFunction.Reset();
        _cohortDistributionHandler.Reset();
        _service = new RemoveParticipant(
            _loggerMock.Object,
            _createResponseMock.Object,
            _handleException.Object,
            _cohortDistributionHandler.Object,
            _participantManagementClient.Object);
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

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        _cohortDistributionHandler.Verify(x => x.SendToCohortDistributionService(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Participant>()), Times.Never);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_MarkParticipantAsIneligibleErrors_ReturnsInternalServerError()
    {
        // Arrange
        SetupValidRequest();
        _httpClientFunction.Setup(x => x.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>())).Throws(new Exception("There was an error"));

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
