namespace NHS.CohortManager.Tests.UnitTests.ExceptionHandlerTests;

using Moq;
using Common;
using Microsoft.Extensions.Logging;
using Model;
using RulesEngine.Models;
using NHS.CohortManager.Tests.TestUtils;
using System.Net;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Common.Interfaces;
using Data.Database;
using DataServices.Client;
using System.Linq.Expressions;

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
    public async Task Run_CreateSystemExceptionLog_IsCalled_WithParticipant_Success(string NhsNumber)
    {
        // Arrange
        var participant = new Participant() { NhsNumber = NhsNumber };

        // Act
        await _function.CreateSystemExceptionLog(new Exception(), participant, "filename");

        // Assert
        var exceptionFlagY = "\\u0022ExceptionFlag\\u0022:\\u0022Y\\u0022";
        _callFunction.Verify(call => call.SendPost(It.Is<string>(s => s == "ExceptionFunctionURL"), It.Is<string>(v => v.Contains(exceptionFlagY))), Times.Once());
    }

    [TestMethod]
    [DataRow(null)]
    public async Task Run_CreateSystemExceptionLog_IsCalled_WithParticipant_NullNhsNumber_SetsExceptionFlagToNull(string NhsNumber)
    {
        // Arrange
        var participant = new Participant() { NhsNumber = NhsNumber };

        // Act
        await _function.CreateSystemExceptionLog(new Exception(), participant, "filename");

        // Assert
        var exceptionFlagNull = "\\u0022ExceptionFlag\\u0022:null";
        _callFunction.Verify(call => call.SendPost(It.Is<string>(s => s == "ExceptionFunctionURL"), It.Is<string>(v => v.Contains(exceptionFlagNull))), Times.Once());
    }

// TODO - why is exeception flag not set for all overloads?

// ----- TODO : Add tests for public methods:
// public async Task CreateSystemExceptionLog(Exception exception, BasicParticipantData participant, string fileName)
// public async Task CreateSystemExceptionLogFromNhsNumber(Exception exception, string nhsNumber, string fileName, string screeningName, string errorRecord)
// public async Task CreateDeletedRecordException(BasicParticipantCsvRecord participantCsvRecord)
//  public async Task CreateSchemaValidationException(BasicParticipantCsvRecord participantCsvRecord, string description)
// public async Task CreateTransformationExceptionLog(IEnumerable<RuleResultTree> transformationErrors, CohortDistributionParticipant participant)

    [TestMethod]
    [DataRow("123456789")]
    [DataRow("0000000000")]
    [DataRow("0")]
    [DataRow("foo")]
    public async Task Run_CreateValidationExceptionLog_Success_AllFatal_WithMessage(string NhsNumber)
    {
        //Arrange
        var participantCsvRecord = new ParticipantCsvRecord() { Participant = new Participant() { ParticipantId = NhsNumber } };
        IEnumerable<RuleResultTree> validationErrors = new List<RuleResultTree>() {GenerateSampleRuleResultTree(CreateSampleRule())};

        var response = MockHelpers.CreateMockHttpResponseData(HttpStatusCode.OK, "[]");

        _callFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.FromResult(response))
            .Verifiable();

        //Act
        var result = await _function.CreateValidationExceptionLog(validationErrors, participantCsvRecord);

        // Assert
        _logger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"A Fatal rule has been found and the record with NHD ID: {NhsNumber} will not be added to the database.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        Times.Once);
        Assert.IsTrue(result.IsFatal);
    }
    

    [TestMethod]
    [DataRow("0000000000")]
    [DataRow("0")]
    public async Task Run_SystemExceptionWithNilReturnFile_IsCalledWithCategory7Exception(string nilReturnFileNhsNumber)
    {
        // Arrange
        var participant = new Participant() { NhsNumber = nilReturnFileNhsNumber };

        // Act
        await _function.CreateSystemExceptionLog(new Exception(), participant, "filename");

        // Assert
        var expectedCategory = "\"Category\":7";
        _callFunction.Verify(call => call.SendPost(It.Is<string>(s => s == "ExceptionFunctionURL"), It.Is<string>(v => v.Contains(expectedCategory))), Times.Once());
    }

    private Rule CreateSampleRule()
    {
        var rule = new Rule
        {
            RuleName = "1.Message.1",
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
                    Expression = "someExpression" }
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
            IsSuccess = true, // or false based on your test scenario
            Inputs = new Dictionary<string, object>(),
            ActionResult = new ActionResult(), // Initialize with appropriate values
            ExceptionMessage = string.Empty // Set to an error message if needed
        };

        var childResults = new List<RuleResultTree>
        {
            // Add child RuleResultTree instances as needed
            new RuleResultTree
            {
                Rule = rule,
                IsSuccess = true, // or false based on your test scenario
                Inputs = new Dictionary<string, object>(),
                ActionResult = new ActionResult(), // Initialize with appropriate values
                ExceptionMessage = string.Empty // Set to an error message if needed
            }
        };

        resultTree.ChildResults = childResults;

        return resultTree;
    }
}
