namespace NHS.CohortManager.Tests.ParticipantManagementServiceTests;

using Common;
using Microsoft.Extensions.Logging;
using System.Net;
using Moq;
using System.Text.Json;
using Model;
using Microsoft.Extensions.Options;
using NHS.CohortManager.ParticipantManagementServices;
using DataServices.Client;

[TestClass]
public class ManageParticipantTests
{
    private readonly Mock<ILogger<ManageParticipant>> _loggerMock = new();
    private readonly Mock<IExceptionHandler> _handleException = new();
    private readonly Mock<IOptions<ManageParticipantConfig>> _config = new();
    private readonly Mock<IDataServiceClient<ParticipantManagement>> _participantManagementClientMock = new();
    private readonly ManageParticipant _sut;
    private readonly BasicParticipantCsvRecord _request;

    public ManageParticipantTests()
    {
        var testConfig = new ManageParticipantConfig
        {

        };

        _config.Setup(c => c.Value).Returns(testConfig);

        _participantManagementClientMock
            .Setup(data => data.Add(It.IsAny<ParticipantManagement>()))
            .ReturnsAsync(true);

        _participantManagementClientMock
            .Setup(data => data.Update(It.IsAny<ParticipantManagement>()))
            .ReturnsAsync(true);

        _sut = new ManageParticipant(
            _loggerMock.Object,
            _handleException.Object,
            _config.Object
        );

        _request = new BasicParticipantCsvRecord
        {
            FileName = "mockFileName",
            Participant = new BasicParticipantData
            {
                NhsNumber = "1234567890",
                ScreeningName = "mockScreeningName"
            }
        };

    }

    [TestMethod]
    public async Task Run_SuccessfullyAddsParticipant_LogsParticipantCreatedAndMarkedEligible()
    {
        // Act
        await _sut.Run(JsonSerializer.Serialize(_request));

        // Assert
        _loggerMock.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Participant created")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
    }

    [TestMethod]
    public async Task Run_FailsToAddParticipant_LogsErrorAndRaisesException()
    {
        // Arrange
        var errorMessage = "There was problem posting the participant to the database";

        _httpClientFunctionMock.Setup(x => x.SendPost("DSaddParticipant", It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });

        // Act
        await _sut.Run(JsonSerializer.Serialize(_request));

        // Assert
        _loggerMock.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(errorMessage)),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);

        _handleException.Verify(i => i.CreateSystemExceptionLog(
            It.Is<Exception>((v, t) => v.ToString().Contains(errorMessage)),
            It.IsAny<BasicParticipantData>(),
            It.IsAny<string>()), Times.Once);
    }


    [TestMethod]
    public async Task Run_StaticValidationReturnsFatalError_LogsErrorAndRaisesException()
    {
        // Arrange
        var errorMessage = "A fatal Rule was violated, so the record cannot be added to the database";

        _httpClientFunctionMock.Setup(x => x.GetResponseText(It.IsAny<HttpResponseMessage>()))
            .ReturnsAsync(JsonSerializer.Serialize(new ValidationExceptionLog()
            {
                IsFatal = true,
                CreatedException = false
            }));

        // Act
        await _sut.Run(JsonSerializer.Serialize(_request));

        // Assert
        _loggerMock.Verify(x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(errorMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

        _handleException.Verify(i => i.CreateSystemExceptionLog(
            It.IsAny<Exception>(),
            It.IsAny<BasicParticipantData>(),
            It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task Run_AddParticipantThrowsError_LogsErrorAndRaisesException()
    {
        // Arrange
        var errorMessage = "Unable to call function";

        _httpClientFunctionMock.Setup(x => x.SendPost("DSaddParticipant", It.IsAny<string>()))
            .ThrowsAsync(new Exception("some new exception"));

        // Act
        await _sut.Run(JsonSerializer.Serialize(_request));

        // Assert
        _loggerMock.Verify(x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(errorMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

        _handleException.Verify(i => i.CreateSystemExceptionLog(
            It.IsAny<Exception>(),
            It.IsAny<BasicParticipantData>(),
            It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task Run_FailsToSendParticipantToCohortDistribution_LogsErrorAndRaisesException()
    {
        // Arrange
        var errorMessage = "Participant failed to send to Cohort Distribution Service";

        _cohortDistributionHandler.Setup(x => x.SendToCohortDistributionService(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Participant>()
        )).ReturnsAsync(false);

        // Act
        await _sut.Run(JsonSerializer.Serialize(_request));

        // Assert
        _loggerMock.Verify(x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(errorMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

        _handleException.Verify(i => i.CreateSystemExceptionLog(
            It.Is<Exception>((v, t) => v.ToString().Contains(errorMessage)),
            It.IsAny<BasicParticipantData>(),
            It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task Run_ParticipantHasInvalidScreeningName_RaisesException()
    {
        // Arrange
        var errorMessage = "invalid screening name and therefore cannot be processed by the static validation function";
        _request.Participant.ScreeningName = string.Empty;

        // Act
        await _sut.Run(JsonSerializer.Serialize(_request));

        // Assert
        _handleException.Verify(i => i.CreateRecordValidationExceptionLog(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.Is<string>((v, t) => v.ToString().Contains(errorMessage)),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }
}
