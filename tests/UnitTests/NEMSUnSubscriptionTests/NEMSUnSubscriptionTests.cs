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
using Common;
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
    private Mock<NHS.CohortManager.NEMSUnSubscription.NEMSUnSubscription.IExceptionHandler> _exceptionHandlerMock;
    private Mock<ILogger<NHS.CohortManager.NEMSUnSubscription.NEMSUnSubscription>> _loggerMock;
    private FunctionContext _context;
    private UnsubscriptionRequest _mockRequest;
    private HttpResponseData _response;
    private HttpRequestData _request;
    public interface IExceptionHandler
    {
        Task<HttpResponseData> HandleAsync(HttpRequestData req, HttpStatusCode code, string message);
    }

    [TestInitialize]
    public void Setup()
    {
        _tableClientMock = new Mock<TableClient>();
        _loggerMock = new Mock<ILogger<NHS.CohortManager.NEMSUnSubscription.NEMSUnSubscription>>();
        _context = CreateMockFunctionContext(_loggerMock.Object);

        _mockRequest = new UnsubscriptionRequest { NhsNumber = "1234567890" };
        string json = JsonSerializer.Serialize(_mockRequest);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        _response = new MockNEMSHttpResponseData(_context, HttpStatusCode.OK);
        _request = new MockNEMSHttpRequestData(_context, bodyStream, _response);

        _exceptionHandlerMock = new Mock<NHS.CohortManager.NEMSUnSubscription.NEMSUnSubscription.IExceptionHandler>();
        _exceptionHandlerMock
            .Setup(x => x.HandleAsync(It.IsAny<HttpRequestData>(), HttpStatusCode.NotFound, It.IsAny<string>()))
            .ReturnsAsync((HttpRequestData req, HttpStatusCode code, string msg) =>
            {
                var response = req.CreateResponse(code);
                response.WriteString(msg);
                return response;
            });

        var loggerMock = new Mock<ILogger<NHS.CohortManager.NEMSUnSubscription.NEMSUnSubscription>>();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

        var func = new TestableNEMSUnSubscription(
            loggerMock.Object,
            httpClientFactoryMock.Object,
            _exceptionHandlerMock.Object,
            new Mock<ICreateResponse>().Object,
            new Mock<ICallFunction>().Object
        )
        {
            TestLookupResult = null
};
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

        var func = new TestableNEMSUnSubscription(
        _loggerMock.Object,
        new Mock<IHttpClientFactory>().Object,
        _exceptionHandlerMock.Object,
        new Mock<ICreateResponse>().Object,
        new Mock<ICallFunction>().Object
        );

        var result = await func.Run(emptyRequest, _context);

        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

   [TestMethod]
    public async Task Run_ReturnsNotFound_WhenSubscriptionIdIsNull()
    {
        var func = new TestableNEMSUnSubscription(
        _loggerMock.Object,
        new Mock<IHttpClientFactory>().Object,
        _exceptionHandlerMock.Object,
        new Mock<ICreateResponse>().Object,
        new Mock<ICallFunction>().Object
        )
        {
            TestLookupResult = null,
            ExceptionHandler = _exceptionHandlerMock.Object
        };

        var result = await func.Run(_request, _context);

        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
    }


    [TestMethod]
    public async Task Run_ReturnsOk_WhenUnsubscribedSuccessfully()
    {
        var func = new TestableNEMSUnSubscription(
        _loggerMock.Object,
        new Mock<IHttpClientFactory>().Object,
        _exceptionHandlerMock.Object,
        new Mock<ICreateResponse>().Object,
        new Mock<ICallFunction>().Object
        )
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
    public IExceptionHandler? ExceptionHandler { get; set; }

    protected override Task<HttpResponseData> HandleNotFoundAsync(HttpRequestData req, string message)
    {
        return ExceptionHandler!.HandleAsync(req, HttpStatusCode.NotFound, message);
    }



    public TestableNEMSUnSubscription(
            ILogger<NHS.CohortManager.NEMSUnSubscription.NEMSUnSubscription> logger,
            IHttpClientFactory httpClientFactory,
            IExceptionHandler handleException,
            ICreateResponse createResponse,
            ICallFunction callFunction)
            : base(logger, httpClientFactory, handleException, createResponse, callFunction)
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

   protected override Task<bool> DeleteSubscriptionFromTableAsync(string nhsNumber)
    {
        return Task.FromResult(TestNemsDeleteResult);
    }
}
