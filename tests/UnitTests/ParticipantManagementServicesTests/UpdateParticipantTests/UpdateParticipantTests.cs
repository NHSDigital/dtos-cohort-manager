namespace NHS.CohortManager.Tests.UnitTests.ParticipantManagementServiceTests;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;
using Moq;
using NHS.CohortManager.Tests.TestUtils;
using updateParticipant;
using RulesEngine.Models;
using Microsoft.Extensions.Options;
using NHS.Screening.UpdateParticipant;

[TestClass]
public class UpdateParticipantTests
{
    private readonly Mock<ILogger<UpdateParticipantFunction>> _logger = new();
    private readonly Mock<ILogger<CohortDistributionHandler>> _cohortDistributionLogger = new();
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly Mock<HttpWebResponse> _webResponse = new();
    private readonly Mock<HttpWebResponse> _webResponseSuccess = new();
    private readonly Mock<ICheckDemographic> _checkDemographic = new();
    private readonly CreateParticipant _createParticipant = new();
    private readonly Mock<HttpWebResponse> _validationWebResponse = new();
    private readonly Mock<HttpWebResponse> _updateParticipantWebResponse = new();
    private readonly Mock<IExceptionHandler> _handleException = new();
    private readonly ICohortDistributionHandler _cohortDistributionHandler;
    private readonly SetupRequest _setupRequest = new();
    private readonly ParticipantCsvRecord _participantCsvRecord;
    private Mock<HttpRequestData> _request;
    private readonly Mock<IAzureQueueStorageHelper> _azureQueueStorageHelper = new();
    private readonly Mock<IOptions<UpdateParticipantConfig>> _config = new();

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

        _cohortDistributionHandler = new CohortDistributionHandler(_cohortDistributionLogger.Object, _azureQueueStorageHelper.Object);

        _handleException.Setup(x => x.CreateValidationExceptionLog(It.IsAny<IEnumerable<RuleResultTree>>(), It.IsAny<ParticipantCsvRecord>()))
            .Returns(Task.FromResult(new ValidationExceptionLog()
            {
                IsFatal = true,
                CreatedException = true
            })).Verifiable();

        _participantCsvRecord = new ParticipantCsvRecord
        {
            FileName = "test.csv",
            Participant = new Participant()
            {
                NhsNumber = "1",
            }
        };
    }

    [TestMethod]
    public async Task Run_ParticipantValidationFails_LogsExceptionRaised()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        _request = _setupRequest.Setup(json);


        var sut = new UpdateParticipantFunction(_logger.Object, 
                                                _callFunction.Object, 
                                                _checkDemographic.Object, 
                                                _createParticipant, 
                                                _handleException.Object, 
                                                _cohortDistributionHandler,
                                                _config.Object);

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.BadRequest);
        _webResponseSuccess.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("StaticValidationURL")), It.IsAny<string>()))
                        .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("UpdateParticipant")), It.IsAny<string>()))
                .Returns(Task.FromResult<HttpWebResponse>(_webResponseSuccess.Object));


        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("CohortDistributionServiceURL")), It.IsAny<string>()))
                .Returns(Task.FromResult<HttpWebResponse>(_webResponseSuccess.Object));

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("DSmarkParticipantAsEligible")), It.IsAny<string>()))
                .Returns(Task.FromResult<HttpWebResponse>(_webResponseSuccess.Object));

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>()))
                .Returns(Task.FromResult<HttpWebResponse>(_webResponseSuccess.Object));

        _checkDemographic.Setup(call => call.GetDemographicAsync(It.IsAny<string>(), It.Is<string>(s => s.Contains("DemographicURIGet"))))
                        .Returns(Task.FromResult<Demographic>(new Demographic()));

        _callFunction.Setup(x => x.GetResponseText(It.IsAny<HttpWebResponse>())).Returns(Task.FromResult<string>(
        JsonSerializer.Serialize<ValidationExceptionLog>(new ValidationExceptionLog()
        {
            IsFatal = false,
            CreatedException = false
        })));

        // Act
        await sut.Run(json);

        // Assert
        _logger.Verify(log =>
            log.Log(
                LogLevel.Information,
                0,
                It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("The participant has been updated but a validation Exception was raised")),
                null,
                (Func<object, Exception, string>)It.IsAny<object>()
            ));

        _callFunction.Verify(call => call.SendPost(It.Is<string>(s => s == "UpdateParticipant"), It.IsAny<string>()), Times.Once());
    }

    [TestMethod]
    public async Task Run_ParticipantIneligibleAndUpdateSuccessful_ParticipantUpdated()
    {
        // Arrange
        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        var json = JsonSerializer.Serialize(_participantCsvRecord);

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("UpdateParticipant")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("StaticValidationURL")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("CohortDistributionServiceURL")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("DSmarkParticipantAsEligible")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));


        _callFunction.Setup(x => x.GetResponseText(It.IsAny<HttpWebResponse>())).Returns(Task.FromResult<string>(json));

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), It.Is<string>(s => s.Contains("DemographicURIGet"))))
            .Returns(Task.FromResult<Demographic>(new Demographic()));

        _callFunction.Setup(x => x.GetResponseText(It.IsAny<HttpWebResponse>())).Returns(Task.FromResult<string>(
        JsonSerializer.Serialize<ValidationExceptionLog>(new ValidationExceptionLog()
        {
            IsFatal = false,
            CreatedException = false
        })));

        _request = _setupRequest.Setup(json);

        var sut = new UpdateParticipantFunction(_logger.Object, 
                                                _callFunction.Object, 
                                                _checkDemographic.Object, 
                                                _createParticipant, 
                                                _handleException.Object, 
                                                _cohortDistributionHandler,
                                                _config.Object);

        // Act
        await sut.Run(json);

        // Assert


        _logger.Verify(log =>
            log.Log(
                LogLevel.Information,
                0,
                It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("Participant updated.")),
                null,
                (Func<object, Exception, string>)It.IsAny<object>()
            ));
    }

    [TestMethod]
    public async Task Run_ParticipantUpdateFails_LogsParticipantFailed()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participantCsvRecord);

        _request = _setupRequest.Setup(json);

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("StaticValidationURL")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("CohortDistributionServiceURL")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _updateParticipantWebResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.BadRequest);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("UpdateParticipant")), It.IsAny<string>()))
                        .Returns(Task.FromResult<HttpWebResponse>(_updateParticipantWebResponse.Object));

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("DSmarkParticipantAsEligible")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>()))
            .ThrowsAsync(new Exception("some new exception"));



        _callFunction.Setup(x => x.GetResponseText(It.IsAny<HttpWebResponse>())).Returns(Task.FromResult<string>(
        JsonSerializer.Serialize<ValidationExceptionLog>(new ValidationExceptionLog()
        {
            IsFatal = false,
            CreatedException = false
        })));

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), It.Is<string>(s => s.Contains("DemographicURIGet"))))
        .Returns(Task.FromResult<Demographic>(new Demographic()));

        var sut = new UpdateParticipantFunction(_logger.Object, 
                                                _callFunction.Object, 
                                                _checkDemographic.Object, 
                                                _createParticipant, 
                                                _handleException.Object, 
                                                _cohortDistributionHandler,
                                                _config.Object);

        // Act
        await sut.Run(json);

        // Assert
        _logger.Verify(log =>
            log.Log(
                LogLevel.Error,
                0,
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.AtLeastOnce(), "Unsuccessfully updated records");
    }

    [TestMethod]
    public async Task Run_ParticipantUpdateThrowsException_ParticipantFailed()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        _request = _setupRequest.Setup(json);

        _validationWebResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s == "StaticValidationURL"), json))
            .Returns(Task.FromResult<HttpWebResponse>(_validationWebResponse.Object));

        _updateParticipantWebResponse.Setup(x => x.StatusCode).Throws(new Exception("an error occurred"));
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("UpdateParticipant")), It.IsAny<string>()))
                        .Returns(Task.FromResult<HttpWebResponse>(_updateParticipantWebResponse.Object));

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("DSmarkParticipantAsEligible")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_validationWebResponse.Object));

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_validationWebResponse.Object));

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), It.Is<string>(s => s.Contains("DemographicURIGet"))))
                        .Returns(Task.FromResult<Demographic>(new Demographic()));


        var sut = new UpdateParticipantFunction(_logger.Object, 
                                                _callFunction.Object, 
                                                _checkDemographic.Object, 
                                                _createParticipant, 
                                                _handleException.Object, 
                                                _cohortDistributionHandler,
                                                _config.Object);

        // Act
        await sut.Run(json);

        // Assert

        _logger.Verify(log =>
            log.Log(
                LogLevel.Information,
                0,
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.AtLeastOnce(), "Update participant failed");
    }

    [TestMethod]
    public async Task Run_DemographicDataIsNull_DemographicFunctionFailed()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        _request = _setupRequest.Setup(json);

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("UpdateParticipant")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("StaticValidationURL")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("CohortDistributionServiceURL")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("DSmarkParticipantAsEligible")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        var sut = new UpdateParticipantFunction(_logger.Object, 
                                                _callFunction.Object, 
                                                _checkDemographic.Object, 
                                                _createParticipant, 
                                                _handleException.Object, 
                                                _cohortDistributionHandler,
                                                _config.Object);
                                                
        //Act
        await sut.Run(json);

        // Assert
        _logger.Verify(log =>
            log.Log(
                LogLevel.Information,
                0,
                It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("demographic function failed")),
                null,
                (Func<object, Exception, string>)It.IsAny<object>()
            ));
    }

    [TestMethod]
    public async Task Run_ParticipantEligibleAndUpdateSuccessful_ParticipantUpdated()
    {
        _participantCsvRecord.Participant.NhsNumber = "9727890016";
        _participantCsvRecord.Participant.ScreeningName = "BSS";
        // Arrange
        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        var json = JsonSerializer.Serialize(_participantCsvRecord);

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("UpdateParticipant")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("StaticValidationURL")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("CohortDistributionServiceURL")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("DSmarkParticipantAsEligible")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunction.Setup(x => x.GetResponseText(It.IsAny<HttpWebResponse>())).Returns(Task.FromResult<string>(json));

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), It.Is<string>(s => s.Contains("DemographicURIGet"))))
            .Returns(Task.FromResult<Demographic>(new Demographic()));

        _callFunction.Setup(x => x.GetResponseText(It.IsAny<HttpWebResponse>())).Returns(Task.FromResult<string>(
        JsonSerializer.Serialize<ValidationExceptionLog>(new ValidationExceptionLog()
        {
            IsFatal = false,
            CreatedException = false
        })));

        _request = _setupRequest.Setup(json);

        var sut = new UpdateParticipantFunction(_logger.Object, 
                                                _callFunction.Object, 
                                                _checkDemographic.Object, 
                                                _createParticipant, 
                                                _handleException.Object, 
                                                _cohortDistributionHandler,
                                                _config.Object);
                                                
        // Act
        await sut.Run(json);

        // Assert

        _logger.Verify(log =>
            log.Log(
                LogLevel.Information,
                0,
                It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("Participant updated.")),
                null,
                (Func<object, Exception, string>)It.IsAny<object>()
            ));
    }
}
