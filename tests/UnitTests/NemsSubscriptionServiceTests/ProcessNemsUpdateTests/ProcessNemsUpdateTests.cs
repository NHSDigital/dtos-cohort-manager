namespace NHS.CohortManager.Tests.UnitTests.ProcessNemsUpdateTests;

using NHS.Screening.ProcessNemsUpdate;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

[TestClass]
public class ProcessNemsUpdateTests
{
    private readonly Mock<ILogger<ProcessNemsUpdate>> _loggerMock = new();
    private readonly Mock<IOptions<ProcessNemsUpdateConfig>> _config = new();
    private readonly ProcessNemsUpdate _sut;

    public ProcessNemsUpdateTests()
    {
        var testConfig = new ProcessNemsUpdateConfig
        {
            RetrievePdsDemographicURL = "RetrievePdsDemographic",
            NemsMessages = "nems-messages"
        };

        _config.Setup(c => c.Value).Returns(testConfig);

        _sut = new ProcessNemsUpdate(
            _loggerMock.Object,
            _config.Object
        );
    }

    [TestMethod]
    public async Task Run_TriggerProcessNemsUpdateFunction_LogsInformation()
    {
        // Arrange
        await using var fileStream = File.OpenRead("mock-patient.json");

        // Act
        await _sut.Run(fileStream, "fileName");

        // Assert
        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("ProcessNemsUpdate function triggered.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
    }
}
