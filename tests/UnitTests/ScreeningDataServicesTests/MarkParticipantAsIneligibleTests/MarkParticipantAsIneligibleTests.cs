namespace NHS.CohortManager.Tests.UnitTests.ScreeningDataServicesTests;

using System.Linq.Expressions;
using System.Net;
using System.Text.Json;
using Common;
using DataServices.Client;
using Model;
using Moq;
using NHS.CohortManager.ScreeningDataServices;
using NHS.CohortManager.Tests.TestUtils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NHS.Screening.MarkParticipantAsIneligible;

[TestClass]
public class MarkParticipantAsIneligibleTests : DatabaseTestBaseSetup<MarkParticipantAsIneligible>
{
    private static readonly Mock<IDataServiceClient<ParticipantManagement>> _participantManagementClient = new();
    private static readonly Mock<IExceptionHandler> _handleException = new();
    private static readonly Mock<ICallFunction> _callFunction = new();
    private static readonly ParticipantCsvRecord _participantCsvRecord = new();
    private static readonly Mock<IOptions<MarkParticipantAsIneligibleConfig>> _config = new();
    private ParticipantManagement _participantManagement = new();

    public MarkParticipantAsIneligibleTests() : base((conn, logger, transaction, command, response) =>
    new MarkParticipantAsIneligible(
        logger,
        response,
        _participantManagementClient.Object,
        _callFunction.Object,
        _handleException.Object,
        _config.Object))
    {
        TestInitialize();
        CreateHttpResponseMock();
        TestInitialize();
    }

    [TestInitialize]
    public void TestInitialize()
    {
        _callFunction.Reset();
        _participantManagementClient.Reset();
        _service = new MarkParticipantAsIneligible(
            _loggerMock.Object,
            _createResponseMock.Object,
            _participantManagementClient.Object,
            _callFunction.Object,
            _handleException.Object,
            _config.Object);
        _participantCsvRecord.Participant = new Participant()
        {
            NhsNumber = "1234567890",
            ScreeningId = "1"
        };
        _participantManagement = new ParticipantManagement()
        {
            NHSNumber = 1234567890,
            ScreeningId = 1
        };
        var testConfig = new MarkParticipantAsIneligibleConfig
        {
            ParticipantManagementUrl = "test-storage",
            LookupValidationURL = "test-inbound",
        };

        _config.Setup(c => c.Value).Returns(testConfig);
        _participantManagementClient.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
            .ReturnsAsync(new List<ParticipantManagement> { _participantManagement });
        _callFunction.Setup(x => x.GetResponseText(It.IsAny<HttpWebResponse>())).Returns(Task.FromResult(
            JsonSerializer.Serialize(new ValidationExceptionLog()
            {
                IsFatal = false,
                CreatedException = false
            })));
        _participantManagementClient.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
            .ReturnsAsync(_participantManagement);
        _participantManagementClient.Setup(data => data.Update(It.IsAny<ParticipantManagement>())).ReturnsAsync(true);
    }

    [DataRow("")]
    [DataRow("Invalid request body")]
    [TestMethod]
    public async Task Run_BadRequest_ReturnsBadRequest(string badRequest)
    {
        // Arrange
        _request = SetupRequest(badRequest);

        // Act
        var result = await _service.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [DataRow("1234567890", "")]
    [DataRow("", "1")]
    [TestMethod]
    public async Task Run_InvalidNhsNumberOrScreeningId_ReturnsFormatException(string nhsNumber, string screeningId)
    {
        // Arrange
        _participantCsvRecord.Participant.NhsNumber = nhsNumber;
        _participantCsvRecord.Participant.ScreeningId = screeningId;
        _request = SetupRequest(JsonSerializer.Serialize(_participantCsvRecord));

        // Act
        var result = await Assert.ThrowsExceptionAsync<FormatException>(() => _service.RunAsync(_request.Object));

        // Assert
        Assert.IsInstanceOfType(result, typeof(FormatException));
        StringAssert.Contains(result.Message, "Could not parse NhsNumber or screeningID");
    }

    [TestMethod]
    public async Task Run_FailedLookupValidation_ReturnsBadRequest()
    {
        // Arrange
        _callFunction.Setup(x => x.GetResponseText(It.IsAny<HttpWebResponse>())).Returns(Task.FromResult(
            JsonSerializer.Serialize(new ValidationExceptionLog()
            {
                IsFatal = true,
                CreatedException = true
            })));

        _request = SetupRequest(JsonSerializer.Serialize(_participantCsvRecord));

        // Act
        var result = await _service.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_UpdateSucceeds_ReturnsOK()
    {

        _request = SetupRequest(JsonSerializer.Serialize(_participantCsvRecord));

        // Act
        var result = await _service.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_UpdateFails_ReturnsBadRequest()
    {
        // Arrange
        _participantManagementClient.Setup(data => data.Update(It.IsAny<ParticipantManagement>())).ReturnsAsync(false);
        _request = SetupRequest(JsonSerializer.Serialize(_participantCsvRecord));

        // Act
        var result = await _service.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_UpdateThrowsException_ReturnsBadRequest()
    {
        // Arrange
        var errorMessage = "There was an error";
        _participantManagementClient.Setup(data => data.Update(It.IsAny<ParticipantManagement>())).Throws(new Exception(errorMessage));
        _request = SetupRequest(JsonSerializer.Serialize(_participantCsvRecord));

        // Act
        var result = await _service.RunAsync(_request.Object);

        // Assert
        _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(errorMessage)),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }
}
