namespace NHS.CohortManager.DemographicServices.NEMSUnSubscription;

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
using System.IO;
using System.Threading.Tasks;
using NHS.Screening.NEMSUnSubscription;
using Microsoft.Extensions.Options;
using TestUtils;

[TestClass]
public class NEMSUnSubscriptionTests
{
    private Mock<ILogger<NEMSUnSubscription>> _loggerMock;
    private FunctionContext _context;
    private UnsubscriptionRequest _mockRequest;
    private HttpResponseData _response;
    private HttpRequestData _request;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<NEMSUnSubscription>>();
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
        var emptyRequest = new MockNEMSHttpRequestData(
            _context,
            new MemoryStream(),
            new MockNEMSHttpResponseData(_context, HttpStatusCode.BadRequest));

        var func = CreateFunction();

        var result = await func.Run(emptyRequest, _context);

        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ReturnsNotFound_WhenSubscriptionIdIsNull()
    {
        var nemsSubscriptionServiceMock = new Mock<INemsSubscriptionService>();
        nemsSubscriptionServiceMock
            .Setup(s => s.LookupSubscriptionIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string?)null);

        var func = CreateFunction(nemsSubscriptionServiceMock.Object);

        var result = await func.Run(_request, _context);

        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ReturnsOk_WhenUnsubscribedSuccessfully()
    {
        var nemsSubscriptionServiceMock = new Mock<INemsSubscriptionService>();
        nemsSubscriptionServiceMock.Setup(s => s.LookupSubscriptionIdAsync(It.IsAny<string>())).ReturnsAsync("abc-123");
        nemsSubscriptionServiceMock.Setup(s => s.DeleteSubscriptionFromNems("abc-123")).ReturnsAsync(true);
        nemsSubscriptionServiceMock.Setup(s => s.DeleteSubscriptionFromTableAsync(It.IsAny<string>())).ReturnsAsync(true);

        var func = CreateFunction(nemsSubscriptionServiceMock.Object);

        var result = await func.Run(_request, _context);

        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

 private NEMSUnSubscription CreateFunction(INemsSubscriptionService? nemsService = null)
{
    var optionsMock = new Mock<IOptions<NEMSUnSubscriptionConfig>>();
    var httpClientFactoryMock = new Mock<IHttpClientFactory>();
    httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

    return new NEMSUnSubscription(
        _loggerMock.Object,
        httpClientFactoryMock.Object,
        new Mock<IExceptionHandler>().Object, // 👈 add this
        new CreateResponse(),
        optionsMock.Object,
        new Mock<ICallFunction>().Object,
        nemsService ?? new Mock<INemsSubscriptionService>().Object
    );
}
}
