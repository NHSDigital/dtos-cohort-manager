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

[TestClass]
public class DurableDemographicTests
{
    private readonly Mock<IDataServiceClient<ParticipantDemographic>> _participantDemographic = new();

    private readonly DurableDemographicFunction _function;

    private readonly Mock<ILogger<DurableDemographicFunction>> _logger = new();

    private readonly Mock<ICreateResponse> _createResponse = new();

    public DurableDemographicTests()
    {
        Environment.SetEnvironmentVariable("ExceptionFunctionURL", "ExceptionFunctionURL");
        _function = new DurableDemographicFunction(_participantDemographic.Object, _logger.Object, _createResponse.Object);
    }


    [TestMethod]
    public async Task Run_return_RunOrchestrator_True()
    {
        // Arrange
        var mockCreateDemographicData = new Mock<ICreateDemographicData>();
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
    public async Task InsertDemographicData_Should_Return_True_When_Data_Is_Inserted_Successfully()
    {
        // Arrange
        var Participants = new List<ParticipantDemographic>();
        var function = new DurableDemographicFunction(_participantDemographic.Object, _logger.Object, _createResponse.Object);
        var mockLogger = new Mock<ILogger>();

        var demographicJsonData = JsonSerializer.Serialize(Participants);
        _participantDemographic.Setup(x => x.AddRange(It.IsAny<IEnumerable<ParticipantDemographic>>())).ReturnsAsync(true);

        // Act
        var result = await function.InsertDemographicData(demographicJsonData, CreateMockFunctionContext());

        // Assert
        Assert.IsTrue(result);
    }

    private static HttpRequestData CreateHttpRequest(string body)
    {
        var context = new Mock<FunctionContext>();
        var request = new Mock<HttpRequestData>(context.Object);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(body));
        request.Setup(req => req.Body).Returns(stream);
        request.Setup(req => req.CreateResponse()).Returns(new Mock<HttpResponseData>(context.Object).Object);
        return request.Object;
    }

    [TestMethod]
    private static FunctionContext CreateMockFunctionContext()
    {
        var context = new Mock<FunctionContext>();
        // var loggerMock = new Mock<ILogger>();
        return context.Object;
    }
}



