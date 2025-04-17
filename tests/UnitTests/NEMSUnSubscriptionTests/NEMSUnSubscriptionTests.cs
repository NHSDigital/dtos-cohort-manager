using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Net;
using System.Text;
using System.Text.Json;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;
using NHS.CohortManager.NEMSUnSubscription;
using System.IO;
using System.Threading.Tasks;
using TestUtils;
using Azure;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Linq;


[TestClass]
public class NEMSUnSubscriptionTests
{
    private Mock<TableClient> _tableClientMock;
    private Mock<ILogger> _loggerMock;
    private FunctionContext _context;
    private UnsubscriptionRequest _mockRequest;
    private HttpResponseData _response;
    private HttpRequestData _request;

    [TestInitialize]
    public void Setup()
    {
        _tableClientMock = new Mock<TableClient>();
        _loggerMock = new Mock<ILogger>();
        _context = CreateMockFunctionContext(_loggerMock.Object);

        _mockRequest = new UnsubscriptionRequest { NhsNumber = "1234567890" };
        string json = JsonSerializer.Serialize(_mockRequest);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        _response = new MockNEMSHttpResponseData(_context, HttpStatusCode.OK);
        _request = new MockNEMSHttpRequestData(_context, bodyStream, _response);
    }

    private FunctionContext CreateMockFunctionContext(ILogger logger)
    {
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(logger);

        var services = new Mock<IServiceProvider>();
        services.Setup(x => x.GetService(typeof(ILoggerFactory))).Returns(loggerFactory.Object);

        var context = new Mock<FunctionContext>();
        context.Setup(x => x.InstanceServices).Returns(services.Object);
        return context.Object;
    }

    [TestMethod]
    public async Task Run_ReturnsBadRequest_WhenRequestIsEmpty()
    {
        var emptyRequest = new MockNEMSHttpRequestData(_context, new MemoryStream(), new MockNEMSHttpResponseData(_context, HttpStatusCode.BadRequest));
        var func = new TestableNEMSUnSubscription(_tableClientMock.Object, new HttpClient());

        var result = await func.Run(emptyRequest, _context);

        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ReturnsNotFound_WhenSubscriptionIdIsNull()
    {
        var func = new TestableNEMSUnSubscription(_tableClientMock.Object, new HttpClient())
        {
            TestLookupResult = null
        };

        var result = await func.Run(_request, _context);

        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ReturnsBadGateway_WhenDeleteFails()
    {
        var func = new TestableNEMSUnSubscription(_tableClientMock.Object, new HttpClient())
        {
            TestLookupResult = "abc-123",
            TestNemsDeleteResult = false
        };

        var result = await func.Run(_request, _context);

        Assert.AreEqual(HttpStatusCode.BadGateway, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ReturnsOk_WhenUnsubscribedSuccessfully()
    {
        var func = new TestableNEMSUnSubscription(_tableClientMock.Object, new HttpClient())
        {
            TestLookupResult = "abc-123",
            TestNemsDeleteResult = true
        };

        var result = await func.Run(_request, _context);

        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

}

public class TestableNEMSUnSubscription : NHS.CohortManager.NEMSUnSubscription.NEMSUnSubscription
{
    public string? TestLookupResult { get; set; }
    public bool TestNemsDeleteResult { get; set; }

    public TestableNEMSUnSubscription(TableClient tableClient, HttpClient httpClient)
        : base(tableClient, httpClient)
    {
    }

    protected override async Task<string?> LookupSubscriptionIdAsync(string nhsNumber)
    {
        return await Task.FromResult(TestLookupResult);
    }

    protected override async Task<bool> DeleteSubscriptionFromNems(string subscriptionId)
    {
        return await Task.FromResult(TestNemsDeleteResult);
    }

    protected override async Task DeleteSubscriptionFromTableAsync(string nhsNumber)
    {
        await Task.CompletedTask;
    }
}
