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

[TestClass]
public class DurableDemographicTests
{
    private readonly Mock<ICreateDemographicData> CreateDemographicData = new();

    private readonly DurableDemographicFunction _function;

    private readonly Mock<ILogger<DurableDemographicFunction>> _logger = new();

    public DurableDemographicTests()
    {
        Environment.SetEnvironmentVariable("ExceptionFunctionURL", "ExceptionFunctionURL");
        _function = new DurableDemographicFunction(CreateDemographicData.Object, _logger.Object);
    }


    [TestMethod]
    public async Task Run_return_RunOrchestrator_True()
    {
        // Arrange
        var mockCreateDemographicData = new Mock<ICreateDemographicData>();
        var function = new DurableDemographicFunction(mockCreateDemographicData.Object, _logger.Object);

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
        var mockCreateDemographicData = new Mock<ICreateDemographicData>();
        var function = new DurableDemographicFunction(mockCreateDemographicData.Object, _logger.Object);
        var mockLogger = new Mock<ILogger>();

        var demographicJsonData = "[{\"NhsNumber\": \"111111\", \"FirstName\": \"Test\"}]";

        mockCreateDemographicData.Setup(x => x.InsertDemographicData(It.IsAny<List<Demographic>>())).Returns(Task.FromResult(true));

        // Act
        var result = await function.InsertDemographicData(demographicJsonData, CreateMockFunctionContext());

        // Assert
        Assert.IsTrue(result);
        mockCreateDemographicData.Verify(x => x.InsertDemographicData(It.IsAny<List<Demographic>>()), Times.Once);
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


/*
public async Task Run_DemographicDataSaved_OK()
{
    // Arrange
    var json = JsonSerializer.Serialize(_participant);
    var sut = new DemographicDataService(_logger.Object, _createResponse.Object, _createDemographicData.Object, _exceptionHandler.Object);

    SetupRequest(json);

    _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), ""))
        .Returns((HttpStatusCode statusCode, HttpRequestData req, string ResponseBody) =>
        {
            var response = req.CreateResponse(statusCode);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            return response;
        });
    _request.Setup(x => x.Method).Returns("POST");
    _createDemographicData.Setup(x => x.InsertDemographicData(It.IsAny<List<Demographic>>())).Returns(Task.FromResult(true));

    // Act
    var result = await sut.Run(_request.Object);

    // Assert
    Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
}*/


