namespace NHS.CohortManager.Tests.ParticipantManagementServiceTests;

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
    private readonly CreateParticipant _createParticipant = new();
    private readonly Mock<IExceptionHandler> _handleException = new();
    private BasicParticipantCsvRecord _request = new();
    private readonly Mock<ICohortDistributionHandler> _cohortDistributionHandler = new();
    private readonly Mock<IQueueClient> _azureQueueStorageHelper = new();
    private readonly Mock<IOptions<UpdateParticipantConfig>> _config = new();

    public UpdateParticipantTests()
    {
        var testConfig = new UpdateParticipantConfig
        {
            DemographicURIGet = "DemographicURIGet",
            UpdateParticipant = "UpdateParticipant",
            StaticValidationURL = "StaticValidationURL"
        };

        var validationResponse = new ValidationExceptionLog { IsFatal = false, CreatedException = false };
        _config.Setup(c => c.Value).Returns(testConfig);

        _httpClientFunction
            .Setup(call => call.GetResponseText(It.IsAny<HttpResponseMessage>()))
            .ReturnsAsync(JsonSerializer.Serialize(validationResponse));

        _request.FileName = "test.csv";
        _request.BasicParticipantData = new BasicParticipantData()
        {
            NhsNumber = "9727890016",
            ScreeningName = "BS Select",
            EligibilityFlag = EligibilityFlag.Eligible
        };

        _httpClientFunction
            .Setup(call => call.SendPost("UpdateParticipant", It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });
        _httpClientFunction
            .Setup(call => call.SendPost("StaticValidationURL", It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });
        _httpClientFunction
            .Setup(call => call.SendPost("CohortDistributionServiceURL", It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });


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
        var sut = new UpdateParticipantFunction(_logger.Object, _httpClientFunction.Object,
                                                _checkDemographic.Object, _createParticipant,
                                                _handleException.Object, _cohortDistributionHandler.Object,
                                                _config.Object);

        // Act
        await sut.Run(JsonSerializer.Serialize(_request));

        // Assert
        _httpClientFunction
            .Verify(x => x.SendPost("UpdateParticipant", It.IsAny<string>()));

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
        _request.BasicParticipantData.EligibilityFlag = EligibilityFlag.Ineligible;
        var sut = new UpdateParticipantFunction(_logger.Object, _httpClientFunction.Object,
                                                _checkDemographic.Object, _createParticipant,
                                                _handleException.Object, _cohortDistributionHandler.Object,
                                                _config.Object);

        // Act
        await sut.Run(JsonSerializer.Serialize(_request));

        // Assert
        _httpClientFunction
            .Verify(x => x.SendPost("UpdateParticipant", It.IsAny<string>()));

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

        _httpClientFunction
            .Setup(call => call.GetResponseText(It.IsAny<HttpResponseMessage>()))
            .ReturnsAsync(JsonSerializer.Serialize(validationResponse));

        var sut = new UpdateParticipantFunction(_logger.Object, _httpClientFunction.Object,
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

        _httpClientFunction
            .Setup(call => call.GetResponseText(It.IsAny<HttpResponseMessage>()))
            .ReturnsAsync(JsonSerializer.Serialize(validationResponse));

        var sut = new UpdateParticipantFunction(_logger.Object, _httpClientFunction.Object,
                                                _checkDemographic.Object, _createParticipant,
                                                _handleException.Object, _cohortDistributionHandler.Object,
                                                _config.Object);

        // Act
        await sut.Run(JsonSerializer.Serialize(_request));

        // Assert
        _httpClientFunction
            .Verify(call => call.SendPost(
                "UpdateParticipant",
                It.Is<string>(x => x.Contains(@"""ExceptionFlag"":""Y"""))),
            Times.Once());
    }

    [TestMethod]
    public async Task Run_ParticipantUpdateFails_CreateExcpetion()
    {
        // Arrange
        _httpClientFunction
            .Setup(call => call.SendPost("UpdateParticipant", It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });

        var sut = new UpdateParticipantFunction(_logger.Object, _httpClientFunction.Object,
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

        var sut = new UpdateParticipantFunction(_logger.Object, _httpClientFunction.Object,
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
