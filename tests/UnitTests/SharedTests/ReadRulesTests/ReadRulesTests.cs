namespace NHS.CohortManager.Tests.UnitTests.ReadRulesTests;

using System.Text.Json;
using Common;
using Microsoft.Extensions.Logging;
using Moq;
using RulesEngine.Models;

[TestClass]
public class ReadRulesTests
{
    private readonly Mock<ILogger<ReadRules>> _logger = new();
    private readonly ReadRules _sut;
    private readonly Workflow[] _mockRules;
    private readonly string _rulesDirectory;
    private readonly string _rulesFile;

    public ReadRulesTests()
    {
        _rulesDirectory = "RulesDirectory";
        _rulesFile = "rules.json";
        _mockRules = [
            new Workflow { WorkflowName = "Test" }
        ];
        _sut = new ReadRules(_logger.Object);

        SetupMockRulesFile();
    }

    [TestMethod]
    public async Task Run_GetRulesFromDirectoryFindsFile_ReturnsContentAsString()
    {
        // Arrange
        var expected = JsonSerializer.Serialize(_mockRules);

        // Act
        var result = await _sut.GetRulesFromDirectory($"{_rulesDirectory}/{_rulesFile}");

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public async Task Run_GetRulesFromDirectoryCannotFindFile_ReturnsEmptyString()
    {
        // Arrange
        var expected = string.Empty;

        // Act
        var result = await _sut.GetRulesFromDirectory(string.Empty);

        // Assert
        Assert.AreEqual(expected, result);
    }

    private void SetupMockRulesFile()
    {
        var configDir = Path.Combine(Environment.CurrentDirectory, _rulesDirectory);
        Directory.CreateDirectory(configDir);
        var configFilePath = Path.Combine(configDir, _rulesFile);

        File.WriteAllText(configFilePath, JsonSerializer.Serialize(_mockRules));
    }
}
