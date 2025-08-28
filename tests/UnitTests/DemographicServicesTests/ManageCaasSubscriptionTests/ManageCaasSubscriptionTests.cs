namespace NHS.CohortManager.Tests.UnitTests.DemographicServicesTests;

using System.Net;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Common;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NHS.CohortManager.DemographicServices;
using NHS.CohortManager.Tests.TestUtils;
using System.Net.Http;

[TestClass]
public class ManageCaasSubscriptionTests
{
    private readonly CreateResponse _createResponse = new();
    private readonly Mock<ILogger<ManageCaasSubscription>> _logger = new();
    private readonly SetupRequest _setupRequest = new();
    private readonly ManageCaasSubscription _sut;
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly Mock<IOptions<ManageCaasSubscriptionConfig>> _config = new();
    private readonly Mock<IMeshSendCaasSubscribe> _mesh = new();

    public ManageCaasSubscriptionTests()
    {
        _config.Setup(x => x.Value).Returns(new ManageCaasSubscriptionConfig
        {
            ManageNemsSubscriptionDataServiceURL = null, // keep stub mode during unit tests
            CaasToMailbox = "TEST_TO",
            CaasFromMailbox = "TEST_FROM"
        });

        _mesh
            .Setup(m => m.SendSubscriptionRequest(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("STUB_MSG_ID");

        _sut = new ManageCaasSubscription(
            _logger.Object,
            _createResponse,
            _httpClientFactory.Object,
            _config.Object,
            _mesh.Object
        );
    }

    [TestMethod]
    public async Task Subscribe_Valid_ReturnsOk()
    {
        var req = _setupRequest.Setup(null, new NameValueCollection { { "nhsNumber", "9000000009" } }, HttpMethod.Post);
        var res = await _sut.Subscribe(req.Object);
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task Subscribe_Invalid_ReturnsBadRequest()
    {
        var req = _setupRequest.Setup(null, new NameValueCollection { { "nhsNumber", "abc" } }, HttpMethod.Post);
        var res = await _sut.Subscribe(req.Object);
        Assert.AreEqual(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [TestMethod]
    public async Task Subscribe_MissingMailboxes_ReturnsInternalServerError()
    {
        // Arrange: missing config values
        _config.Setup(x => x.Value).Returns(new ManageCaasSubscriptionConfig
        {
            ManageNemsSubscriptionDataServiceURL = null,
            CaasToMailbox = null,
            CaasFromMailbox = null
        });

        var sutMissing = new ManageCaasSubscription(
            _logger.Object,
            _createResponse,
            _httpClientFactory.Object,
            _config.Object,
            _mesh.Object
        );

        var req = _setupRequest.Setup(null, new NameValueCollection { { "nhsNumber", "9000000009" } }, HttpMethod.Post);

        // Act
        var res = await sutMissing.Subscribe(req.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, res.StatusCode);
    }

    [TestMethod]
    public async Task Unsubscribe_Valid_ReturnsOk()
    {
        var req = _setupRequest.Setup(null, new NameValueCollection { { "nhsNumber", "9000000009" } }, HttpMethod.Post);
        var res = await _sut.Unsubscribe(req.Object);
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task CheckSubscriptionStatus_Valid_ReturnsOk()
    {
        var req = _setupRequest.Setup(null, new NameValueCollection { { "nhsNumber", "9000000009" } }, HttpMethod.Get);
        var res = await _sut.CheckSubscriptionStatus(req.Object);
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task DataService_ReturnsOk()
    {
        var req = _setupRequest.Setup(null, new NameValueCollection(), HttpMethod.Get);
        var res = await _sut.NemsSubscriptionDataService(req.Object, "key");
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task CheckSubscriptionStatus_Forwarded_PropagatesResponse()
    {
        // Arrange
        var handler = new TestHandler(async (message) =>
        {
            Assert.AreEqual(HttpMethod.Get, message.Method);
            StringAssert.Contains(message.RequestUri.ToString(), "/api/CheckSubscriptionStatus?nhsNumber=9000000009");
            return new HttpResponseMessage(HttpStatusCode.Accepted)
            {
                Content = new StringContent("FORWARDED", System.Text.Encoding.UTF8, "application/json")
            };
        });
        var httpClient = new HttpClient(handler);
        _httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _config.Setup(x => x.Value).Returns(new ManageCaasSubscriptionConfig
        {
            ManageNemsSubscriptionBaseURL = "http://downstream",
            CaasToMailbox = "TEST_TO",
            CaasFromMailbox = "TEST_FROM"
        });

        var req = _setupRequest.Setup(null, new NameValueCollection { { "nhsNumber", "9000000009" } }, HttpMethod.Get);

        // Act
        var res = await _sut.CheckSubscriptionStatus(req.Object);
        var body = await AssertionHelper.ReadResponseBodyAsync(res);

        // Assert
        Assert.AreEqual(HttpStatusCode.Accepted, res.StatusCode);
        Assert.AreEqual("FORWARDED", body);
    }

    [TestMethod]
    public async Task NemsSubscriptionDataService_Forwarded_GetWithKeyAndQuery_Propagates()
    {
        // Arrange
        var handler = new TestHandler(async (message) =>
        {
            Assert.AreEqual(HttpMethod.Get, message.Method);
            StringAssert.Contains(message.RequestUri.ToString(), "/api/NemsSubscriptionDataService/my-key?foo=bar");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("FWD-OK")
            };
        });
        var httpClient = new HttpClient(handler);
        _httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _config.Setup(x => x.Value).Returns(new ManageCaasSubscriptionConfig
        {
            ManageNemsSubscriptionDataServiceURL = "http://downstream/api/NemsSubscriptionDataService",
            CaasToMailbox = "TEST_TO",
            CaasFromMailbox = "TEST_FROM"
        });

        var req = _setupRequest.Setup(null, new NameValueCollection { { "foo", "bar" } }, HttpMethod.Get);

        // Act
        var res = await _sut.NemsSubscriptionDataService(req.Object, "my-key");
        var body = await AssertionHelper.ReadResponseBodyAsync(res);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        Assert.AreEqual("FWD-OK", body);
    }

    [TestMethod]
    public async Task NemsSubscriptionDataService_Forwarded_PostBody_Propagates()
    {
        // Arrange
        var handler = new TestHandler(async (message) =>
        {
            Assert.AreEqual(HttpMethod.Post, message.Method);
            var content = await message.Content.ReadAsStringAsync();
            Assert.AreEqual("{\"foo\":\"bar\"}", content);
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("CREATED")
            };
        });
        var httpClient = new HttpClient(handler);
        _httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _config.Setup(x => x.Value).Returns(new ManageCaasSubscriptionConfig
        {
            ManageNemsSubscriptionDataServiceURL = "http://downstream/api/NemsSubscriptionDataService",
            CaasToMailbox = "TEST_TO",
            CaasFromMailbox = "TEST_FROM"
        });

        var req = _setupRequest.Setup("{\"foo\":\"bar\"}", new NameValueCollection(), HttpMethod.Post);

        // Act
        var res = await _sut.NemsSubscriptionDataService(req.Object, "abc");
        var body = await AssertionHelper.ReadResponseBodyAsync(res);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, res.StatusCode);
        Assert.AreEqual("CREATED", body);
    }

    private sealed class TestHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;
        public TestHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request);
        }
    }
}
