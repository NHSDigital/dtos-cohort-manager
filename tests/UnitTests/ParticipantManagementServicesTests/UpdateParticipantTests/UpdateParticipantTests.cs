namespace NHS.CohortManager.Tests.ParticipantManagementServiceTests;

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
    private readonly Mock<ICallFunction> _callFunctionMock = new();
    private readonly Mock<HttpWebResponse> _webResponse = new();
    private readonly Mock<ICheckDemographic> _checkDemographic = new();
    private readonly CreateParticipant _createParticipant = new();
    private readonly Mock<IExceptionHandler> _handleException = new();
    private ParticipantCsvRecord _request = new();
    private readonly Mock<ICohortDistributionHandler> _cohortDistributionHandler = new();
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

        var validationResponse = new ValidationExceptionLog { IsFatal = false, CreatedException = false };
        _config.Setup(c => c.Value).Returns(testConfig);

        _callFunctionMock
            .Setup(call => call.GetResponseText(It.IsAny<HttpWebResponse>()))
            .ReturnsAsync(JsonSerializer.Serialize(validationResponse));

        _request.FileName = "test.csv";
        _request.Participant = new Participant()
        {
            NhsNumber = "9727890016",
            ScreeningName = "BS Select",
            EligibilityFlag = EligibilityFlag.Eligible
        };

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        _callFunctionMock
            .Setup(call => call.SendPost("UpdateParticipant", It.IsAny<string>()))
            .ReturnsAsync(_webResponse.Object);
        _callFunctionMock
            .Setup(call => call.SendPost("StaticValidationURL", It.IsAny<string>()))
            .ReturnsAsync(_webResponse.Object);
        _callFunctionMock
            .Setup(call => call.SendPost("CohortDistributionServiceURL", It.IsAny<string>()))
            .ReturnsAsync(_webResponse.Object);
        _callFunctionMock
            .Setup(call => call.SendPost("DSmarkParticipantAsEligible", It.IsAny<string>()))
            .ReturnsAsync(_webResponse.Object);
        _callFunctionMock
            .Setup(call => call.SendPost("markParticipantAsIneligible", It.IsAny<string>()))
            .ReturnsAsync(_webResponse.Object);

        _checkDemographic
            .Setup(x => x.GetDemographicAsync(It.IsAny<string>(), "DemographicURIGet"))
            .ReturnsAsync(new Demographic());

        _cohortDistributionHandler
            .Setup(call => call.SendToCohortDistributionService(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Participant>()))
            .ReturnsAsync(true);

        _handleException
            .Setup(x => x.CreateSystemExceptionLog(
                It.IsAny<Exception>(),
                It.IsAny<Participant>(),
                It.IsAny<string>(),
                It.IsAny<string>())
            );

    }

    [TestMethod]
    public async Task Run_ParticipantEligible_MarkAsEligibleAndSendToCohortDistribution()
    {
        // Arrange
        var sut = new UpdateParticipantFunction(_logger.Object, _callFunctionMock.Object,
                                                _checkDemographic.Object, _createParticipant,
                                                _handleException.Object, _cohortDistributionHandler.Object,
                                                _config.Object);

        // Act
        await sut.Run(JsonSerializer.Serialize(_request));

        // Assert
        _callFunctionMock
            .Verify(x => x.SendPost("UpdateParticipant", It.IsAny<string>()));
        _callFunctionMock
            .Verify(x => x.SendPost("DSmarkParticipantAsEligible", It.IsAny<string>()));

        _cohortDistributionHandler
            .Verify(call => call.SendToCohortDistributionService(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Participant>()));
    }

    [TestMethod]
    public async Task Run_ParticipantIneligible_MarkAsIneligibleAndSendToCohortDistribution()
    {
        // Arrange
        _request.Participant.EligibilityFlag = EligibilityFlag.Ineligible;
        var sut = new UpdateParticipantFunction(_logger.Object, _callFunctionMock.Object,
                                                _checkDemographic.Object, _createParticipant,
                                                _handleException.Object, _cohortDistributionHandler.Object,
                                                _config.Object);

        // Act
        await sut.Run(JsonSerializer.Serialize(_request));

        // Assert
        _callFunctionMock
            .Verify(x => x.SendPost("UpdateParticipant", It.IsAny<string>()));
        _callFunctionMock
            .Verify(x => x.SendPost("markParticipantAsIneligible", It.IsAny<string>()));

        _cohortDistributionHandler
            .Verify(call => call.SendToCohortDistributionService(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Participant>()));
    }

    [TestMethod]
    public async Task Run_FatalRuleTriggered_CreateException()
    {
        // Arrange
        var validationResponse = new ValidationExceptionLog { IsFatal = true, CreatedException = true };

        _callFunctionMock
            .Setup(call => call.GetResponseText(It.IsAny<HttpWebResponse>()))
            .ReturnsAsync(JsonSerializer.Serialize(validationResponse));

        var sut = new UpdateParticipantFunction(_logger.Object, _callFunctionMock.Object,
                                                _checkDemographic.Object, _createParticipant,
                                                _handleException.Object, _cohortDistributionHandler.Object,
                                                _config.Object);

        // Act
        await sut.Run(JsonSerializer.Serialize(_request));

        // Assert
        _handleException
            .Verify(call => call.CreateSystemExceptionLog(
                It.Is<Exception>(e => e.Message == "A fatal Rule was violated and therefore the record cannot be added to the database"),
                It.IsAny<BasicParticipantData>(),
                It.IsAny<string>()
            ));
    }

    [TestMethod]
    public async Task Run_NonFatalRuleTriggered_SetExceptionFlag()
    {
        // Arrange
        var validationResponse = new ValidationExceptionLog { IsFatal = false, CreatedException = true };

        _callFunctionMock
            .Setup(call => call.GetResponseText(It.IsAny<HttpWebResponse>()))
            .ReturnsAsync(JsonSerializer.Serialize(validationResponse));

        var sut = new UpdateParticipantFunction(_logger.Object, _callFunctionMock.Object,
                                                _checkDemographic.Object, _createParticipant,
                                                _handleException.Object, _cohortDistributionHandler.Object,
                                                _config.Object);

        // Act
        await sut.Run(JsonSerializer.Serialize(_request));

        // Assert
        _callFunctionMock
            .Verify(call => call.SendPost(
                "UpdateParticipant",
                It.Is<string>(x => x.Contains(@"""ExceptionFlag"":""Y"""))),
            Times.Once());
    }

    [TestMethod]
    public async Task Run_ParticipantUpdateFails_CreateExcpetion()
    {
        // Arrange
        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.InternalServerError);

        _callFunctionMock
            .Setup(call => call.SendPost("UpdateParticipant", It.IsAny<string>()))
            .ReturnsAsync(_webResponse.Object);

        var sut = new UpdateParticipantFunction(_logger.Object, _callFunctionMock.Object,
                                                _checkDemographic.Object, _createParticipant,
                                                _handleException.Object, _cohortDistributionHandler.Object,
                                                _config.Object);
        // Act
        await sut.Run(JsonSerializer.Serialize(_request));

        // Assert
        _handleException
            .Verify(call => call.CreateSystemExceptionLog(
                It.Is<Exception>(e => e.Message == "There was problem posting the participant to the database"),
                It.IsAny<BasicParticipantData>(),
                It.IsAny<string>()
            ));
    }

    [TestMethod]
    public async Task Run_NoDemographicData_CreateException()
    {
        // Arrange
        _checkDemographic
            .Setup(call => call.GetDemographicAsync(It.IsAny<string>(), "DemographicURIGet"))
            .Throws(new WebException("Demographic data not found"));

        var sut = new UpdateParticipantFunction(_logger.Object, _callFunctionMock.Object,
                                        _checkDemographic.Object, _createParticipant,
                                        _handleException.Object, _cohortDistributionHandler.Object,
                                        _config.Object);

        //Act
        await sut.Run(JsonSerializer.Serialize(_request));

        // Assert
        _handleException
            .Verify(call => call.CreateSystemExceptionLog(
                It.Is<Exception>(e => e.Message == "Demographic data not found"),
                It.IsAny<BasicParticipantData>(),
                It.IsAny<string>()
            ));
    }
}
