using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Common;
using Model;
using RulesEngine.Models;
using NHS.CohortManager.Tests.TestUtils;
using Model.Enums;

namespace NHS.CohortManager.Tests.UnitTests.ExceptionHandlerTests;
[TestClass]
public class ExceptionHandlerTests
{
    private readonly Mock<ILogger<ExceptionHandler>> _logger = new();
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly ExceptionHandler _function;

    public ExceptionHandlerTests()
    {
        Environment.SetEnvironmentVariable("ExceptionFunctionURL", "ExceptionFunctionURL");
        _function = new ExceptionHandler(_logger.Object, _callFunction.Object);
    }

    [TestMethod]
    [DataRow("123456789")]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("0")]
    [DataRow("0000000000")]
    [DataRow("N/A")]
    public async Task CreateSystemExceptionLog_IsCalledWithParticipantExceptionAndFileName_Success(string NhsNumber)
    {
        // Arrange
        var participant = new Participant() { NhsNumber = NhsNumber };

        // Act
        await _function.CreateSystemExceptionLog(new Exception("Test exception"), participant, "filename");

        // Assert - when NhsNumber is not null (even if empty or whitespace), ExceptionFlag should be set to "Y"
        var exceptionFlagY = "\\u0022ExceptionFlag\\u0022:\\u0022Y\\u0022";
        _callFunction.Verify(call => call.SendPost(
            It.Is<string>(s => s == "ExceptionFunctionURL"),
            It.Is<string>(v => v.Contains(exceptionFlagY))), Times.Once());
    }

    [TestMethod]
    [DataRow(null)]
    public async Task CreateSystemExceptionLog_IsCalledWithParticipantWithNullNhsNumber_SetsExceptionFlagToNull(string NhsNumber)
    {
        // Arrange
        var participant = new Participant() { NhsNumber = NhsNumber };

        // Act
        await _function.CreateSystemExceptionLog(new Exception("Test exception"), participant, "filename");

        // Assert - when NhsNumber is null, the JSON should show ExceptionFlag as null
        var exceptionFlagNull = "\\u0022ExceptionFlag\\u0022:null";
        _callFunction.Verify(call => call.SendPost(
            It.Is<string>(s => s == "ExceptionFunctionURL"),
            It.Is<string>(v => v.Contains(exceptionFlagNull))), Times.Once());
    }

    [TestMethod]
    [DataRow("123456789")]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow(null)]
    [DataRow("0000000000")]
    [DataRow("0")]
    [DataRow("foo")]
    [DataRow("abc123")]
    [DataRow("9999999999")]
    [DataRow("12345678901234567890")]
    public async Task CreateValidationExceptionLog_IsCalledWithAllFatalErrorsAndParticipant_LogsFatalMessage(string participantId)
    {
        // Arrange
        var participantCsvRecord = new ParticipantCsvRecord()
        {
            Participant = new Participant() { ParticipantId = participantId, NhsNumber = participantId }
        };
        IEnumerable<RuleResultTree> validationErrors = new List<RuleResultTree>()
        {
            GenerateSampleRuleResultTree(CreateSampleRule())
        };

        var response = MockHelpers.CreateMockHttpResponseData(HttpStatusCode.OK, "[]");

        _callFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.FromResult(response))
            .Verifiable();

        // Act
        var result = await _function.CreateValidationExceptionLog(validationErrors, participantCsvRecord);

        // Build the expected log message.
        var expectedId = participantId ?? "(null)";
        var expectedMessage = $"A Fatal rule has been found and the record with NHD ID: {expectedId} will not be added to the database.";

        // Assert - fatal rule should be detected and a log entry created
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedMessage)),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
        Assert.IsTrue(result.IsFatal);
    }

    [TestMethod]
    [DataRow("0", true)]
    [DataRow("0000000000", true)]
    [DataRow("123456789", false)]
    [DataRow(null, false)]
    [DataRow("", false)]
    [DataRow("   ", false)]
    [DataRow("0 ", false)]          // trailing space – not exactly "0"
    [DataRow(" 0000000000", false)] // leading space – not exactly "0000000000"
    [DataRow("abc", false)]
    public async Task CreateSystemExceptionLogFromNhsNumber_IsCalledWithExceptionNhsNumberFileNameScreeningNameAndErrorRecord_NilNhsNumberReturnsCategory7InPayload(string nhsNumber, bool nhsNumberIsNil)
    {
        // Arrange
        var exception = new Exception("Test exception");
        string fileName = "testfile";
        string screeningName = "ScreeningTest";
        string errorRecord = "{}";
        string capturedPayload = null;
        var response = MockHelpers.CreateMockHttpResponseData(HttpStatusCode.OK, "{}");
        _callFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((url, payload) => capturedPayload = payload)
            .ReturnsAsync(response);

        // Act
        await _function.CreateSystemExceptionLogFromNhsNumber(exception, nhsNumber, fileName, screeningName, errorRecord);

        // Assert - if the input is considered nil, the payload should include Category 7; otherwise it should not.
        if (nhsNumberIsNil)
        {
            _callFunction.Verify(call => call.SendPost(
                It.Is<string>(s => s == "ExceptionFunctionURL"),
                It.Is<string>(v => v.Contains("\"Category\":7"))), Times.Once());
            Assert.IsTrue(capturedPayload.Contains("\"Category\":7"), "Expected payload to contain Category 7 for nil NhsNumber");
        }
        else
        {
            _callFunction.Verify(call => call.SendPost(
                It.Is<string>(s => s == "ExceptionFunctionURL"),
                It.Is<string>(v => !v.Contains("\"Category\":7"))), Times.Once());
            Assert.IsFalse(capturedPayload.Contains("\"Category\":7"), "Expected payload NOT to contain Category 7 for non-nil NhsNumber");
        }
    }

    [TestMethod]
    [DataRow("ScreeningTest")]
    [DataRow(null)]
    public async Task CreateSystemExceptionLog_CalledWithBasicParticipantData_SuccessWithNhsNumber(string? screeningName)
    {
        // Arrange
        var basicParticipant = new BasicParticipantData { NhsNumber = "987654321", ScreeningName = screeningName };
        var exception = new Exception("Test exception");
        var fileName = "testfile";
        var response = MockHelpers.CreateMockHttpResponseData(HttpStatusCode.OK, "{}");
        _callFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync(response);

        // Act 
        await _function.CreateSystemExceptionLog(exception, basicParticipant, fileName);

        // Assert - verify that the JSON includes the provided NhsNumber and ScreeningName
        _callFunction.Verify(x => x.SendPost(
            It.Is<string>(s => s == "ExceptionFunctionURL"),
            It.Is<string>(s => s.Contains("\"NhsNumber\":\"987654321\"") && s.Contains($"\"ScreeningName\":\"{screeningName}\""))),
            Times.Once());
    }

    [TestMethod]
    public async Task CreateSystemExceptionLog_BasicParticipantDataOverload_Success_WithNullNhsNumber()
    {
        // Arrange
        var basicParticipant = new BasicParticipantData { NhsNumber = null, ScreeningName = "ScreeningTest" };
        var exception = new Exception("Test exception");
        var fileName = "testfile";
        var response = MockHelpers.CreateMockHttpResponseData(HttpStatusCode.OK, "{}");
        _callFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync(response);

        // Act
        await _function.CreateSystemExceptionLog(exception, basicParticipant, fileName);

        // Assert - check that the default (empty) NhsNumber is used
        _callFunction.Verify(x => x.SendPost(
            It.Is<string>(s => s == "ExceptionFunctionURL"),
            It.Is<string>(s => s.Contains("\"NhsNumber\":\"\""))),
            Times.Once());
    }

    [TestMethod]
    public async Task CreateDeletedRecordException_CalledWithCsvRecord_SuccessNoError()
    {
        // Arrange
        var participantCsvRecord = new BasicParticipantCsvRecord
        {
            FileName = "file.csv",
            Participant = new BasicParticipantData { NhsNumber = "123456789", ScreeningName = "ScreeningTest" }
        };
        var response = MockHelpers.CreateMockHttpResponseData(HttpStatusCode.OK, "{}");
        _callFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync(response);

        // Act
        await _function.CreateDeletedRecordException(participantCsvRecord);

        // Assert - no error should be logged when the response is OK
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never());
    }

    [TestMethod]
    public async Task CreateDeletedRecordException_CalledWithCsvRecord_FailureLogsError()
    {
        // Arrange
        var participantCsvRecord = new BasicParticipantCsvRecord
        {
            FileName = "file.csv",
            Participant = new BasicParticipantData { NhsNumber = "123456789", ScreeningName = "ScreeningTest" }
        };
        var response = MockHelpers.CreateMockHttpResponseData(HttpStatusCode.InternalServerError, "{}");
        _callFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync(response);

        // Act
        await _function.CreateDeletedRecordException(participantCsvRecord);

        // Assert - an error log should be written on failure
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("There was an error while logging an exception to the database.")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once());
    }

    [TestMethod]
    public async Task CreateSchemaValidationException_CalledWithCsvRecordAndDescription_SuccessNoErrorCorrectPayload()
    {
        // Arrange
        var participantCsvRecord = new BasicParticipantCsvRecord
        {
            FileName = "file.csv",
            Participant = new BasicParticipantData { NhsNumber = "123456789", ScreeningName = "ScreeningTest" }
        };
        string description = "Schema error occurred";
        var response = MockHelpers.CreateMockHttpResponseData(HttpStatusCode.OK, "{}");
        _callFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync(response)
                     .Verifiable();

        // Act
        await _function.CreateSchemaValidationException(participantCsvRecord, description);

        // Assert - no error log should be generated for a successful call
        // and POST contains NHS Number, screening name and rule description
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never());
        _callFunction.Verify(x => x.SendPost(
                It.Is<string>(s => s == "ExceptionFunctionURL"),
                It.Is<string>(s => s.Contains("\"NhsNumber\":\"123456789\"") &&
                                   s.Contains("\"ScreeningName\":\"ScreeningTest\"") &&
                                   s.Contains($"\"RuleDescription\":\"{description}\""))
            ), Times.Once);
    }

    [TestMethod]
    public async Task CreateSchemaValidationException_FailureResponse_LogsError()
    {
        // Arrange
        var participantCsvRecord = new BasicParticipantCsvRecord
        {
            FileName = "file.csv",
            Participant = new BasicParticipantData { NhsNumber = "123456789", ScreeningName = "ScreeningTest" }
        };
        string description = "Schema error occurred";
        var response = MockHelpers.CreateMockHttpResponseData(HttpStatusCode.InternalServerError, "{}");
        _callFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync(response);

        // Act
        await _function.CreateSchemaValidationException(participantCsvRecord, description);

        // Assert - an error log should be written for a failure response
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("There was an error while logging an exception to the database.")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once());
    }

    [TestMethod]
    public async Task CreateTransformationExceptionLog_CalledWithTransformationErrors_EndpointCalledNoError()
    {
        // Arrange
        var rule = CreateSampleRule();
        var ruleResult = GenerateSampleRuleResultTree(rule);
        var rule2 = CreateSampleRule();
        var ruleResult2 = GenerateSampleRuleResultTree(rule2);
        var transformationErrors = new List<RuleResultTree>
        {
            ruleResult,
            ruleResult2
        };
        var participant = new CohortDistributionParticipant { NhsNumber = "123456789", ScreeningName = "ScreeningTest" };
        var response = MockHelpers.CreateMockHttpResponseData(HttpStatusCode.OK, "{}");
        _callFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync(response);

        // Act
        await _function.CreateTransformationExceptionLog(transformationErrors, participant);

        // Assert - verify that SendPost is called for each error and no error is logged
        _callFunction.Verify(x => x.SendPost(
            It.Is<string>(s => s == "ExceptionFunctionURL"),
            It.Is<string>(s => s.Contains("\"NhsNumber\":\"123456789\"") &&
                                s.Contains("\"ScreeningName\":\"ScreeningTest\"")
        )), Times.Exactly(transformationErrors.Count));
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never());
    }

    [TestMethod]
    public async Task CreateTransformationExceptionLog_FailureResponse_LogsError()
    {
        // Arrange
        var rule = CreateSampleRule();
        var ruleResult = GenerateSampleRuleResultTree(rule);
        var transformationErrors = new List<RuleResultTree> { ruleResult };
        var participant = new CohortDistributionParticipant { NhsNumber = "123456789", ScreeningName = "ScreeningTest" };
        var response = MockHelpers.CreateMockHttpResponseData(HttpStatusCode.InternalServerError, "{}");
        _callFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync(response);

        // Act
        await _function.CreateTransformationExceptionLog(transformationErrors, participant);

        // Assert - verify that an error is logged for each failed call
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("There was an error while logging a transformation exception to the database")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(transformationErrors.Count));
    }

    [TestMethod]
    public async Task CreateTransformExecutedExceptions_CalledWithCohortDistributionParticipantAndRule_NoErrorCorrectPayloadSent()
    {
        // Arrange
        var cohortDistributionParticipant = new CohortDistributionParticipant
        {
            ParticipantId = "PID123",
            NhsNumber = "123456789",
            ScreeningName = "ScreeningTest"
        };
        string ruleName = "51.Message.0";
        var response = MockHelpers.CreateMockHttpResponseData(HttpStatusCode.OK, "{}");
        _callFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync(response)
                     .Verifiable();

        // Act
        await _function.CreateTransformExecutedExceptions(cohortDistributionParticipant, ruleName, 1);

        // Assert - verify that SendPost is called for each error and no error is logged
        _callFunction.Verify(x => x.SendPost(
                It.Is<string>(s => s == "ExceptionFunctionURL"),
                It.Is<string>(s => s.Contains($"\"RuleDescription\":\"Participant was transformed as transform rule: {ruleName} was executed\"") &&
                                   s.Contains("\"RuleId\":1") &&
                                   s.Contains($"\"Category\":{(int)ExceptionCategory.TransformExecuted}"))
            ), Times.Once);
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never());
    }

    [TestMethod]
    public async Task CreateTransformExecutedExceptions_RequestFailure_LogsError()
    {
        // Arrange
        var cohortDistributionParticipant = new CohortDistributionParticipant
        {
            ParticipantId = "PID123",
            NhsNumber = "123456789",
            ScreeningName = "ScreeningTest"
        };
        var response = MockHelpers.CreateMockHttpResponseData(HttpStatusCode.BadRequest, "{}");
        _callFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync(response);

        // Act
        await _function.CreateTransformExecutedExceptions(cohortDistributionParticipant, "51.Message.0", 1);

        // Assert - verify that an error is logged for each failed call
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("There was an error while logging a transformation exception to the database")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task CreateValidationExceptionLog_CalledWithFatalRuleAndNoExceptionMessage_SuccessDefaultMessageLogged()
    {
        // Arrange
        var rule = CreateSampleRule(); // rule.RuleName is "1.Message.1" so fatal
        var ruleResult = GenerateSampleRuleResultTree(rule);
        ruleResult.ActionResult.Output = "TestError";
        ruleResult.ExceptionMessage = "";
        var validationErrors = new List<RuleResultTree> { ruleResult };
        var participantCsvRecord = new ParticipantCsvRecord
        {
            FileName = "file.csv",
            Participant = new Participant { ParticipantId = "PID123", NhsNumber = "123456789", ScreeningName = "ScreeningTest" }
        };
        var response = MockHelpers.CreateMockHttpResponseData(HttpStatusCode.Created, "{}");
        _callFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync(response);

        // Act
        var result = await _function.CreateValidationExceptionLog(validationErrors, participantCsvRecord);

        var expectedMessage = $"A Fatal rule has been found and the record with NHD ID: PID123 will not be added to the database.";

        // Assert - fatal rule should be detected and logged
        Assert.IsTrue(result.IsFatal);
        Assert.IsTrue(result.CreatedException);
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedMessage)),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once());
    }

    [TestMethod]
    public async Task CreateValidationExceptionLog_NonFatalRuleNoExceptionMessage_SuccessDefaultMessageLogged()
    {
        // Arrange
        var rule = CreateSampleRule();
        rule.RuleName = "51.Message.0.0"; // non-fatal rule
        var ruleResult = GenerateSampleRuleResultTree(rule);
        ruleResult.ActionResult.Output = "NonFatalError";
        ruleResult.ExceptionMessage = "";
        var validationErrors = new List<RuleResultTree> { ruleResult };
        var participantCsvRecord = new ParticipantCsvRecord
        {
            FileName = "file.csv",
            Participant = new Participant { ParticipantId = "PID123", NhsNumber = "123456789", ScreeningName = "ScreeningTest" }
        };
        var response = MockHelpers.CreateMockHttpResponseData(HttpStatusCode.Created, "{}");
        _callFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync(response);

        var notExpectedMessage = "an exception was raised while running the rules. Exception Message:";
        var notExpectedError = "There was an error while logging an exception to the database";

        // Act
        var result = await _function.CreateValidationExceptionLog(validationErrors, participantCsvRecord);

        // Assert - non-fatal rule should not trigger a fatal log message
        Assert.IsFalse(result.IsFatal);
        Assert.IsTrue(result.CreatedException);
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never());
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(notExpectedMessage)),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never());
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(notExpectedError)),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never());
    }


    [TestMethod]
    public async Task CreateValidationExceptionLog_ExceptionMessageSupplied_OverridesLogOutputErrorMessage()
    {
        // Arrange
        var rule = CreateSampleRule();
        rule.RuleName = "1.Message.1.1";
        var ruleResult = GenerateSampleRuleResultTree(rule);
        ruleResult.ActionResult.Output = "TestError";
        ruleResult.ExceptionMessage = "Actual exception occurred";
        var validationErrors = new List<RuleResultTree> { ruleResult };
        var participantCsvRecord = new ParticipantCsvRecord
        {
            FileName = "file.csv",
            Participant = new Participant { ParticipantId = "PID123", NhsNumber = "123456789", ScreeningName = "ScreeningTest" }
        };
        var response = MockHelpers.CreateMockHttpResponseData(HttpStatusCode.OK, "{}");
        _callFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync(response);

        // Act
        var result = await _function.CreateValidationExceptionLog(validationErrors, participantCsvRecord);

        // Assert - the exception message should override the output and be logged as an error
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Actual exception occurred")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once());
    }

    [TestMethod]
    public async Task CreateValidationExceptionLog_RequestFailure_ReturnsCreatedExceptionFalseAndSpecificErrorLog()
    {
        // Arrange
        var rule = CreateSampleRule();
        rule.RuleName = "1.Message.1.1";
        var ruleResult = GenerateSampleRuleResultTree(rule);
        ruleResult.ActionResult.Output = "TestError";
        ruleResult.ExceptionMessage = "";
        var validationErrors = new List<RuleResultTree> { ruleResult };
        var participantCsvRecord = new ParticipantCsvRecord
        {
            FileName = "file.csv",
            Participant = new Participant { ParticipantId = "PID123", NhsNumber = "123456789", ScreeningName = "ScreeningTest" }
        };
        var response = MockHelpers.CreateMockHttpResponseData(HttpStatusCode.BadRequest, "{}");
        _callFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync(response);

        // Act
        var result = await _function.CreateValidationExceptionLog(validationErrors, participantCsvRecord);

        // Assert - a failure response should yield CreatedException = false and log an error
        Assert.IsFalse(result.CreatedException);
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("There was an error while logging an exception to the database")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once());
    }

    [TestMethod]
    public async Task CreateRecordValidationExceptionLog_OkResponse_ReturnsTrue()
    {
        // Arrange
        string nhsNumber = "123456789";
        string fileName = "file.csv";
        string errorDescription = "Error description";
        string screeningName = "ScreeningTest";
        string errorRecord = "{}";
        var response = MockHelpers.CreateMockHttpResponseData(HttpStatusCode.OK, "{}");
        _callFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync(response);

        // Act
        var result = await _function.CreateRecordValidationExceptionLog(nhsNumber, fileName, errorDescription, screeningName, errorRecord);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task CreateRecordValidationExceptionLog_RequestFailure_LogsErrorReturnsFalse()
    {
        // Arrange
        string nhsNumber = "123456789";
        string fileName = "file.csv";
        string errorDescription = "Error description";
        string screeningName = "ScreeningTest";
        string errorRecord = "{}";
        var response = MockHelpers.CreateMockHttpResponseData(HttpStatusCode.InternalServerError, "{}");
        _callFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync(response);

        // Act
        var result = await _function.CreateRecordValidationExceptionLog(nhsNumber, fileName, errorDescription, screeningName, errorRecord);

        // Assert
        Assert.IsFalse(result);
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("There was an error while logging an exception to the database")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once());
    }

    [TestMethod]
    public void ExceptionHandlerConstructor_EnvironmentVariableNotSet_ThrowsException()
    {
        // Arrange - unset the environment variable temporarily
        Environment.SetEnvironmentVariable("ExceptionFunctionURL", null);

        // Act & Assert - should throw an InvalidOperationException
        Assert.ThrowsException<InvalidOperationException>(() =>
            new ExceptionHandler(_logger.Object, _callFunction.Object));

        // Reset the environment variable for other tests
        Environment.SetEnvironmentVariable("ExceptionFunctionURL", "ExceptionFunctionURL");
    }

    // -------------------- Helper Methods --------------------

    private Rule CreateSampleRule()
    {
        var rule = new Rule
        {
            RuleName = "1.Message.1.1",
            Properties = new Dictionary<string, object>
            {
                { "Property1", "Value1" },
                { "Property2", 42 }
            },
            Operator = ">",
            ErrorMessage = "Sample error message",
            Enabled = true,
            RuleExpressionType = RuleExpressionType.LambdaExpression,
            WorkflowsToInject = new[] { "Workflow1", "Workflow2" },
            LocalParams = new List<ScopedParam> {
                new ScopedParam {
                    Name = "param1",
                    Expression = "someExpression"
                }
            }
        };

        rule.Actions = new RuleActions();

        return rule;
    }

    private static RuleResultTree GenerateSampleRuleResultTree(Rule rule)
    {
        var resultTree = new RuleResultTree
        {
            Rule = rule,
            IsSuccess = true,
            Inputs = new Dictionary<string, object>(),
            ActionResult = new ActionResult(),
            ExceptionMessage = string.Empty
        };

        var childResults = new List<RuleResultTree>
        {
            new RuleResultTree
            {
                Rule = rule,
                IsSuccess = true,
                Inputs = new Dictionary<string, object>(),
                ActionResult = new ActionResult(),
                ExceptionMessage = string.Empty
            }
        };

        resultTree.ChildResults = childResults;

        return resultTree;
    }
}
