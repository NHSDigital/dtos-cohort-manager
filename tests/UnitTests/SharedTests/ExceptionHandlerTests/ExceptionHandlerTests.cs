using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Common;
using Model;
using RulesEngine.Models;
using Model.Enums;
using Common.Interfaces;
using System.Security.Cryptography.X509Certificates;

namespace NHS.CohortManager.Tests.UnitTests.ExceptionHandlerTests;

[TestClass]
public class ExceptionHandlerTests
{
    private readonly Mock<ILogger<ExceptionHandler>> _logger = new();
    private readonly Mock<IExceptionSender> _exceptionSender = new();
    private readonly ExceptionHandler _function;

    public ExceptionHandlerTests()
    {
        Environment.SetEnvironmentVariable("ExceptionFunctionURL", "ExceptionFunctionURL");
        _function = new ExceptionHandler(_logger.Object, _exceptionSender.Object);
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
        var participant = new Participant() { NhsNumber = NhsNumber, Source = "filename" };

        _exceptionSender.Setup(x => x.sendToCreateException(It.IsAny<ValidationException>()))
                  .Returns(Task.FromResult(true))
                  .Verifiable();

        // Act
        await _function.CreateSystemExceptionLog(new Exception("Test exception"), participant);

        // Assert - when NhsNumber is not null (even if empty or whitespace), ExceptionFlag should be set to "Y"
        _exceptionSender.Verify(call => call.sendToCreateException(
            It.IsAny<ValidationException>()), Times.Once());
    }

    [TestMethod]
    [DataRow(null)]
    public async Task CreateSystemExceptionLog_IsCalledWithParticipantWithNullNhsNumber_SetsExceptionFlagToNull(string NhsNumber)
    {
        // Arrange
        var participant = new Participant() { NhsNumber = NhsNumber, Source = "filename" };

        _exceptionSender.Setup(x => x.sendToCreateException(It.IsAny<ValidationException>()))
                     .Returns(Task.FromResult(true))
                     .Verifiable();

        // Act
        await _function.CreateSystemExceptionLog(new Exception("Test exception"), participant);

        // Assert - when NhsNumber is null, the JSON should show ExceptionFlag as null
        _exceptionSender.Verify(call => call.sendToCreateException(
           It.IsAny<ValidationException>()), Times.Once());
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

        var participant = new Participant() { ParticipantId = participantId, NhsNumber = participantId, Source = "filename" };
        IEnumerable<RuleResultTree> validationErrors = new List<RuleResultTree>()
        {
            GenerateSampleRuleResultTree(CreateSampleRule())
        };

        _exceptionSender.Setup(x => x.sendToCreateException(It.IsAny<ValidationException>()))
                         .Returns(Task.FromResult(true))
                         .Verifiable();
        // Act
        var result = await _function.CreateValidationExceptionLog(validationErrors, participant);

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
        _exceptionSender.Setup(x => x.sendToCreateException(It.IsAny<ValidationException>()))
                 .Returns(Task.FromResult(true))
                 .Verifiable();
        // Act
        await _function.CreateSystemExceptionLogFromNhsNumber(exception, nhsNumber, fileName, screeningName, errorRecord);

        // Assert - if the input is considered nil, the payload should include Category 7; otherwise it should not.
        if (nhsNumberIsNil)
        {
            _exceptionSender.Verify(call => call.sendToCreateException(
                It.Is<ValidationException>(s => s.Category == 7)), Times.Once()
                );
        }
        else
        {
            _exceptionSender.Verify(call => call.sendToCreateException(
                 It.Is<ValidationException>(s => s.Category != 7)
                 ), Times.Once());

        }
    }

    [TestMethod]
    [DataRow("ScreeningTest")]
    [DataRow("")]
    public async Task CreateSystemExceptionLog_CalledWithBasicParticipantData_SuccessWithNhsNumber(string? screeningName)
    {
        // Arrange
        var basicParticipant = new BasicParticipantData { NhsNumber = "987654321", ScreeningName = screeningName };
        var exception = new Exception("Test exception");
        var fileName = "testfile";
        _exceptionSender.Setup(x => x.sendToCreateException(It.IsAny<ValidationException>()))
                  .Returns(Task.FromResult(true))
                  .Verifiable();

        // Act
        await _function.CreateSystemExceptionLog(exception, basicParticipant, fileName);

        // Assert - verify that the JSON includes the provided NhsNumber and ScreeningName
        _exceptionSender.Verify(call => call.sendToCreateException(
               It.Is<ValidationException>(s => s.NhsNumber == "987654321" && s.ScreeningName == screeningName)
               ), Times.Once());
    }

    [TestMethod]
    public async Task CreateSystemExceptionLog_BasicParticipantDataOverload_Success_WithNullNhsNumber()
    {
        // Arrange
        var basicParticipant = new BasicParticipantData { NhsNumber = null, ScreeningName = "ScreeningTest" };
        var exception = new Exception("Test exception");
        var fileName = "testfile";
        _exceptionSender.Setup(x => x.sendToCreateException(It.IsAny<ValidationException>()))
                   .Returns(Task.FromResult(true))
                   .Verifiable();

        // Act
        await _function.CreateSystemExceptionLog(exception, basicParticipant, fileName);

        // Assert - check that the default (empty) NhsNumber is used
        _exceptionSender.Verify(call => call.sendToCreateException(
              It.Is<ValidationException>(s => s.NhsNumber == "")
              ), Times.Once());
    }

    [TestMethod]
    public async Task CreateDeletedRecordException_CalledWithCsvRecord_SuccessNoError()
    {
        // Arrange
        var participantCsvRecord = new BasicParticipantCsvRecord
        {
            FileName = "file.csv",
            BasicParticipantData = new BasicParticipantData { NhsNumber = "123456789", ScreeningName = "ScreeningTest" }
        };
        _exceptionSender.Setup(x => x.sendToCreateException(It.IsAny<ValidationException>()))
                   .Returns(Task.FromResult(true))
                   .Verifiable();

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
            BasicParticipantData = new BasicParticipantData { NhsNumber = "123456789", ScreeningName = "ScreeningTest" }
        };
        _exceptionSender.Setup(x => x.sendToCreateException(It.IsAny<ValidationException>()))
                         .Returns(Task.FromResult(false))
                         .Verifiable();

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
            BasicParticipantData = new BasicParticipantData { NhsNumber = "123456789", ScreeningName = "ScreeningTest" }
        };
        string description = "Schema error occurred";
        _exceptionSender.Setup(x => x.sendToCreateException(It.IsAny<ValidationException>()))
                   .Returns(Task.FromResult(true))
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

        // Assert - check that the default (empty) NhsNumber is used
        _exceptionSender.Verify(call => call.sendToCreateException(
              It.Is<ValidationException>(
                s => s.NhsNumber == "123456789" &&
                s.ScreeningName == "ScreeningTest" &&
                s.RuleDescription == description
              )), Times.Once());
    }

    [TestMethod]
    public async Task CreateSchemaValidationException_FailureResponse_LogsError()
    {
        // Arrange
        var participantCsvRecord = new BasicParticipantCsvRecord
        {
            FileName = "file.csv",
            BasicParticipantData = new BasicParticipantData { NhsNumber = "123456789", ScreeningName = "ScreeningTest" }
        };
        string description = "Schema error occurred";
        _exceptionSender.Setup(x => x.sendToCreateException(It.IsAny<ValidationException>()))
                   .Returns(Task.FromResult(false))
                   .Verifiable();

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
        _exceptionSender.Setup(x => x.sendToCreateException(It.IsAny<ValidationException>()))
                    .Returns(Task.FromResult(true))
                    .Verifiable();

        // Act
        await _function.CreateTransformationExceptionLog(transformationErrors, participant);

        // Assert - verify that SendPost is called for each error and no error is logged
        _exceptionSender.Verify(call => call.sendToCreateException(
             It.Is<ValidationException>(
               s => s.NhsNumber == "123456789" &&
               s.ScreeningName == "ScreeningTest"
             )),
             Times.Exactly(transformationErrors.Count));

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

        _exceptionSender.Setup(x => x.sendToCreateException(It.IsAny<ValidationException>()))
               .Returns(Task.FromResult(false))
               .Verifiable();

        // Act
        await _function.CreateTransformationExceptionLog(transformationErrors, participant);

        // Assert - verify that an error is logged for each failed call
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("There was an error while logging an exception to the database")),
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
        _exceptionSender.Setup(x => x.sendToCreateException(It.IsAny<ValidationException>()))
                .Returns(Task.FromResult(true))
                .Verifiable();
        // Act
        await _function.CreateTransformExecutedExceptions(cohortDistributionParticipant, ruleName, 1);

        // Assert - verify that SendPost is called for each error and no error is logged

        _exceptionSender.Verify(call => call.sendToCreateException(
        It.Is<ValidationException>(
            s => s.RuleDescription.Contains("Participant was transformed as transform rule") &&
            s.RuleId == 1 &&
            s.Category == (int)ExceptionCategory.TransformExecuted
        )), Times.Once);

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
        _exceptionSender.Setup(x => x.sendToCreateException(It.IsAny<ValidationException>()))
                .Returns(Task.FromResult(false))
                .Verifiable();
        // Act
        await _function.CreateTransformExecutedExceptions(cohortDistributionParticipant, "51.Message.0", 1);

        // Assert - verify that an error is logged for each failed call
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("There was an error while logging an exception to the database.")),
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
        _exceptionSender.Setup(x => x.sendToCreateException(It.IsAny<ValidationException>()))
                .Returns(Task.FromResult(true))
                .Verifiable();

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
        // We parse the rule "ValidationExceptionLog" enum so that it enters the database correctly 
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
        _exceptionSender.Setup(x => x.sendToCreateException(It.IsAny<ValidationException>()))
                .Returns(Task.FromResult(true))
                .Verifiable();

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
        // We parse the rule "ValidationExceptionLog" enum so that it enters the database correctly 
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
        _exceptionSender.Setup(x => x.sendToCreateException(It.IsAny<ValidationException>()))
               .Returns(Task.FromResult(true))
               .Verifiable();
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
        // We parse the rule "ValidationExceptionLog" enum so that it enters the database correctly 
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
        _exceptionSender.Setup(x => x.sendToCreateException(It.IsAny<ValidationException>()))
               .Returns(Task.FromResult(false))
               .Verifiable();

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
        _exceptionSender.Setup(x => x.sendToCreateException(It.IsAny<ValidationException>()))
               .Returns(Task.FromResult(true))
               .Verifiable();

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
        _exceptionSender.Setup(x => x.sendToCreateException(It.IsAny<ValidationException>()))
               .Returns(Task.FromResult(false))
               .Verifiable();
        // Act
        var result = await _function.CreateRecordValidationExceptionLog(nhsNumber, fileName, errorDescription, screeningName, errorRecord);

        // Assert
        Assert.IsFalse(result);
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("There was an error while logging an exception to the database.")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once());
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

}
