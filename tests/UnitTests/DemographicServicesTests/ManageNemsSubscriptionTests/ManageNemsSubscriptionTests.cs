namespace NHS.CohortManager.Tests.UnitTests.DemographicServicesTests;

using System.Net;
using System.Security.Cryptography.X509Certificates;
using Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using Moq;
using NHS.CohortManager.DemographicServices;
using DataServices.Core;
using System.Linq.Expressions;
using System.Collections.Specialized;
using Microsoft.Azure.Functions.Worker.Http;
using NHS.CohortManager.Tests.TestUtils;

[TestClass]
public class ManageNemsSubscriptionTests
{
    private readonly Mock<INemsHttpClientFunction> _nemsHttpClientFunction = new();
    private readonly CreateResponse _createResponse = new();
    private readonly Mock<IDataServiceAccessor<NemsSubscription>> _nemsSubscriptionAccessor = new();
    private readonly Mock<IOptions<ManageNemsSubscriptionConfig>> _config = new();
    private readonly Mock<ILogger<ManageNemsSubscription>> _logger = new();
    private readonly Mock<ILogger<NemsSubscriptionManager>> _subscriptionManagerLogger = new();
    private readonly SetupRequest _setupRequest = new();
    private readonly NemsSubscriptionManager _nemsSubscriptionManager;
    private readonly ManageNemsSubscription _sut;
    private const long ValidNhsNumber = 9000000009;

    public ManageNemsSubscriptionTests()
    {
        var config = new ManageNemsSubscriptionConfig
        {
            NemsFhirEndpoint = "https://nems.fhir.endpoint",
            FromAsid = "FromAsid",
            ToAsid = "ToAsid",
            SubscriptionProfile = "SubscriptionProfile",
            SubscriptionCriteria = "SubscriptionCriteria",
            NemsLocalCertPath = null,
            NemsLocalCertPassword = null
        };

        _config.Setup(x => x.Value).Returns(config);

        _nemsSubscriptionAccessor
            .Setup(x => x.Remove(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync(true);

        _nemsSubscriptionAccessor
            .Setup(x => x.GetSingle(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync(new NemsSubscription
            {
                NhsNumber = ValidNhsNumber, // Valid NHS number with checksum
                SubscriptionId = "d3b8f5c2-4c1e-4f0a-9b6c-7e8f9d1a2b3c",
            });

        _nemsSubscriptionAccessor
            .Setup(x => x.InsertSingle(It.IsAny<NemsSubscription>()))
            .ReturnsAsync(true);

        _nemsHttpClientFunction
            .Setup(x => x.SendSubscriptionPost(It.IsAny<NemsSubscriptionPostRequest>()))
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        _nemsHttpClientFunction
            .Setup(x => x.SendSubscriptionDelete(It.IsAny<NemsSubscriptionRequest>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var requestHandler = new Mock<IRequestHandler<NemsSubscription>>();

        var dummyCert = new X509Certificate2();

        _nemsSubscriptionManager = new NemsSubscriptionManager(
            _nemsHttpClientFunction.Object,
            _config.Object,
            _subscriptionManagerLogger.Object,
            _nemsSubscriptionAccessor.Object,
            dummyCert // <-- Inject here
        );

        _sut = new(
            _logger.Object,
            _createResponse,
            _nemsSubscriptionManager,
            requestHandler.Object
        );
    }

    [TestMethod]
    public async Task Subscribe_ValidRequest_ReturnsOk()
    {
        var request = _setupRequest.Setup(null, new NameValueCollection { { "NhsNumber", ValidNhsNumber.ToString() } }, HttpMethod.Post);
        var response = await _sut.Subscribe(request.Object);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task Unsubscribe_ValidRequest_ReturnsOk()
    {
        var request = _setupRequest.Setup(null, new NameValueCollection { { "NhsNumber", ValidNhsNumber.ToString() } }, HttpMethod.Post);
        var response = await _sut.Unsubscribe(request.Object);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task Subscribe_InvalidNhsNumber_ReturnsBadRequest()
    {
        var request = _setupRequest.Setup(null, new NameValueCollection { { "NhsNumber", "" } }, HttpMethod.Post);
        var response = await _sut.Subscribe(request.Object);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Unsubscribe_InvalidNhsNumber_ReturnsBadRequest()
    {
        var request = _setupRequest.Setup(null, new NameValueCollection { { "NhsNumber", "" } }, HttpMethod.Post);
        var response = await _sut.Unsubscribe(request.Object);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Unsubscribe_SubscriptionNotFound_ReturnsNotFound()
    {
        var request = _setupRequest.Setup(null, new NameValueCollection { { "NhsNumber", ValidNhsNumber.ToString() } }, HttpMethod.Post);

        _nemsSubscriptionAccessor
            .Setup(x => x.GetSingle(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync((NemsSubscription?)null);

        var response = await _sut.Unsubscribe(request.Object);
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Subscribe_InsertFails_ReturnsInternalServerError()
    {
        // Arrange: Make sure the NEMS POST returns a valid subscriptionId
        _nemsHttpClientFunction
            .Setup(x => x.SendSubscriptionPost(It.IsAny<NemsSubscriptionPostRequest>()))
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Created,
                Headers = { Location = new Uri("https://nems.fhir.endpoint/Subscription/abcd1234") }
            });

        // Arrange: Make DB insert fail
        _nemsSubscriptionAccessor
            .Setup(x => x.InsertSingle(It.IsAny<NemsSubscription>()))
            .ReturnsAsync(false);

        // Arrange: Make DB get single return null
        _nemsSubscriptionAccessor
            .Setup(x => x.GetSingle(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync((NemsSubscription?)null);


        var request = _setupRequest.Setup(null, new NameValueCollection { { "NhsNumber", ValidNhsNumber.ToString() } }, HttpMethod.Post);

        // Act
        var response = await _sut.Subscribe(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }


    [TestMethod]
    public async Task Subscribe_NemsPostFails_ReturnsInternalServerError()
    {
        var request = _setupRequest.Setup(null, new NameValueCollection { { "NhsNumber", ValidNhsNumber.ToString() } }, HttpMethod.Post);

        _nemsSubscriptionAccessor
            .Setup(x => x.GetSingle(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync((NemsSubscription?)null);


        _nemsHttpClientFunction
            .Setup(x => x.SendSubscriptionPost(It.IsAny<NemsSubscriptionPostRequest>()))
            .ReturnsAsync((HttpResponseMessage)null);

        var response = await _sut.Subscribe(request.Object);
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [TestMethod]
    public async Task Unsubscribe_DeleteFromDatabaseFails_ReturnsInternalServerError()
    {
        // Arrange: There *is* a subscription to remove
        _nemsSubscriptionAccessor
            .Setup(x => x.GetSingle(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync(new NemsSubscription
            {
                NhsNumber = ValidNhsNumber,
                SubscriptionId = "d3b8f5c2-4c1e-4f0a-9b6c-7e8f9d1a2b3c"
            });

        // Arrange: Removal fails
        _nemsSubscriptionAccessor
            .Setup(x => x.Remove(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync(false);

        var request = _setupRequest.Setup(null, new NameValueCollection { { "NhsNumber", ValidNhsNumber.ToString() } }, HttpMethod.Post);

        // Act
        var response = await _sut.Unsubscribe(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }


    [DataTestMethod]
    [DataRow(HttpStatusCode.BadRequest)]
    [DataRow(HttpStatusCode.InternalServerError)]
    [DataRow(HttpStatusCode.NotFound)]
    [DataRow(HttpStatusCode.Unauthorized)]
    [DataRow(HttpStatusCode.Forbidden)]
    public async Task Unsubscribe_DeleteFromNemsFails_ReturnsInternalServerError(HttpStatusCode nemsStatus)
    {
        var request = _setupRequest.Setup(null, new NameValueCollection { { "NhsNumber", ValidNhsNumber.ToString() } }, HttpMethod.Post);

        _nemsHttpClientFunction
            .Setup(x => x.SendSubscriptionDelete(It.IsAny<NemsSubscriptionRequest>()))
            .ReturnsAsync(new HttpResponseMessage(nemsStatus));

        var response = await _sut.Unsubscribe(request.Object);
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    //Edge case tests
    [TestMethod]
    public async Task Unsubscribe_DeleteFromNemsReturnsNull_ReturnsInternalServerError()
    {
        var request = _setupRequest.Setup(null, new NameValueCollection { { "NhsNumber", ValidNhsNumber.ToString() } }, HttpMethod.Post);
        _nemsHttpClientFunction
            .Setup(x => x.SendSubscriptionDelete(It.IsAny<NemsSubscriptionRequest>()))
            .ReturnsAsync((HttpResponseMessage)null);

        var response = await _sut.Unsubscribe(request.Object);
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [DataTestMethod]
    [DataRow("123.456.7890")]
    [DataRow("abcdefghij")]
    [DataRow("123456789_")]
    [DataRow("012345678")]
    [DataRow(null)]
    [DataRow("")]
    public async Task Subscribe_Unsubscribe_InvalidNhsNumbers_ReturnsBadRequest(string nhsNumber)
    {
        var request = _setupRequest.Setup(null, new NameValueCollection { { "NhsNumber", nhsNumber } }, HttpMethod.Post);
        var subscribeResponse = await _sut.Subscribe(request.Object);
        Assert.AreEqual(HttpStatusCode.BadRequest, subscribeResponse.StatusCode);

        var unsubscribeResponse = await _sut.Unsubscribe(request.Object);
        Assert.AreEqual(HttpStatusCode.BadRequest, unsubscribeResponse.StatusCode);
    }

    [DataTestMethod]
    [DataRow(HttpStatusCode.Conflict)]
    [DataRow((HttpStatusCode)418)]
    public async Task Unsubscribe_DeleteFromNemsWeirdStatus_ReturnsInternalServerError(HttpStatusCode nemsStatus)
    {
        var request = _setupRequest.Setup(null, new NameValueCollection { { "NhsNumber", ValidNhsNumber.ToString() } }, HttpMethod.Post);
        _nemsHttpClientFunction
            .Setup(x => x.SendSubscriptionDelete(It.IsAny<NemsSubscriptionRequest>()))
            .ReturnsAsync(new HttpResponseMessage(nemsStatus));
        var response = await _sut.Unsubscribe(request.Object);
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [TestMethod]
    public async Task CheckSubscriptionStatus_ValidNhsNumber_ReturnsOk()
    {
        var request = _setupRequest.Setup(null, new NameValueCollection { { "NhsNumber", ValidNhsNumber.ToString() } }, HttpMethod.Get);
        var response = await _sut.CheckSubscriptionStatus(request.Object);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task CheckSubscriptionStatus_NoSubscription_ReturnsNotFound()
    {
        var request = _setupRequest.Setup(null, new NameValueCollection { { "NhsNumber", ValidNhsNumber.ToString() } }, HttpMethod.Get);

        _nemsSubscriptionAccessor
            .Setup(x => x.GetSingle(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync((NemsSubscription?)null);

        var response = await _sut.CheckSubscriptionStatus(request.Object);
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task CheckSubscriptionStatus_InvalidNhsNumber_ReturnsBadRequest()
    {
        var request = _setupRequest.Setup(null, new NameValueCollection { { "NhsNumber", "invalid" } }, HttpMethod.Get);
        var response = await _sut.CheckSubscriptionStatus(request.Object);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task CheckSubscriptionStatus_MissingNhsNumber_ReturnsBadRequest()
    {
        var request = _setupRequest.Setup(null, new NameValueCollection(), HttpMethod.Get);
        var response = await _sut.CheckSubscriptionStatus(request.Object);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task NemsSubscriptionDataService_ValidRequest_ReturnsOk()
    {
        var request = _setupRequest.Setup(null, new NameValueCollection(), HttpMethod.Get);
        var mockResponse = _createResponse.CreateHttpResponse(HttpStatusCode.OK, request.Object, "Success");

        var requestHandler = new Mock<IRequestHandler<NemsSubscription>>();
        requestHandler
            .Setup(x => x.HandleRequest(It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .ReturnsAsync(mockResponse);

        var sut = new ManageNemsSubscription(
            _logger.Object,
            _createResponse,
            _nemsSubscriptionManager,
            requestHandler.Object
        );

        var response = await sut.NemsSubscriptionDataService(request.Object, "test-key");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task NemsSubscriptionDataService_ExceptionThrown_ReturnsInternalServerError()
    {
        var request = _setupRequest.Setup(null, new NameValueCollection(), HttpMethod.Get);

        var requestHandler = new Mock<IRequestHandler<NemsSubscription>>();
        requestHandler
            .Setup(x => x.HandleRequest(It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Request handler error"));

        var sut = new ManageNemsSubscription(
            _logger.Object,
            _createResponse,
            _nemsSubscriptionManager,
            requestHandler.Object
        );

        var response = await sut.NemsSubscriptionDataService(request.Object, "test-key");
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
