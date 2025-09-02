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
using DataServices.Core;
using Model;
using System.ComponentModel.DataAnnotations;
using System.Linq;

[TestClass]
public class ManageCaasSubscriptionTests
{
    private readonly CreateResponse _createResponse = new();
    private readonly Mock<ILogger<ManageCaasSubscription>> _logger = new();
    private readonly SetupRequest _setupRequest = new();
    private readonly ManageCaasSubscription _sut;
    private readonly Mock<IOptions<ManageCaasSubscriptionConfig>> _config = new();
    private readonly Mock<IMeshSendCaasSubscribe> _mesh = new();
    private readonly Mock<IRequestHandler<NemsSubscription>> _requestHandler = new();
    private readonly Mock<IDataServiceAccessor<NemsSubscription>> _nemsAccessor = new();
    private readonly Mock<IMeshPoller> _meshPoller = new();

    public ManageCaasSubscriptionTests()
    {
        _config.Setup(x => x.Value).Returns(new ManageCaasSubscriptionConfig
        {
            CaasToMailbox = "TEST_TO",
            CaasFromMailbox = "TEST_FROM",
            MeshApiBaseUrl = "http://localhost",
            MeshCaasSharedKey = "dummy"
        });

        _mesh
            .Setup(m => m.SendSubscriptionRequest(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("STUB_MSG_ID");

        // Default: pretend a subscription exists so generic tests expect OK
        _nemsAccessor
            .Setup(a => a.GetSingle(It.IsAny<System.Linq.Expressions.Expression<Func<NemsSubscription, bool>>>() ))
            .ReturnsAsync(new NemsSubscription { NhsNumber = 9000000009, SubscriptionId = "SUB123" });

        // Default: request handler returns OK for data-service calls
        _requestHandler
            .Setup(r => r.HandleRequest(It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .ReturnsAsync((HttpRequestData r, string k) => _createResponse.CreateHttpResponse(HttpStatusCode.OK, r, "OK"));

        // Default: DB insert succeeds for subscribe happy path
        _nemsAccessor
            .Setup(a => a.InsertSingle(It.IsAny<NemsSubscription>()))
            .ReturnsAsync(true);

        _sut = new ManageCaasSubscription(
            _logger.Object,
            _createResponse,
            _config.Object,
            _mesh.Object,
            _requestHandler.Object,
            _nemsAccessor.Object,
            _meshPoller.Object
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
    public async Task Unsubscribe_Valid_ReturnsOk()
    {
        var req = _setupRequest.Setup(null, new NameValueCollection { { "nhsNumber", "9000000009" } }, HttpMethod.Post);
        var res = await _sut.Unsubscribe(req.Object);
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task Unsubscribe_Invalid_ReturnsBadRequest()
    {
        var req = _setupRequest.Setup(null, new NameValueCollection { { "nhsNumber", "" } }, HttpMethod.Post);
        var res = await _sut.Unsubscribe(req.Object);
        Assert.AreEqual(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [TestMethod]
    public async Task CheckSubscriptionStatus_Valid_ReturnsOk()
    {
        var req = _setupRequest.Setup(null, new NameValueCollection { { "nhsNumber", "9000000009" } }, HttpMethod.Get);
        var res = await _sut.CheckSubscriptionStatus(req.Object);
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [DataTestMethod]
    [DataRow((string)null)]
    [DataRow("")]
    [DataRow("abc")]
    [DataRow("12345")]
    public async Task CheckSubscriptionStatus_InvalidInputs_ReturnsBadRequest(string nhsNumber)
    {
        var req = _setupRequest.Setup(null, new NameValueCollection { { "nhsNumber", nhsNumber } }, HttpMethod.Get);
        var res = await _sut.CheckSubscriptionStatus(req.Object);
        Assert.AreEqual(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [TestMethod]
    public async Task DataService_ReturnsOk()
    {
        var req = _setupRequest.Setup(null, new NameValueCollection(), HttpMethod.Get);
        var res = await _sut.NemsSubscriptionDataService(req.Object, "key");
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task CheckSubscriptionStatus_Found_ReturnsOk()
    {
        _nemsAccessor
            .Setup(a => a.GetSingle(It.IsAny<System.Linq.Expressions.Expression<Func<NemsSubscription, bool>>>() ))
            .ReturnsAsync(new NemsSubscription { NhsNumber = 9000000009, SubscriptionId = "SUB123" });

        var req = _setupRequest.Setup(null, new NameValueCollection { { "nhsNumber", "9000000009" } }, HttpMethod.Get);

        // Act
        var res = await _sut.CheckSubscriptionStatus(req.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task NemsSubscriptionDataService_ValidRequest_DelegatesToHandler()
    {
        var req = _setupRequest.Setup(null, new NameValueCollection(), HttpMethod.Get);
        var expected = _createResponse.CreateHttpResponse(HttpStatusCode.OK, req.Object, "OK");
        _requestHandler
            .Setup(r => r.HandleRequest(It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .ReturnsAsync(expected);

        // Act
        var res = await _sut.NemsSubscriptionDataService(req.Object, "my-key");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        _requestHandler.Verify(r => r.HandleRequest(It.Is<HttpRequestData>(h => object.ReferenceEquals(h, req.Object)), "my-key"), Times.Once);
    }

    [TestMethod]
    public async Task NemsSubscriptionDataService_HandlerThrows_ReturnsInternalServerError()
    {
        var req = _setupRequest.Setup(null, new NameValueCollection(), HttpMethod.Get);
        _requestHandler
            .Setup(r => r.HandleRequest(It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("boom"));
        var res = await _sut.NemsSubscriptionDataService(req.Object, "key");
        Assert.AreEqual(HttpStatusCode.InternalServerError, res.StatusCode);
    }

    [TestMethod]
    public async Task CheckSubscriptionStatus_NotFound_ReturnsNotFound()
    {
        var req = _setupRequest.Setup(null, new NameValueCollection { { "nhsNumber", "9000000009" } }, HttpMethod.Get);
        _nemsAccessor
            .Setup(a => a.GetSingle(It.IsAny<System.Linq.Expressions.Expression<Func<NemsSubscription, bool>>>() ))
            .ReturnsAsync((NemsSubscription?)null);

        var res = await _sut.CheckSubscriptionStatus(req.Object);
        Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
    }

    [TestMethod]
    public async Task CheckSubscriptionStatus_AccessorThrows_ReturnsInternalServerError()
    {
        var req = _setupRequest.Setup(null, new NameValueCollection { { "nhsNumber", "9000000009" } }, HttpMethod.Get);
        _nemsAccessor
            .Setup(a => a.GetSingle(It.IsAny<System.Linq.Expressions.Expression<Func<NemsSubscription, bool>>>() ))
            .ThrowsAsync(new Exception("db-error"));
        var res = await _sut.CheckSubscriptionStatus(req.Object);
        Assert.AreEqual(HttpStatusCode.InternalServerError, res.StatusCode);
    }

    [TestMethod]
    public async Task Subscribe_MeshCalled_WithConfigMailboxes()
    {
        _nemsAccessor.Setup(a => a.InsertSingle(It.IsAny<NemsSubscription>())).ReturnsAsync(true);
        var req = _setupRequest.Setup(null, new NameValueCollection { { "nhsNumber", "9000000009" } }, HttpMethod.Post);
        var res = await _sut.Subscribe(req.Object);
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        _mesh.Verify(m => m.SendSubscriptionRequest(9000000009L, "TEST_TO", "TEST_FROM"), Times.Once);
        _nemsAccessor.Verify(a => a.InsertSingle(It.Is<NemsSubscription>(n => n.NhsNumber == 9000000009L && n.SubscriptionSource == SubscriptionSource.MESH && !string.IsNullOrEmpty(n.SubscriptionId))), Times.Once);
    }

    [TestMethod]
    public async Task Subscribe_MeshThrows_ReturnsInternalServerError()
    {
        _mesh
            .Setup(m => m.SendSubscriptionRequest(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("mesh-down"));

        var req = _setupRequest.Setup(null, new NameValueCollection { { "nhsNumber", "9000000009" } }, HttpMethod.Post);
        var res = await _sut.Subscribe(req.Object);
        Assert.AreEqual(HttpStatusCode.InternalServerError, res.StatusCode);
    }

    [TestMethod]
    public async Task Subscribe_DBInsertFails_ReturnsInternalServerError()
    {
        _nemsAccessor.Setup(a => a.InsertSingle(It.IsAny<NemsSubscription>())).ReturnsAsync(false);
        var req = _setupRequest.Setup(null, new NameValueCollection { { "nhsNumber", "9000000009" } }, HttpMethod.Post);
        var res = await _sut.Subscribe(req.Object);
        Assert.AreEqual(HttpStatusCode.InternalServerError, res.StatusCode);
    }

    [TestMethod]
    public async Task PollMeshMailbox_UsesConfigFromMailbox()
    {
        // Ensure config has expected mailbox
        _config.Setup(x => x.Value).Returns(new ManageCaasSubscriptionConfig
        {
            CaasFromMailbox = "TEST_FROM",
            CaasToMailbox = "TEST_TO",
            MeshApiBaseUrl = "http://localhost",
            MeshCaasSharedKey = "dummy"
        });

        var sut = new ManageCaasSubscription(
            _logger.Object,
            _createResponse,
            _config.Object,
            _mesh.Object,
            _requestHandler.Object,
            _nemsAccessor.Object,
            _meshPoller.Object
        );

        await sut.RunAsync(null);
        _meshPoller.Verify(p => p.ExecuteHandshake("TEST_FROM"), Times.Once);
    }

    [TestMethod]
    public void Config_MissingMailboxes_FailsValidation()
    {
        var cfg = new ManageCaasSubscriptionConfig
        {
            MeshApiBaseUrl = "http://localhost",
            MeshCaasSharedKey = "dummy"
        };
        var context = new ValidationContext(cfg);
        var results = new System.Collections.Generic.List<ValidationResult>();
        var isValid = Validator.TryValidateObject(cfg, context, results, validateAllProperties: true);
        Assert.IsFalse(isValid);
        Assert.IsTrue(results.Any(r => r.MemberNames.Contains("CaasToMailbox")));
        Assert.IsTrue(results.Any(r => r.MemberNames.Contains("CaasFromMailbox")));
    }

    [TestMethod]
    public void Config_MissingMeshApiBaseUrl_FailsValidation()
    {
        // Arrange
        var cfg = new ManageCaasSubscriptionConfig
        {
            CaasToMailbox = "TEST_TO",
            CaasFromMailbox = "TEST_FROM",
            MeshCaasSharedKey = "dummy"
        };

        var context = new ValidationContext(cfg);
        var results = new System.Collections.Generic.List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(cfg, context, results, validateAllProperties: true);

        // Assert
        Assert.IsFalse(isValid);
        Assert.IsTrue(results.Any(r => r.MemberNames.Contains("MeshApiBaseUrl")));
    }
}
