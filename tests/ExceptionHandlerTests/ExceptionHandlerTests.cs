namespace NHS.CohortManager.Tests.ExceptionHandlerTests;

using Moq;
using Common;
using Microsoft.Extensions.Logging;
using Model;

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
}
