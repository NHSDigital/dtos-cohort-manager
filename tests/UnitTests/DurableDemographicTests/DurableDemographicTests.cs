namespace NHS.CohortManager.Tests.UnitTests.DurableDemographicTests;

using Microsoft.Extensions.Logging;
using Model;
using NHS.CohortManager.DemographicServices;
using Moq;
using Data.Database;
using Microsoft.DurableTask;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using System.Text;
using DataServices.Client;
using System.Collections.Generic;
using System.Text.Json;
using Common;
using System.Net;
using Microsoft.DurableTask.Client;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;

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


    public DurableDemographicTests()
    {
        Environment.SetEnvironmentVariable("ExceptionFunctionURL", "ExceptionFunctionURL");
        _function = new DurableDemographicFunction(_participantDemographic.Object, _logger.Object, _createResponse.Object);

        serviceProvider = _serviceCollection.BuildServiceProvider();
        mockFunctionContext = CreateMockFunctionContext();
        mockFunctionContext.SetupProperty(c => c.InstanceServices, serviceProvider);
    }


    [TestMethod]
    public async Task RunOrchestrator_ValidInput_ReturnsTrue()
    {
        // Arrange
        var function = new DurableDemographicFunction(_participantDemographic.Object, _logger.Object, _createResponse.Object);

        var mockContext = new Mock<TaskOrchestrationContext>();
        var logger = Mock.Of<ILogger>();
        mockContext.Setup(ctx => ctx.CreateReplaySafeLogger(It.IsAny<string>())).Returns(logger);
        mockContext.Setup(ctx => ctx.GetInput<string>()).Returns("[{\"NhsNumber\": \"111111\", \"FirstName\": \"Test\"}]");

        mockContext
            .Setup(ctx => ctx.CallActivityAsync<bool>(nameof(function.InsertDemographicData), It.IsAny<string>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync(true);

        // Act
        var result = await function.RunOrchestrator(mockContext.Object);

        // Assert
        Assert.IsTrue(result);
        mockContext.Verify(ctx => ctx.CallActivityAsync<bool>(nameof(function.InsertDemographicData), It.IsAny<string>(), It.IsAny<TaskOptions>()), Times.Once);
    }


    [TestMethod]
    public async Task InsertDemographicData_DataInsertedSuccessfully_ReturnsTrue()
    {
        // Arrange
        var Participants = new List<ParticipantDemographic>();
        var function = new DurableDemographicFunction(_participantDemographic.Object, _logger.Object, _createResponse.Object);
        var mockLogger = new Mock<ILogger>();

        var demographicJsonData = JsonSerializer.Serialize(Participants);
        _participantDemographic.Setup(x => x.AddRange(It.IsAny<IEnumerable<ParticipantDemographic>>())).ReturnsAsync(true);

        // Act
        var result = await function.InsertDemographicData(demographicJsonData, CreateMockFunctionContext().Object);

        // Assert
        Assert.IsTrue(result);
    }


    [TestMethod]
    public async Task InsertDemographicData_DataInsertionFails_ReturnsFalseAndLogsError()
    {
        // Arrange
        var Participants = new List<ParticipantDemographic>();
        var function = new DurableDemographicFunction(_participantDemographic.Object, _logger.Object, _createResponse.Object);
        var mockLogger = new Mock<ILogger>();

        var demographicJsonData = JsonSerializer.Serialize(Participants);
        _participantDemographic.Setup(x => x.AddRange(It.IsAny<IEnumerable<ParticipantDemographic>>()))
            .ThrowsAsync(new Exception("some new exception"));

        // Act
        var result = await function.InsertDemographicData(demographicJsonData, CreateMockFunctionContext().Object);

        // Assert
        Assert.IsFalse(result);

        _logger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
              It.IsAny<EventId>(),
              It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Inserting demographic data failed")),
              It.IsAny<Exception>(),
              It.IsAny<Func<It.IsAnyType, Exception, string>>()),
          Times.Once);
    }

    [TestMethod]
    public async Task HttpStart_InvalidInput_ReturnsNull()
    {
        // Define constants
        const string functionName = "DurableDemographicFunction";
        const string instanceId = "7E467BDB-213F-407A-B86A-1954053D3C24";

        var loggerMock = new Mock<ILogger>();
        var clientMock = new Mock<DurableTaskClient>(MockBehavior.Default, new object[] { "test" });

        var json = JsonSerializer.Serialize(new BasicParticipantCsvRecord());

        var HttpRequestData = SetupRequest(json);

        // Mock StartNewAsync method
        clientMock.
            Setup(x => x.ScheduleNewOrchestrationInstanceAsync(functionName, It.IsAny<string>(), CancellationToken.None)).
            ReturnsAsync(instanceId);

        var function = new DurableDemographicFunction(_participantDemographic.Object, _logger.Object, _createResponse.Object);

        // Call Orchestration trigger function
        var result = await function.HttpStart(
            mockHttpRequest.Object,
            clientMock.Object,
            mockFunctionContext.Object);

        Assert.IsNull(result);
    }

    public Mock<HttpRequestData> SetupRequest(string json)
    {
        var byteArray = Encoding.ASCII.GetBytes(json);
        var bodyStream = new MemoryStream(byteArray);

        mockHttpRequest = new Mock<HttpRequestData>(mockFunctionContext.Object);
        mockHttpRequest.Setup(s => s.Body).Returns(bodyStream);
        mockHttpRequest.Setup(s => s.CreateResponse()).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(mockFunctionContext.Object);
            response.SetupProperty(s => s.Headers, new HttpHeadersCollection());
            response.SetupProperty(s => s.StatusCode, HttpStatusCode.Accepted);
            response.SetupProperty(s => s.Body, new MemoryStream());
            return response.Object;
        });

        return mockHttpRequest;
    }

    private static Mock<FunctionContext> CreateMockFunctionContext()
    {
        var context = new Mock<FunctionContext>();
        return context;
    }

}



