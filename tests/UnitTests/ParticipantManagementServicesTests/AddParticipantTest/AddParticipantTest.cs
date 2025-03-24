namespace NHS.CohortManager.Tests.UnitTests.ParticipantManagementServiceTests;

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
using NHS.Screening.AddParticipant;
using Microsoft.Extensions.Options;

[TestClass]
public class AddNewParticipantTest
{
    private readonly Mock<ILogger<AddParticipantFunction>> _loggerMock = new();
    private readonly Mock<ILogger<CohortDistributionHandler>> _cohortDistributionLogger = new();
    private readonly Mock<ICallFunction> _callFunctionMock = new();
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<HttpWebResponse> _webResponse = new();
    private readonly Mock<HttpWebResponse> _validationResponse = new();
    private readonly Mock<HttpWebResponse> _sendToCohortDistributionResponse = new();
    private readonly Mock<ICheckDemographic> _checkDemographic = new();
    private readonly CreateParticipant _createParticipant = new();
    private readonly Mock<IExceptionHandler> _handleException = new();
    private readonly SetupRequest _setupRequest = new();
    private readonly Mock<IAzureQueueStorageHelper> _azureQueueStorageHelper = new();
    private readonly Mock<ICohortDistributionHandler> _cohortDistributionHandler = new Mock<ICohortDistributionHandler>();
    private readonly Mock<IOptions<AddParticipantConfig>> _config = new();
    private Mock<HttpRequestData> _request;

    public AddNewParticipantTest()
    {
        var testConfig = new AddParticipantConfig
        {
            DemographicURIGet = "DemographicURIGet",
            DSaddParticipant = "DSaddParticipant",
            DSmarkParticipantAsEligible = "DSmarkParticipantAsEligible",
            StaticValidationURL = "StaticValidationURL"
        };

        _config.Setup(c => c.Value).Returns(testConfig);

        var participantCsvRecord = new ParticipantCsvRecord
        {
            FileName = "test.csv",
            Participant = new Participant()
            {
                FirstName = "Joe",
                FamilyName = "Bloggs",
                NhsNumber = "1",
                RecordType = Actions.New
            }
        };

        var json = JsonSerializer.Serialize(participantCsvRecord);
        _request = _setupRequest.Setup(json);

        _callFunctionMock.Setup(call => call.SendPost("CohortDistributionServiceURL", It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_sendToCohortDistributionResponse.Object));

        _callFunctionMock.Setup(call => call.SendPost("StaticValidationURL", It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_validationResponse.Object));
        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), It.IsAny<string>())).Returns(It.IsAny<HttpResponseData>());
    }

    [TestMethod]
    public async Task Run_Should_log_Participant_Created()
    {
        // Arrange
        var basicParticipantCsvRecord = new BasicParticipantCsvRecord
        {
            FileName = "ExampleFileName.CSV",
            Participant = new BasicParticipantData
            {
                NhsNumber = "1234567890"
            }
        };
        var Demographic = new Demographic();
        var participantCsvRecord = new ParticipantCsvRecord
        {
            Participant = new Participant
            {
                NhsNumber = "1234567890",
                ExceptionFlag = "N"
            }
        };
        var RequestJson = JsonSerializer.Serialize(basicParticipantCsvRecord);
        var validateResonseJson = JsonSerializer.Serialize(participantCsvRecord);
        var mockRequest = MockHelpers.CreateMockHttpRequestData(RequestJson);


        _validationResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        _sendToCohortDistributionResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        _callFunctionMock.Setup(call => call.SendPost("DSaddParticipant", It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunctionMock.Setup(call => call.SendPost("DSmarkParticipantAsEligible", It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunctionMock.Setup(call => call.GetResponseText(It.IsAny<HttpWebResponse>()))
            .ReturnsAsync(validateResonseJson);
        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), "DemographicURIGet"))
            .Returns(Task.FromResult<Demographic>(new Demographic()));



        var sut = new AddParticipantFunction(
            _loggerMock.Object, 
            _callFunctionMock.Object, 
            _createResponse.Object, 
            _checkDemographic.Object, 
            _createParticipant, 
            _handleException.Object, 
            _cohortDistributionHandler.Object,
            _config.Object
        );


        // Act
        await sut.Run(JsonSerializer.Serialize(basicParticipantCsvRecord));

        // Assert
        _loggerMock.Verify(log =>
            log.Log(
            LogLevel.Information,
            0,
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.AtLeastOnce(), "Participant created");
    }

    [TestMethod]
    public async Task Run_FailedParticipantCreation_LogError()
    {
        // Arrange
        var basicParticipantCsvRecord = new BasicParticipantCsvRecord
        {
            FileName = "ExampleFileName.CSV",
            Participant = new BasicParticipantData
            {
                NhsNumber = "1234567890"
            }
        };
        var participantCsvRecord = new ParticipantCsvRecord
        {
            Participant = new Participant
            {
                NhsNumber = "1234567890",
                ExceptionFlag = "N"
            }
        };
        var requestJson = JsonSerializer.Serialize(basicParticipantCsvRecord);
        var validateResponseJson = JsonSerializer.Serialize(participantCsvRecord);

        _validationResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        var errorResponse = new Mock<HttpWebResponse>();
        errorResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.BadRequest); // Simulate error response

        _callFunctionMock.Setup(call => call.SendPost("DSaddParticipant", It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(errorResponse.Object)); // Return error response

        _callFunctionMock.Setup(call => call.GetResponseText(It.IsAny<HttpWebResponse>()))
            .ReturnsAsync(validateResponseJson);

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), "DemographicURIGet"))
            .Returns(Task.FromResult(new Demographic()));

        _handleException.Setup(x => x.CreateSystemExceptionLog(
            It.IsAny<Exception>(),
            It.IsAny<Participant>(),
            It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var sut = new AddParticipantFunction(
            _loggerMock.Object,
            _callFunctionMock.Object,
            _createResponse.Object,
            _checkDemographic.Object,
            _createParticipant,
            _handleException.Object,
            _cohortDistributionHandler.Object,
            _config.Object
        );

        // Act
        await sut.Run(JsonSerializer.Serialize(basicParticipantCsvRecord));

        // Assert
        _loggerMock.Verify(log =>
            log.Log(
                LogLevel.Error,
                0,
                It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("There was problem posting the participant to the database")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once); // Ensure the LogError is called exactly once
    }

    [TestMethod]
    public async Task Run_Should_Log_Participant_Marked_As_Eligible()
    {
        // Arrange
        _validationResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        _callFunctionMock.Setup(call => call.SendPost("DSmarkParticipantAsEligible", It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunctionMock.Setup(call => call.SendPost("DSaddParticipant", It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunctionMock.Setup(call => call.SendPost("CohortDistributionServiceURL", It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), "DemographicURIGet"))
            .Returns(Task.FromResult<Demographic>(new Demographic()));

        _callFunctionMock.Setup(x => x.GetResponseText(It.IsAny<HttpWebResponse>())).Returns(Task.FromResult<string>(
            JsonSerializer.Serialize<ValidationExceptionLog>(new ValidationExceptionLog()
            {
                IsFatal = false,
                CreatedException = false
            })));

        var sut = new AddParticipantFunction(
            _loggerMock.Object, 
            _callFunctionMock.Object, 
            _createResponse.Object, 
            _checkDemographic.Object, 
            _createParticipant, 
            _handleException.Object, 
            _cohortDistributionHandler.Object,
            _config.Object
        );

        // Act
        var basicParticipantCsvRecord = new BasicParticipantCsvRecord
        {
            FileName = "ExampleFileName.CSV",
            Participant = new BasicParticipantData
            {
                NhsNumber = "1234567890"
            }
        };
        await sut.Run(JsonSerializer.Serialize(basicParticipantCsvRecord));

        // Assert
        _loggerMock.Verify(log =>
            log.Log(
            LogLevel.Information,
            0,
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.AtLeastOnce(), "Participant created, marked as eligible");
    }
    [TestMethod]
    public async Task Run_MarkAsEligibleFails_ThrowException()
    {
        // Arrange

        var basicParticipantCsvRecord = new BasicParticipantCsvRecord
        {
            FileName = "ExampleFileName.CSV",
            Participant = new BasicParticipantData
            {
                NhsNumber = "1234567890"
            }
        };

        var participantJson = JsonSerializer.Serialize(basicParticipantCsvRecord);

        // Simulate eligibleResponse with a non-OK status code
        var eligibleResponse = new Mock<HttpWebResponse>();
        eligibleResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.BadRequest);

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        _callFunctionMock.Setup(call => call.SendPost(
                "DSmarkParticipantAsEligible",
                It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(eligibleResponse.Object));

        _callFunctionMock.Setup(call => call.SendPost("DSaddParticipant", It.IsAny<string>()))
        .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunctionMock.Setup(call => call.SendPost("CohortDistributionServiceURL", It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), "DemographicURIGet"))
            .Returns(Task.FromResult<Demographic>(new Demographic()));

        _callFunctionMock.Setup(x => x.GetResponseText(It.IsAny<HttpWebResponse>())).Returns(Task.FromResult<string>(
            JsonSerializer.Serialize<ValidationExceptionLog>(new ValidationExceptionLog()
            {
                IsFatal = false,
                CreatedException = false
            })));


        // Mock the exception handling
        _handleException.Setup(x => x.CreateSystemExceptionLog(
            It.IsAny<Exception>(),
            It.IsAny<Participant>(),
            It.IsAny<string>()
        )).Returns(Task.CompletedTask);

        var sut = new AddParticipantFunction(
            _loggerMock.Object,
            _callFunctionMock.Object,
            _createResponse.Object,
            _checkDemographic.Object,
            _createParticipant,
            _handleException.Object,
            _cohortDistributionHandler.Object,
            _config.Object
        );

        // Act
        await sut.Run(JsonSerializer.Serialize(basicParticipantCsvRecord));

        //Assert
        var invocations = _handleException.Invocations;

        Assert.IsTrue(invocations.Any(i =>
            i.Method.Name == "CreateSystemExceptionLog" &&
            i.Arguments[0] is Exception ex &&
            ex.Message.Contains("There was an error while marking participant as eligible")
        ));


        _callFunctionMock.Verify(call => call.SendPost(
            "DSmarkParticipantAsEligible",
            It.IsAny<string>()), Times.Once);


    }
    [TestMethod]
    public async Task Run_Should_Call_Create_Cohort_EndPoint_Success()
    {
        // Arrange
        _validationResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        _webResponse.Setup(x => x.StatusCode)
            .Returns(HttpStatusCode.Created);

        _sendToCohortDistributionResponse.Setup(x => x.StatusCode)
            .Returns(HttpStatusCode.OK);

        _callFunctionMock.Setup(call => call.SendPost("DSmarkParticipantAsEligible", It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunctionMock.Setup(call => call.SendPost("CohortDistributionServiceURL", It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_sendToCohortDistributionResponse.Object));

        _checkDemographic.Setup(x => x.GetDemographicAsync("DemographicURIGet", It.IsAny<string>()))
            .Returns(Task.FromResult<Demographic>(new Demographic()))
            .Verifiable();



        var sut = new AddParticipantFunction(
            _loggerMock.Object, 
            _callFunctionMock.Object, 
            _createResponse.Object, 
            _checkDemographic.Object, 
            _createParticipant, 
            _handleException.Object, 
            _cohortDistributionHandler.Object,
            _config.Object
        );

        // Act
        var basicParticipantCsvRecord = new BasicParticipantCsvRecord
        {
            FileName = "ExampleFileName.CSV",
            Participant = new BasicParticipantData
            {
                NhsNumber = "1234567890"
            }
        };
        await sut.Run(JsonSerializer.Serialize(basicParticipantCsvRecord));

        // Assert
        //_callFunctionMock.Verify(x => x.SendPost("DemographicURIGet", It.IsAny<string>()),Times.Once);
        _checkDemographic.Verify(x => x.GetDemographicAsync(It.IsAny<string>(), "DemographicURIGet"), Times.Once);
    }

    [TestMethod]
    public async Task Run_CohortDistribution_EndPointFailure()
    {
        // Arrange
        _validationResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        _webResponse.Setup(x => x.StatusCode)
            .Returns(HttpStatusCode.Created);

        _sendToCohortDistributionResponse.Setup(x => x.StatusCode)
            .Returns(HttpStatusCode.InternalServerError);

        _callFunctionMock.Setup(call => call.SendPost("DSmarkParticipantAsEligible", It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _callFunctionMock.Setup(call => call.SendPost("CohortDistributionServiceURL", It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_sendToCohortDistributionResponse.Object));

        _checkDemographic.Setup(x => x.GetDemographicAsync("DemographicURIGet", It.IsAny<string>()))
            .Returns(Task.FromResult<Demographic>(new Demographic()))
            .Verifiable();

        var sut = new AddParticipantFunction(
            _loggerMock.Object, 
            _callFunctionMock.Object, 
            _createResponse.Object, 
            _checkDemographic.Object, 
            _createParticipant, 
            _handleException.Object, 
            _cohortDistributionHandler.Object,
            _config.Object
        );

        // Act
        var basicParticipantCsvRecord = new BasicParticipantCsvRecord
        {
            FileName = "ExampleFileName.CSV",
            Participant = new BasicParticipantData
            {
                NhsNumber = "1234567890"
            }
        };
        await sut.Run(JsonSerializer.Serialize(basicParticipantCsvRecord));

        // Assert
        _checkDemographic.Verify(x => x.GetDemographicAsync(It.IsAny<string>(), "DemographicURIGet"), Times.Once);
    }

    [TestMethod]
    public async Task Run_Should_Log_Participant_Log_Error()
    {
        // Arrange
        _validationResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.Created);
        _callFunctionMock.Setup(call => call.SendPost("DSmarkParticipantAsEligible", It.IsAny<string>()));

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), "DemographicURIGet"))
            .Returns(Task.FromResult<Demographic>(new Demographic()));

        var sut = new AddParticipantFunction(
            _loggerMock.Object, 
            _callFunctionMock.Object, 
            _createResponse.Object, 
            _checkDemographic.Object, 
            _createParticipant, 
            _handleException.Object, 
            _cohortDistributionHandler.Object,
            _config.Object
        );

        // Act
        var basicParticipantCsvRecord = new BasicParticipantCsvRecord
        {
            FileName = "ExampleFileName.CSV",
            Participant = new BasicParticipantData
            {
                NhsNumber = "1234567890"
            }
        };
        await sut.Run(JsonSerializer.Serialize(basicParticipantCsvRecord));

        // Assert
        _loggerMock.Verify(log =>
            log.Log(
            LogLevel.Information,
            0,
            It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("Unable to call function")),
            It.IsAny<Exception>(),
            (Func<object, Exception, string>)It.IsAny<object>()
            ));
    }

    [TestMethod]
    public async Task Run_Should_Marked_As_Eligible_Log_Error()
    {
        // Arrange
        _validationResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.Created);
        _callFunctionMock.Setup(call => call.SendPost("DSaddParticipant", It.IsAny<string>()));

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), "DemographicURIGet"))
            .Returns(Task.FromResult<Demographic>(new Demographic()));

        var sut = new AddParticipantFunction(
            _loggerMock.Object, 
            _callFunctionMock.Object, 
            _createResponse.Object, 
            _checkDemographic.Object, 
            _createParticipant, 
            _handleException.Object, 
            _cohortDistributionHandler.Object,
            _config.Object
        );

        // Act
        var basicParticipantCsvRecord = new BasicParticipantCsvRecord
        {
            FileName = "ExampleFileName.CSV",
            Participant = new BasicParticipantData
            {
                NhsNumber = "1234567890"
            }
        };
        await sut.Run(JsonSerializer.Serialize(basicParticipantCsvRecord));

        // Assert
        _loggerMock.Verify(log =>
            log.Log(
            LogLevel.Information,
            0,
            It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("Unable to call function")),
            It.IsAny<Exception>(),
            (Func<object, Exception, string>)It.IsAny<object>()
            ));
    }

    [TestMethod]
    public async Task Run_Should_Add_Participant_With_ExceptionFlag_When_Validation_Fails()
    {
        // Arrange
        _validationResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.BadRequest);

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), "DemographicURIGet"))
            .Returns(Task.FromResult<Demographic>(new Demographic()));
        _sendToCohortDistributionResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        var sut = new AddParticipantFunction(
            _loggerMock.Object, 
            _callFunctionMock.Object, 
            _createResponse.Object, 
            _checkDemographic.Object, 
            _createParticipant, 
            _handleException.Object, 
            _cohortDistributionHandler.Object,
            _config.Object
        );

        _callFunctionMock.Setup(x => x.GetResponseText(It.IsAny<HttpWebResponse>())).Returns(Task.FromResult<string>(
            JsonSerializer.Serialize<ValidationExceptionLog>(new ValidationExceptionLog()
            {
                IsFatal = false,
                CreatedException = false
            })));
        // Act
        var basicParticipantCsvRecord = new BasicParticipantCsvRecord
        {
            FileName = "ExampleFileName.CSV",
            Participant = new BasicParticipantData
            {
                NhsNumber = "1234567890"
            }
        };
        await sut.Run(JsonSerializer.Serialize(basicParticipantCsvRecord));

        // Assert
        _callFunctionMock.Verify(call => call.SendPost("DSaddParticipant", It.IsAny<string>()), Times.Once());
    }
}
