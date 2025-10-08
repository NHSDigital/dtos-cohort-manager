namespace NHS.CohortManager.Tests.UnitTests.DurableDemographicTests;

using Microsoft.Extensions.Logging;
using Model;
using NHS.CohortManager.DemographicServices;
using Moq;
using Microsoft.DurableTask;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using DataServices.Client;
using System.Collections.Generic;
using System.Text.Json;
using Common;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.DependencyInjection;
using NHS.CohortManager.Tests.TestUtils;
using System.Net;
using System.Text;
using Microsoft.Extensions.Options;

[TestClass]
public class DurableDemographicTests
{
    private readonly Mock<IDataServiceClient<ParticipantDemographic>> _participantDemographic = new();
    private readonly DurableDemographicFunction _function;
    private readonly Mock<ILogger<DurableDemographicFunction>> _logger = new();
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly ServiceCollection _serviceCollection = new();
    private readonly ServiceProvider serviceProvider;
    private Mock<HttpRequestData> mockHttpRequest;
    private readonly Mock<FunctionContext> mockFunctionContext;
    private readonly SetupRequest _setupRequest = new();
    private DemographicDurableFunctionConfig demographicDurableFunctionConfig;
    private Mock<IOptions<DemographicDurableFunctionConfig>> _demographicDurableFunctionConfig = new();

    public DurableDemographicTests()
    {
        Environment.SetEnvironmentVariable("ExceptionFunctionURL", "ExceptionFunctionURL");
        demographicDurableFunctionConfig = new DemographicDurableFunctionConfig
        {
            DemographicDataServiceURL = "DemographicDataServiceURL",
            MaxRetryCount = 3
        };
        _demographicDurableFunctionConfig.Setup(c => c.Value).Returns(demographicDurableFunctionConfig);

        _function = new DurableDemographicFunction(_participantDemographic.Object, _logger.Object, _createResponse.Object, _demographicDurableFunctionConfig.Object);

        serviceProvider = _serviceCollection.BuildServiceProvider();
        mockFunctionContext = CreateMockFunctionContext();
        mockFunctionContext.SetupProperty(c => c.InstanceServices, serviceProvider);

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string ResponseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                response.WriteString(ResponseBody);
                return response;
            });
    }




    [TestMethod]
    public async Task RunOrchestrator_ValidInput_InsertsData()
    {
        // Arrange
        var function = new DurableDemographicFunction(_participantDemographic.Object, _logger.Object, _createResponse.Object, _demographicDurableFunctionConfig.Object);

        var mockContext = new Mock<TaskOrchestrationContext>();
        var logger = Mock.Of<ILogger>();
        mockContext.Setup(ctx => ctx.CreateReplaySafeLogger(It.IsAny<string>())).Returns(logger);
        mockContext.Setup(ctx => ctx.GetInput<string>()).Returns("[{\"NhsNumber\": \"111111\", \"FirstName\": \"Test\"}]");

        mockContext
            .Setup(ctx => ctx.CallActivityAsync<bool>(nameof(function.InsertDemographicData), It.IsAny<string>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync(true);

        // Act
        await function.RunOrchestrator(mockContext.Object);

        // Assert
        mockContext.Verify(ctx => ctx.CallActivityAsync<bool>(nameof(function.InsertDemographicData), It.IsAny<string>(), It.IsAny<TaskOptions>()), Times.Once);
        _logger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Warning),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Retrying batch after failure. Current Retry count")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
        Times.Never);
    }


    [TestMethod]
    public async Task InsertDemographicData_DataInsertedSuccessfully_ReturnsTrue()
    {
        // Arrange
        var Participants = new List<ParticipantDemographic>();
        var function = new DurableDemographicFunction(_participantDemographic.Object, _logger.Object, _createResponse.Object, _demographicDurableFunctionConfig.Object);
        var mockLogger = new Mock<ILogger>();

        var demographicJsonData = JsonSerializer.Serialize(Participants);
        _participantDemographic.Setup(x => x.AddRange(It.IsAny<IEnumerable<ParticipantDemographic>>())).ReturnsAsync(true);

        // Act
        await function.InsertDemographicData(demographicJsonData, CreateMockFunctionContext().Object);

        // Assert
        _logger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
             It.IsAny<EventId>(),
             It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("InsertDemographicData function has successfully completed")),
             It.IsAny<Exception>(),
             It.IsAny<Func<It.IsAnyType, Exception, string>>()),
         Times.Once);
    }



    [TestMethod]
    [ExpectedException(typeof(Exception),
    "some new exception")]
    public async Task InsertDemographicData_DataInsertionFails_ReturnsFalseAndLogsError()
    {
        // Arrange
        var Participants = new List<ParticipantDemographic>();
        var function = new DurableDemographicFunction(_participantDemographic.Object, _logger.Object, _createResponse.Object, _demographicDurableFunctionConfig.Object);
        var mockLogger = new Mock<ILogger>();

        var demographicJsonData = JsonSerializer.Serialize(Participants);
        _participantDemographic.Setup(x => x.AddRange(It.IsAny<IEnumerable<ParticipantDemographic>>()))
            .ThrowsAsync(new Exception("some new exception"));

        // Act

        await function.InsertDemographicData(demographicJsonData, CreateMockFunctionContext().Object);

        // Assert
        _logger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
              It.IsAny<EventId>(),
              It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Inserting demographic data failed")),
              It.IsAny<Exception>(),
              It.IsAny<Func<It.IsAnyType, Exception, string>>()),
          Times.Once);
    }

    [TestMethod]
    public async Task HttpStart_InvalidInput_ReturnsInternalServerError()
    {
        // Define constants
        const string functionName = "DurableDemographicFunction";
        const string instanceId = "7E467BDB-213F-407A-B86A-1954053D3C24";

        var loggerMock = new Mock<ILogger>();
        var clientMock = new Mock<DurableTaskClient>(MockBehavior.Default, new object[] { "test" });

        var json = JsonSerializer.Serialize(new BasicParticipantCsvRecord());

        mockHttpRequest = _setupRequest.Setup(json);

        // Mock StartNewAsync method
        clientMock.
            Setup(x => x.ScheduleNewOrchestrationInstanceAsync(functionName, It.IsAny<string>(), CancellationToken.None)).
            ReturnsAsync(instanceId);

        var function = new DurableDemographicFunction(_participantDemographic.Object, _logger.Object, _createResponse.Object, _demographicDurableFunctionConfig.Object);

        // Call Orchestration trigger function
        var result = await function.HttpStart(
            mockHttpRequest.Object,
            clientMock.Object,
            mockFunctionContext.Object);

        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }


    [TestMethod]
    public async Task GetOrchestrationStatus_ValidRequest_ReturnOrchestrationStatus()
    {
        // Arrange
        var _mockClient = new Mock<DurableTaskClient>(MockBehavior.Default, new object[] { "test" });
        var function = new DurableDemographicFunction(_participantDemographic.Object, _logger.Object, _createResponse.Object, _demographicDurableFunctionConfig.Object);

        var instanceId = "test-instance";
        var request = _setupRequest.Setup(JsonSerializer.Serialize(instanceId));
        var OrchestrationMetadata = new OrchestrationMetadata("test-instance", instanceId);

        _mockClient.Setup(x => x.GetInstanceAsync(instanceId, default)).ReturnsAsync(OrchestrationMetadata);

        // Act
        var result = await _function.GetOrchestrationStatus(request.Object, _mockClient.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _mockClient.Verify(c => c.GetInstanceAsync(instanceId, default), Times.Once);
    }

    [TestMethod]
    public async Task GetOrchestrationStatus_InstanceIdIsEmpty_LogsWarning()
    {
        // Arrange
        var _mockClient = new Mock<DurableTaskClient>(MockBehavior.Default, new object[] { "test" });
        var request = _setupRequest.Setup(JsonSerializer.Serialize(""));

        // Act
        var result = await _function.GetOrchestrationStatus(request.Object, _mockClient.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

        _logger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Warning),
              It.IsAny<EventId>(),
              It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"instance found")),
              It.IsAny<Exception>(),
              It.IsAny<Func<It.IsAnyType, Exception, string>>()),
          Times.Once);
    }

    private static Mock<FunctionContext> CreateMockFunctionContext()
    {
        var context = new Mock<FunctionContext>();
        return context;
    }

}



