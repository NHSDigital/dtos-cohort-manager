namespace NHS.CohortManager.Tests.UnitTests.CheckDemographicTests;

using Common;
using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Model;
using System.Text.Json;
using System.Threading.Tasks;
using NHS.Screening.ReceiveCaasFile;
using Microsoft.Extensions.Options;

[TestClass]
public class CallDurableDemographicFuncTests
{
    private readonly Mock<ILogger<CallDurableDemographicFunc>> _logger = new();
    private readonly Mock<IHttpClientFunction> _httpClientFunction = new();
    private readonly CallDurableDemographicFunc _callDurableDemographicFunc;
    private readonly Mock<ICopyFailedBatchToBlob> _copyFailedBatchToBlob = new();
    private readonly Mock<IOptions<ReceiveCaasFileConfig>> _config = new();

    public CallDurableDemographicFuncTests()
    {
        var testConfig = new ReceiveCaasFileConfig
        {
            GetOrchestrationStatusURL = "http://testURL.com",
            maxNumberOfChecks = 50
        };

        _config.Setup(c => c.Value).Returns(testConfig);
        _callDurableDemographicFunc = new CallDurableDemographicFunc(_httpClientFunction.Object, _logger.Object, _copyFailedBatchToBlob.Object, _config.Object);
    }

    [TestMethod]
    public async Task PostDemographicDataAsync_NoParticipants_ReturnTrue()
    {
        // Arrange
        var participants = new List<ParticipantDemographic>();
        var uri = "test-uri.com/post";

        // Act
        var result = await _callDurableDemographicFunc.PostDemographicDataAsync(participants, uri);

        // Assert
        Assert.IsTrue(result);
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("There were no items to to send to the demographic durable function")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once());
    }

    [TestMethod]
    public async Task PostDemographicDataAsync_ParticipantsExist_ReturnTrue()
    {
        // Arrange
        var participants = new List<ParticipantDemographic> { new ParticipantDemographic() };
        var uri = "test-uri.com/post";

        _httpClientFunction.Setup(x => x.SendGet(It.IsAny<string>()))
            .Returns(Task.FromResult("status"))
            .Verifiable();

        // Act
        var result = await _callDurableDemographicFunc.PostDemographicDataAsync(participants, uri);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task GetStatus_ValidResponse_ReturnWorkflowStatus()
    {
        // Arrange
        var uri = "http://test-uri.com/get-status";
        var participants = new List<ParticipantDemographic> { new ParticipantDemographic() };

        _httpClientFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        _httpClientFunction.Setup(x => x.SendGet(It.IsAny<string>()))
            .ReturnsAsync(JsonSerializer.Serialize(new WebhookResponse { RuntimeStatus = "Completed" }));

        // Act
        var result = await _callDurableDemographicFunc.PostDemographicDataAsync(participants, uri);

        // Assert
        Assert.IsTrue(result);
        _logger.Verify(x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Durable function completed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once());
    }

    [TestMethod]
    public async Task GetStatus_ResponseError_UnknownStatus()
    {
        // Arrange
        var uri = "http://test-uri.com/get-status";
        var participants = new List<ParticipantDemographic> { new ParticipantDemographic() };

        _httpClientFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Simulated exception"));

        _httpClientFunction.Setup(x => x.SendGet(It.IsAny<string>()))
            .ReturnsAsync(JsonSerializer.Serialize(new WebhookResponse { RuntimeStatus = "Completed" }));

        // Act
        var result = await _callDurableDemographicFunc.PostDemographicDataAsync(participants, uri);

        // Assert
        Assert.IsTrue(result);
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("still sending records to queue")
                && v.ToString().Contains("Simulated exception")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        Times.Once());

    }

    // Missing test coverage for exception state - due to the retry logic and private const _maxNumberOfChecks it would add 150 seconds to test run
    // Tried to use reflection to manually change value but can't as it's const.
    // Recommend to refactor CheckDemographic.cs to make it more testable.
}
