namespace NHS.CohortManager.Tests.UnitTests.AddCohortDistributionDataTests;

using System.Net;
using System.Text.Json;
using Common;
using DataServices.Client;
using Model;
using Moq;
using NHS.CohortManager.CohortDistributionDataServices;
using NHS.CohortManager.Tests.TestUtils;

[TestClass]
public class AddCohortDistributionDataTests : DatabaseTestBaseSetup<AddCohortDistributionDataFunction>
{
    private static readonly Mock<IExceptionHandler> _handleException = new();
    private static readonly Mock<IDataServiceClient<CohortDistribution>> _cohortDistributionDataService = new();
    private CohortDistributionParticipant _participantCsvRecord = new();

    public AddCohortDistributionDataTests() : base((conn, logger, transaction, command, response) =>
    new AddCohortDistributionDataFunction(
        logger,
        response,
        _handleException.Object,
        _cohortDistributionDataService.Object))
    {
        CreateHttpResponseMock();
    }

    [TestInitialize]
    public void TestInitialize()
    {
        _cohortDistributionDataService.Reset();
        _service = new AddCohortDistributionDataFunction(
            _loggerMock.Object,
            _createResponseMock.Object,
            _handleException.Object,
            _cohortDistributionDataService.Object);
        _participantCsvRecord = new CohortDistributionParticipant() { NhsNumber = "1234567890" };
    }

    [DataRow("")]
    [DataRow("Invalid request body")]
    [TestMethod]
    public async Task Run_InvalidRequest_ReturnsInternalServerError(string badRequest)
    {
        // Arrange
        _request = SetupRequest(badRequest);

        // Act
        var result = await _service.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_InsertCohortDistributionDataSucceeds_ReturnsOk()
    {
        // Arrange
        var participantRecord = JsonSerializer.Serialize(_participantCsvRecord);
        _request = SetupRequest(participantRecord);

        _cohortDistributionDataService.Setup(x => x.Add(It.IsAny<CohortDistribution>())).Returns(Task.FromResult(true));

        // Act
        var result = await _service.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_InsertCohortDistributionDataFails_ReturnsInternalServerError()
    {
        // Arrange
        var participantRecord = JsonSerializer.Serialize(_participantCsvRecord);
        _request = SetupRequest(participantRecord);

        _cohortDistributionDataService.Setup(x => x.Add(It.IsAny<CohortDistribution>())).Returns(Task.FromResult(false));

        // Act
        var result = await _service.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }
}
