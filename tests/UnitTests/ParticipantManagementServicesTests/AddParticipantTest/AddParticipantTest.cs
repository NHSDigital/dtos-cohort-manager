namespace NHS.CohortManager.Tests.ParticipantManagementServiceTests;

using Common;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using Moq;
using System.Text.Json;
using Model;
using addParticipant;
using NHS.CohortManager.Tests.TestUtils;
using System.Text;

[TestClass]
public class AddParticipantTests
{
    private readonly Mock<ILogger<AddParticipantFunction>> _loggerMock = new();
    private readonly Mock<ICallFunction> _callFunctionMock = new();
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<HttpWebResponse> _webResponse = new();
    private readonly Mock<HttpWebResponse> _validationResponse = new();
    private readonly Mock<HttpWebResponse> _sendToCohortDistributionResponse = new();
    private readonly Mock<ICheckDemographic> _checkDemographic = new();
    private readonly CreateParticipant _createParticipant = new();
    private readonly Mock<IExceptionHandler> _handleException = new();
    private readonly Mock<ICohortDistributionHandler> _cohortDistributionHandler = new();
    private BasicParticipantCsvRecord _request = new();

    public AddParticipantTests()
    {
        Environment.SetEnvironmentVariable("DSaddParticipant", "DSaddParticipant");
        Environment.SetEnvironmentVariable("DSmarkParticipantAsEligible", "DSmarkParticipantAsEligible");
        Environment.SetEnvironmentVariable("DemographicURIGet", "DemographicURIGet");
        Environment.SetEnvironmentVariable("StaticValidationURL", "StaticValidationURL");
        Environment.SetEnvironmentVariable("CohortDistributionServiceURL", "CohortDistributionServiceURL");

        _request.FileName = "ExampleFileName.parquet";
        _request.Participant = new BasicParticipantData{ NhsNumber = "1234567890", ScreeningName = "BS Select" };

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        _sendToCohortDistributionResponse.Setup(x => x.StatusCode)
            .Returns(HttpStatusCode.OK);
        _validationResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        var validationResponse = new ValidationExceptionLog { IsFatal = false, CreatedException = false };

        _callFunctionMock
            .Setup(call => call.GetResponseText(It.IsAny<HttpWebResponse>()))
            .ReturnsAsync(JsonSerializer.Serialize(validationResponse));

        _callFunctionMock
            .Setup(call => call.SendPost("StaticValidationURL", It.IsAny<string>()))
            .ReturnsAsync(_validationResponse.Object);
        _callFunctionMock
            .Setup(call => call.SendPost("DSaddParticipant", It.IsAny<string>()))
            .ReturnsAsync(_webResponse.Object);
        _callFunctionMock
            .Setup(call => call.SendPost("DSmarkParticipantAsEligible", It.IsAny<string>()))
            .ReturnsAsync(_webResponse.Object);

        _cohortDistributionHandler
            .Setup(call => call.SendToCohortDistributionService(
                It.IsAny<string>(), 
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Participant>()))
            .ReturnsAsync(true);

        _checkDemographic
            .Setup(call => call.GetDemographicAsync(It.IsAny<string>(), "DemographicURIGet"))
            .ReturnsAsync(new Demographic())
            .Verifiable();

        _createResponse
            .Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .Returns(It.IsAny<HttpResponseData>());

        _handleException
            .Setup(x => x.CreateSystemExceptionLog(
                It.IsAny<Exception>(),
                It.IsAny<Participant>(),
                It.IsAny<string>())
            );
    }

    [TestMethod]
    public async Task Run_ValidRequest_SendToCohortDistribution()
    {
        // Arrange
        var sut = new AddParticipantFunction(_loggerMock.Object, _callFunctionMock.Object,
                                            _createResponse.Object, _checkDemographic.Object,
                                            _createParticipant, _handleException.Object,
                                            _cohortDistributionHandler.Object);

        // Act
        await sut.Run(JsonSerializer.Serialize(_request));

        // Assert
        _callFunctionMock
            .Verify(x => x.SendPost("DSaddParticipant", It.IsAny<string>()));
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
    public async Task Run_CreateFails_CreateException()
    {
        // Arrange
        var errorResponse = new Mock<HttpWebResponse>();
        errorResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.BadRequest);

        _callFunctionMock
            .Setup(call => call.SendPost("DSaddParticipant", It.IsAny<string>()))
            .ReturnsAsync(errorResponse.Object);

        var sut = new AddParticipantFunction(
            _loggerMock.Object,
            _callFunctionMock.Object,
            _createResponse.Object,
            _checkDemographic.Object,
            _createParticipant,
            _handleException.Object,
             _cohortDistributionHandler.Object);

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
    public async Task Run_MarkAsEligibleFails_CreateException()
    {
        // Arrange
        var eligibleResponse = new Mock<HttpWebResponse>();
        eligibleResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.BadRequest);

        _callFunctionMock
            .Setup(call => call.SendPost("DSmarkParticipantAsEligible", It.IsAny<string>()))
            .ReturnsAsync(eligibleResponse.Object);

        var sut = new AddParticipantFunction(
            _loggerMock.Object,
            _callFunctionMock.Object,
            _createResponse.Object,
            _checkDemographic.Object,
            _createParticipant,
            _handleException.Object,
            _cohortDistributionHandler.Object);

        // Act
        await sut.Run(JsonSerializer.Serialize(_request));

        //Assert
        _handleException
            .Verify(call => call.CreateSystemExceptionLog(
                It.Is<Exception>(e => e.Message.Contains("There was an error while marking participant as eligible")),
                It.IsAny<BasicParticipantData>(),
                It.IsAny<string>()
            ));
    }

    [TestMethod]
    public async Task Run_CreateCohortDistributionFails_CreateException()
    {
        // Arrange
        _cohortDistributionHandler
            .Setup(call => call.SendToCohortDistributionService(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Participant>()))
            .ReturnsAsync(false);

        var sut = new AddParticipantFunction(_loggerMock.Object, _callFunctionMock.Object,
                                            _createResponse.Object, _checkDemographic.Object,
                                            _createParticipant, _handleException.Object,
                                            _cohortDistributionHandler.Object);

        // Act
        await sut.Run(JsonSerializer.Serialize(_request));

        // Assert
        _handleException
            .Verify(call => call.CreateSystemExceptionLog(
                It.Is<Exception>(e => e.Message == "participant failed to send to Cohort Distribution Service"),
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
            .Throws(new WebException("participant not found"));

        var sut = new AddParticipantFunction(_loggerMock.Object, _callFunctionMock.Object,
                                            _createResponse.Object, _checkDemographic.Object,
                                            _createParticipant, _handleException.Object,
                                            _cohortDistributionHandler.Object);

        // Act
        await sut.Run(JsonSerializer.Serialize(_request));

        // Assert
        _handleException
            .Verify(call => call.CreateSystemExceptionLog(
                It.Is<Exception>(e => e.Message == "participant not found"),
                It.IsAny<BasicParticipantData>(),
                It.IsAny<string>()
            ));
    }

    [TestMethod]
    public async Task Run_FatalRuleTriggered_CreateException()
    {
        // Arrange
        var validationResponse = new ValidationExceptionLog { IsFatal = true, CreatedException = true };

        _callFunctionMock
            .Setup(call => call.GetResponseText(It.IsAny<HttpWebResponse>()))
            .ReturnsAsync(JsonSerializer.Serialize(validationResponse));

        var sut = new AddParticipantFunction(_loggerMock.Object, _callFunctionMock.Object,
                                            _createResponse.Object, _checkDemographic.Object,
                                            _createParticipant, _handleException.Object,
                                            _cohortDistributionHandler.Object);

        // Act
        await sut.Run(JsonSerializer.Serialize(_request));

        // Assert
        _handleException
            .Verify(call => call.CreateSystemExceptionLog(
                It.Is<Exception>(e => e.Message == "A fatal Rule was violated, so the record cannot be added to the database"),
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

        var sut = new AddParticipantFunction(_loggerMock.Object, _callFunctionMock.Object,
                                            _createResponse.Object, _checkDemographic.Object,
                                            _createParticipant, _handleException.Object,
                                            _cohortDistributionHandler.Object);

        // Act
        await sut.Run(JsonSerializer.Serialize(_request));

        // Assert
        _callFunctionMock
            .Verify(call => call.SendPost(
                "DSaddParticipant",
                It.Is<string>(x => x.Contains(@"""ExceptionFlag"":""Y"""))),
            Times.Once());
    }
}
