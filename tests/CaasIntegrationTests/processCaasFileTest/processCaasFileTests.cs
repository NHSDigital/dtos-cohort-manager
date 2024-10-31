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

    private readonly Mock<IAzureQueueStorageHelper> _azureQueueStorageHelper = new();

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
                new() { NhsNumber = "9876543210", RecordType = Actions.New },
                new() { NhsNumber = "9876543210", RecordType = Actions.New },
            }
        };
        var json = JsonSerializer.Serialize(cohort);
        _request = _setupRequest.Setup(json);

        _checkDemographic.Setup(x => x.PostDemographicDataAsync(It.IsAny<Participant>(), It.IsAny<string>())).Returns(Task.FromResult(true));

        var sut = new ProcessCaasFileFunction(_logger.Object, _callFunction.Object, _createResponse.Object, _checkDemographic.Object, _createBasicParticipantData.Object, _handleException.Object, _azureQueueStorageHelper.Object);

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        _azureQueueStorageHelper.Verify(
            x => x.AddItemToQueueAsync<BasicParticipantCsvRecord>(It.IsAny<BasicParticipantCsvRecord>(), It.IsAny<string>()),
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
                new() {NhsNumber = "9876543210", RecordType = Actions.Amended },
                new() {NhsNumber = "9876543210", RecordType = Actions.Amended }
            }
        };
        var json = JsonSerializer.Serialize(cohort);
        _request = _setupRequest.Setup(json);

        _checkDemographic.Setup(x => x.PostDemographicDataAsync(It.IsAny<Participant>(), It.IsAny<string>())).Returns(Task.FromResult(true));
        var sut = new ProcessCaasFileFunction(_logger.Object, _callFunction.Object, _createResponse.Object, _checkDemographic.Object, _createBasicParticipantData.Object, _handleException.Object, _azureQueueStorageHelper.Object);

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
                new() {NhsNumber = "9876543210",  RecordType = Actions.Removed },
                new() {NhsNumber = "9876543210",  RecordType = Actions.Removed }
            }
        };
        var json = JsonSerializer.Serialize(cohort);

        _request = _setupRequest.Setup(json);

        var sut = new ProcessCaasFileFunction(_logger.Object, _callFunction.Object, _createResponse.Object, _checkDemographic.Object, _createBasicParticipantData.Object, _handleException.Object, _azureQueueStorageHelper.Object);

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        _callFunction.Verify(
            x => x.SendPost(It.Is<string>(s => s.Contains("PMSRemoveParticipant")), It.IsAny<string>()),
            Times.Exactly(2));

        _callFunction.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_ShouldCreateNewExceptionForEachUnknownParticipantRecordType_ErrorIsThrownAndLogged()
    {
        // Arrange
        var cohort = new Cohort
        {
            Participants = new List<Participant>
            {

                new() {NhsNumber = "9876543210", RecordType = "Unknown" }
            }
        };
        var json = JsonSerializer.Serialize(cohort);

        _request = _setupRequest.Setup(json);

        var sut = new ProcessCaasFileFunction(_logger.Object, _callFunction.Object, _createResponse.Object, _checkDemographic.Object, _createBasicParticipantData.Object, _handleException.Object, _azureQueueStorageHelper.Object);

        // Act
        var result = await sut.Run(_request.Object);

        // Assert
        _logger.Verify(log =>
                log.Log(
                LogLevel.Error,
                0,
                It.Is<It.IsAnyType>((state, type) => state.ToString().Contains($"Cannot parse record type with action: Unknown")),
                null,
                (Func<object, Exception, string>)It.IsAny<object>()
                ), Times.Once);

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
                new() {NhsNumber = "9876543210", RecordType = Actions.New }
            }
        };
        var json = JsonSerializer.Serialize(cohort);
        _request = _setupRequest.Setup(json);

        _checkDemographic.Setup(x => x.PostDemographicDataAsync(It.IsAny<Participant>(), It.IsAny<string>())).Returns(Task.FromResult(true));

        _azureQueueStorageHelper.Setup(call => call.AddItemToQueueAsync<BasicParticipantCsvRecord>(It.IsAny<BasicParticipantCsvRecord>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception());

        var sut = new ProcessCaasFileFunction(_logger.Object, _callFunction.Object, _createResponse.Object, _checkDemographic.Object, _createBasicParticipantData.Object, _handleException.Object, _azureQueueStorageHelper.Object);

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
                new() {NhsNumber = "9876543210", RecordType = Actions.Amended }
            }
        };
        var json = JsonSerializer.Serialize(cohort);
        _request = _setupRequest.Setup(json);

        _checkDemographic.Setup(x => x.PostDemographicDataAsync(It.IsAny<Participant>(), It.IsAny<string>())).Returns(Task.FromResult(true));

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("PMSUpdateParticipant")), It.IsAny<string>()))
            .ThrowsAsync(new Exception());

        var sut = new ProcessCaasFileFunction(_logger.Object, _callFunction.Object, _createResponse.Object, _checkDemographic.Object, _createBasicParticipantData.Object, _handleException.Object, _azureQueueStorageHelper.Object);

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
                new() {NhsNumber = "9876543210", RecordType = Actions.Removed }
            }
        };
        var json = JsonSerializer.Serialize(cohort);
        _request = _setupRequest.Setup(json);

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("PMSRemoveParticipant")), It.IsAny<string>()))
            .ThrowsAsync(new Exception());

        var sut = new ProcessCaasFileFunction(_logger.Object, _callFunction.Object, _createResponse.Object, _checkDemographic.Object, _createBasicParticipantData.Object, _handleException.Object, _azureQueueStorageHelper.Object);

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

    [TestMethod]
    public void IsValidDate_ShouldReturnFalse_WhenDateIsInTheFuture()
    {
        // Arrange: Setup dependencies using Mock.Of and create future date
        var logger = Mock.Of<ILogger<ProcessCaasFileFunction>>();
        var callFunction = Mock.Of<ICallFunction>();
        var createResponse = Mock.Of<ICreateResponse>();
        var checkDemographic = Mock.Of<ICheckDemographic>();
        var createBasicParticipantData = Mock.Of<ICreateBasicParticipantData>();
        var handleException = Mock.Of<IExceptionHandler>();

        var validator = new ProcessCaasFileFunction(logger, callFunction, createResponse, checkDemographic, createBasicParticipantData, handleException, _azureQueueStorageHelper.Object);
        var futureDate = DateTime.UtcNow.AddDays(1);

        // Act: Call IsValidDate with a future date
        bool result = validator.IsValidDate(futureDate);

        // Assert: The method should return false for dates in the future
        Assert.IsFalse(result, "The 'IsValidDate' method should return false for dates in the future.");
    }

    [TestMethod]
    public void IsValidDate_ShouldReturnTrue_WhenDateIsNotInTheFuture()
    {
        // Arrange: Setup dependencies using Mock.Of and create future date
        var logger = Mock.Of<ILogger<ProcessCaasFileFunction>>();
        var callFunction = Mock.Of<ICallFunction>();
        var createResponse = Mock.Of<ICreateResponse>();
        var checkDemographic = Mock.Of<ICheckDemographic>();
        var createBasicParticipantData = Mock.Of<ICreateBasicParticipantData>();
        var handleException = Mock.Of<IExceptionHandler>();

        var validator = new ProcessCaasFileFunction(logger, callFunction, createResponse, checkDemographic, createBasicParticipantData, handleException, _azureQueueStorageHelper.Object);
        var pastDate = DateTime.UtcNow.AddDays(-1);
        var currentDate = DateTime.UtcNow;

        // Act & Assert: The method should return true for past and current dates
        Assert.IsTrue(validator.IsValidDate(pastDate), "The 'IsValidDate' method should return true for dates in the past.");
        Assert.IsTrue(validator.IsValidDate(currentDate), "The 'IsValidDate' method should return true for the current date.");
    }

    [TestMethod]
    public void IsValidDate_ShouldReturnTrue_WhenDateIsNull()
    {
        // Arrange: Set up dependencies and create a null date
        var logger = Mock.Of<ILogger<ProcessCaasFileFunction>>();
        var callFunction = Mock.Of<ICallFunction>();
        var createResponse = Mock.Of<ICreateResponse>();
        var checkDemographic = Mock.Of<ICheckDemographic>();
        var createBasicParticipantData = Mock.Of<ICreateBasicParticipantData>();
        var handleException = Mock.Of<IExceptionHandler>();

        var validator = new ProcessCaasFileFunction(logger, callFunction, createResponse, checkDemographic, createBasicParticipantData, handleException, _azureQueueStorageHelper.Object);
        DateTime? nullDate = null;

        // Act: Call IsValidDate with a null date
        bool result = validator.IsValidDate(nullDate);

        // Assert: The method should return true for null dates
        Assert.IsTrue(result, "The 'IsValidDate' method should return true for null dates.");
    }
}
