namespace NHS.CohortManager.Tests.UnitTests.ParticipantManagementServiceTests;

using System.Net;
using System.Text.Json;
using Common;
using Microsoft.Extensions.Logging;
using Model;
using Moq;
using updateParticipant;
using Microsoft.Extensions.Options;
using NHS.Screening.UpdateParticipant;

[TestClass]
public class UpdateParticipantTests
{
    private readonly Mock<ILogger<UpdateParticipantFunction>> _logger = new();
    private readonly Mock<IHttpClientFunction> _httpClientFunction = new();
    private readonly Mock<ICheckDemographic> _checkDemographic = new();
    private readonly Mock<CreateParticipant> _createParticipant = new();
    private readonly Mock<IExceptionHandler> _handleException = new();
    private readonly Mock<ICohortDistributionHandler> _cohortDistributionHandler = new();
    private readonly ParticipantCsvRecord _participantCsvRecord;
    private readonly Mock<IOptions<UpdateParticipantConfig>> _config = new();
    private readonly UpdateParticipantFunction _sut;

    public UpdateParticipantTests()
    {
        var testConfig = new UpdateParticipantConfig
        {
            DemographicURIGet = "DemographicURIGet",
            UpdateParticipant = "UpdateParticipant",
            StaticValidationURL = "StaticValidationURL",
            DSmarkParticipantAsEligible = "DSmarkParticipantAsEligible",
            markParticipantAsIneligible = "markParticipantAsIneligible"
        };

        _config.Setup(c => c.Value).Returns(testConfig);

        _participantCsvRecord = new ParticipantCsvRecord
        {
            FileName = "test.csv",
            Participant = new Participant()
            {
                NhsNumber = "1",
            }
        };

        _sut = new UpdateParticipantFunction(
            _logger.Object,
            _httpClientFunction.Object,
            _checkDemographic.Object,
            _createParticipant.Object,
            _handleException.Object,
            _cohortDistributionHandler.Object,
            _config.Object
        );

        _httpClientFunction.Setup(x => x.SendPost(It.Is<string>(s => s.Contains("StaticValidationURL")), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        _httpClientFunction.Setup(x => x.SendPost(It.Is<string>(s => s.Contains("UpdateParticipant")), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        _httpClientFunction.Setup(x => x.SendPost(It.Is<string>(s => s.Contains("CohortDistributionServiceURL")), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        _httpClientFunction.Setup(x => x.SendPost(It.Is<string>(s => s.Contains("DSmarkParticipantAsEligible")), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        _httpClientFunction.Setup(x => x.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), It.Is<string>(s => s.Contains("DemographicURIGet"))))
            .ReturnsAsync(new Demographic());

        _httpClientFunction.Setup(x => x.GetResponseText(It.IsAny<HttpResponseMessage>()))
            .ReturnsAsync(JsonSerializer.Serialize(new ValidationExceptionLog()
            {
                IsFatal = false,
                CreatedException = false
            }));
    }

    [TestMethod]
    public async Task Run_ParticipantValidationFails_LogsExceptionRaised()
    {
        // Arrange
        _httpClientFunction.Setup(x => x.SendPost(It.Is<string>(s => s.Contains("StaticValidationURL")), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest });

        // Act
        await _sut.Run(JsonSerializer.Serialize(_participantCsvRecord));

        // Assert
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("The participant has been updated but a validation Exception was raised")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);

        _httpClientFunction.Verify(x => x.SendPost(
            It.Is<string>(s => s == "UpdateParticipant"),
            It.IsAny<string>()),
        Times.Once());
    }

    [TestMethod]
    public async Task Run_ParticipantIneligibleAndUpdateSuccessful_ParticipantUpdated()
    {
        // Act
        await _sut.Run(JsonSerializer.Serialize(_participantCsvRecord));

        // Assert
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Participant updated.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task Run_ParticipantUpdateFails_LogsParticipantFailed()
    {
        // Arrange
        _httpClientFunction.Setup(x => x.SendPost(It.Is<string>(s => s.Contains("UpdateParticipant")), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest });

        _httpClientFunction.Setup(x => x.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>()))
            .ThrowsAsync(new Exception("some new exception"));

        // Act
        await _sut.Run(JsonSerializer.Serialize(_participantCsvRecord));

        // Assert
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Unsuccessfully updated records")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
    }

    [TestMethod]
    public async Task Run_ParticipantUpdateThrowsException_ParticipantFailed()
    {
        // Arrange
        _httpClientFunction.Setup(x => x.SendPost(It.Is<string>(s => s.Contains("UpdateParticipant")), It.IsAny<string>()))
            .ThrowsAsync(new Exception("some new exception"));

        // Act
        await _sut.Run(JsonSerializer.Serialize(_participantCsvRecord));

        // Assert
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Update participant failed")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task Run_DemographicDataIsNull_DemographicFunctionFailed()
    {
        // Arrange
        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), It.Is<string>(s => s.Contains("DemographicURIGet"))))
            .Returns(Task.FromResult<Demographic>(null));

        //Act
        await _sut.Run(JsonSerializer.Serialize(_participantCsvRecord));

        // Assert
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("demographic function failed")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
    }

    [TestMethod]
    public async Task Run_ParticipantEligibleAndUpdateSuccessful_ParticipantUpdated()
    {
        // Arrange
        _participantCsvRecord.Participant.NhsNumber = "9727890016";
        _participantCsvRecord.Participant.ScreeningName = "BSS";

        // Act
        await _sut.Run(JsonSerializer.Serialize(_participantCsvRecord));

        // Assert
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Participant updated.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.AtLeastOnce);
    }
}
