namespace NHS.CohortManager.Tests.UnitTests.ParticipantManagementServiceTests;

using Common;
using Microsoft.Extensions.Logging;
using System.Net;
using Moq;
using System.Text.Json;
using Model;
using addParticipant;
using NHS.Screening.AddParticipant;
using Microsoft.Extensions.Options;

[TestClass]
public class AddNewParticipantTest
{
    private readonly Mock<ILogger<AddParticipantFunction>> _loggerMock = new();
    private readonly Mock<IHttpClientFunction> _httpClientFunctionMock = new();
    private readonly Mock<ICheckDemographic> _checkDemographic = new();
    private readonly Mock<CreateParticipant> _createParticipant = new();
    private readonly Mock<IExceptionHandler> _handleException = new();
    private readonly Mock<ICohortDistributionHandler> _cohortDistributionHandler = new();
    private readonly Mock<IOptions<AddParticipantConfig>> _config = new();
    private readonly AddParticipantFunction _sut;
    private readonly BasicParticipantCsvRecord _request;

    public AddNewParticipantTest()
    {
        var testConfig = new AddParticipantConfig
        {
            DemographicURIGet = "DemographicURIGet",
            DSaddParticipant = "DSaddParticipant",
            StaticValidationURL = "StaticValidationURL"
        };

        _config.Setup(c => c.Value).Returns(testConfig);

        _sut = new AddParticipantFunction(
            _loggerMock.Object,
            _httpClientFunctionMock.Object,
            _checkDemographic.Object,
            _createParticipant.Object,
            _handleException.Object,
            _cohortDistributionHandler.Object,
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

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), "DemographicURIGet"))
            .ReturnsAsync(new Demographic());

        _httpClientFunctionMock.Setup(x => x.SendPost("StaticValidationURL", It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        _httpClientFunctionMock.Setup(x => x.GetResponseText(It.IsAny<HttpResponseMessage>()))
            .ReturnsAsync(JsonSerializer.Serialize(new ValidationExceptionLog()
            {
                IsFatal = false,
                CreatedException = false
            }));

        _httpClientFunctionMock.Setup(x => x.SendPost("DSaddParticipant", It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        _cohortDistributionHandler.Setup(x => x.SendToCohortDistributionService(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Participant>()
        )).ReturnsAsync(true);
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
