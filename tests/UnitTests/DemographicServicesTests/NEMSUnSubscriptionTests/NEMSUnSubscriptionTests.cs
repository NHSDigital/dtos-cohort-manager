namespace NHS.CohortManager.DemographicServices.NEMSUnSubscription;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;
using Model;
using Common;
using System.Threading.Tasks;
using NHS.CohortManager.Tests.TestUtils;

[TestClass]
public class NEMSUnSubscriptionTests : DatabaseTestBaseSetup<NEMSUnSubscription>
{
    private Mock<INemsSubscriptionService> _nemsSubscriptionServiceMock;
    private Mock<ICreateResponse> _createResponseMock;
    private HttpRequestData _request;
    private HttpResponseData _response;

    private static IHttpClientFactory CreateHttpClientFactory()
    {
        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(new HttpClient());
        return mockFactory.Object;
    }

    private NEMSUnSubscription CreateFunction(INemsSubscriptionService nemsService)
    {
        return new NEMSUnSubscription(
            _loggerMock.Object,
            CreateHttpClientFactory(),
            _createResponseMock.Object,
            nemsService
        );
    }

    public NEMSUnSubscriptionTests() : base(
        (connection, logger, transaction, command, createResponse) =>
            new NEMSUnSubscription(
                logger,
                CreateHttpClientFactory(),
                createResponse,
                new Mock<INemsSubscriptionService>().Object
            )
    )
    { }


    [TestInitialize]
    public void Setup()
    {
        _nemsSubscriptionServiceMock = new Mock<INemsSubscriptionService>();
        _createResponseMock = CreateHttpResponseMock();

        var requestBody = JsonSerializer.Serialize(new UnsubscriptionRequest { NhsNumber = "1234567890" });
        _request = SetupRequest(requestBody).Object;
    }

    [TestMethod]
    public async Task Run_ReturnsBadRequest_WhenRequestIsEmpty()
    {
        var request = SetupRequest(string.Empty).Object;

        var func = CreateFunction(_nemsSubscriptionServiceMock.Object);

        var result = await func.Run(request);

        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ReturnsNotFound_WhenSubscriptionIdIsNull()
    {
        _nemsSubscriptionServiceMock.Setup(s => s.LookupSubscriptionIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string?)null);

        var func = CreateFunction(_nemsSubscriptionServiceMock.Object);

        var result = await func.Run(_request);

        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ReturnsOk_WhenUnsubscribedSuccessfully()
    {
        _nemsSubscriptionServiceMock.Setup(s => s.LookupSubscriptionIdAsync(It.IsAny<string>()))
            .ReturnsAsync("abc-123");

        _nemsSubscriptionServiceMock.Setup(s => s.DeleteSubscriptionFromNems("abc-123"))
            .ReturnsAsync(true);

        _nemsSubscriptionServiceMock.Setup(s => s.DeleteSubscriptionFromTableAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        var func = CreateFunction(_nemsSubscriptionServiceMock.Object);

        var result = await func.Run(_request);

        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }
}
