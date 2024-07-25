namespace NHS.CohortManager.Tests.CaasIntegrationTests;

using Moq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using Common;
using Model;
using NHS.CohortManager.Tests.TestUtils;
using NHS.CohortManager.CaasIntegrationService;

[TestClass]
public class ProcessCaasFileTests
{
    private readonly Mock<ILogger<ProcessCaasFileFunction>> _logger = new();
    private readonly Mock<ICallFunction> _callFunction = new();
    private Mock<HttpRequestData> _request;
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<ICheckDemographic> _checkDemographic = new();
    private readonly SetupRequest _setupRequest = new();
    private readonly Mock<ICreateBasicParticipantData> _createBasicParticipantData = new();
    private readonly Mock<IExceptionHandler> _handleException = new();

    public ProcessCaasFileTests()
    {
        Environment.SetEnvironmentVariable("PMSAddParticipant", "PMSAddParticipant");
        Environment.SetEnvironmentVariable("PMSRemoveParticipant", "PMSRemoveParticipant");
        Environment.SetEnvironmentVariable("PMSUpdateParticipant", "PMSUpdateParticipant");
        Environment.SetEnvironmentVariable("StaticValidationURL", "StaticValidationURL");
    }

    [TestMethod]
    public async Task Run_Should_Call_AddParticipant_For_Each_New_Participant()
    {
        // Arrange
        var cohort = new Cohort
        {
            Participants = new List<Participant>
            {
                new() { RecordType = Actions.New },
                new() { RecordType = Actions.New },
            }
        };
        var json = JsonSerializer.Serialize(cohort);
        _request = _setupRequest.Setup(json);

        var sut = new ProcessCaasFileFunction(_logger.Object, _callFunction.Object, _createResponse.Object, _checkDemographic.Object, _createBasicParticipantData.Object, _handleException.Object);

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        _callFunction.Verify(
            x => x.SendPost(It.Is<string>(s => s.Contains("PMSAddParticipant")), It.IsAny<string>()),
            Times.Exactly(2));
        _callFunction.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_Should_Call_UpdateParticipant_For_Each_Amended_Participant()
    {
        // Arrange
        var cohort = new Cohort
        {
            Participants = new List<Participant>
            {
                new() { RecordType = Actions.Amended },
                new() { RecordType = Actions.Amended }
            }
        };
        var json = JsonSerializer.Serialize(cohort);
        _request = _setupRequest.Setup(json);

        var sut = new ProcessCaasFileFunction(_logger.Object, _callFunction.Object, _createResponse.Object, _checkDemographic.Object, _createBasicParticipantData.Object, _handleException.Object);

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        _callFunction.Verify(
            x => x.SendPost(It.Is<string>(s => s.Contains("PMSUpdateParticipant")), It.IsAny<string>()),
            Times.Exactly(2));
        _callFunction.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_Should_Call_RemoveParticipant_For_Each_Removed_Participant()
    {
        // Arrange
        var cohort = new Cohort
        {
            Participants = new List<Participant>
            {
                new() { RecordType = Actions.Removed },
                new() { RecordType = Actions.Removed }
            }
        };
        var json = JsonSerializer.Serialize(cohort);

        _request = _setupRequest.Setup(json);

        var sut = new ProcessCaasFileFunction(_logger.Object, _callFunction.Object, _createResponse.Object, _checkDemographic.Object, _createBasicParticipantData.Object, _handleException.Object);

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        _callFunction.Verify(
            x => x.SendPost(It.Is<string>(s => s.Contains("PMSRemoveParticipant")), It.IsAny<string>()),
            Times.Exactly(2));
        _callFunction.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_Should_Call_StaticValidation_For_Each_Unknown_Participant_RecordType()
    {
        // Arrange
        var cohort = new Cohort
        {
            Participants = new List<Participant>
            {
                new() { RecordType = null },
                new() { RecordType = "Unknown" }
            }
        };
        var json = JsonSerializer.Serialize(cohort);

        _request = _setupRequest.Setup(json);

        var sut = new ProcessCaasFileFunction(_logger.Object, _callFunction.Object, _createResponse.Object, _checkDemographic.Object, _createBasicParticipantData.Object, _handleException.Object);

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        _callFunction.Verify(
            x => x.SendPost(It.Is<string>(s => s.Contains("StaticValidationURL")), It.IsAny<string>()),
            Times.Exactly(2));
        _callFunction.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_Should_Log_Error_When_AddParticipant_Fails()
    {
        // Arrange
        var cohort = new Cohort
        {
            Participants = new List<Participant>
            {
                new() { RecordType = Actions.New }
            }
        };
        var json = JsonSerializer.Serialize(cohort);
        _request = _setupRequest.Setup(json);

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("PMSAddParticipant")), It.IsAny<string>()))
            .ThrowsAsync(new Exception());

        var sut = new ProcessCaasFileFunction(_logger.Object, _callFunction.Object, _createResponse.Object, _checkDemographic.Object, _createBasicParticipantData.Object, _handleException.Object);

        // Act
        await sut.Run(_request.Object);

        // Assert
        _logger.Verify(log =>
            log.Log(
            LogLevel.Error,
            0,
            It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("Add participant function failed.")),
            null,
            (Func<object, Exception, string>)It.IsAny<object>()
            ), Times.Once);
    }

    [TestMethod]
    public async Task Run_Should_Log_Error_When_UpdateParticipant_Fails()
    {
        // Arrange
        var cohort = new Cohort
        {
            Participants = new List<Participant>
            {
                new() { RecordType = Actions.Amended }
            }
        };
        var json = JsonSerializer.Serialize(cohort);
        _request = _setupRequest.Setup(json);

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("PMSUpdateParticipant")), It.IsAny<string>()))
            .ThrowsAsync(new Exception());

        var sut = new ProcessCaasFileFunction(_logger.Object, _callFunction.Object, _createResponse.Object, _checkDemographic.Object, _createBasicParticipantData.Object, _handleException.Object);

        // Act
        await sut.Run(_request.Object);

        // Assert
        _logger.Verify(log =>
            log.Log(
            LogLevel.Error,
            0,
            It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("Update participant function failed.")),
            null,
            (Func<object, Exception, string>)It.IsAny<object>()
            ), Times.Once);
    }

    [TestMethod]
    public async Task Run_Should_Log_Error_When_RemoveParticipant_Fails()
    {
        // Arrange
        var cohort = new Cohort
        {
            Participants = new List<Participant>
            {
                new() { RecordType = Actions.Removed }
            }
        };
        var json = JsonSerializer.Serialize(cohort);
        _request = _setupRequest.Setup(json);

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("PMSRemoveParticipant")), It.IsAny<string>()))
            .ThrowsAsync(new Exception());

        var sut = new ProcessCaasFileFunction(_logger.Object, _callFunction.Object, _createResponse.Object, _checkDemographic.Object, _createBasicParticipantData.Object, _handleException.Object);

        // Act
        await sut.Run(_request.Object);

        // Assert
        _logger.Verify(log =>
            log.Log(
            LogLevel.Error,
            0,
            It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("Remove participant function failed.")),
            null,
            (Func<object, Exception, string>)It.IsAny<object>()
            ), Times.Once);
    }
}
