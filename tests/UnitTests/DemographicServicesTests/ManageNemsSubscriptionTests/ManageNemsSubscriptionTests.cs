namespace NHS.CohortManager.Tests.UnitTests.DemographicServicesTests;

using System.Net;
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
    private readonly Mock<IHttpClientFunction> _httpClientFunction = new();
    private readonly CreateResponse _createResponse = new();
    private readonly Mock<IDataServiceAccessor<NemsSubscription>> _nemsSubscriptionAccessor = new();
    private readonly Mock<IOptions<ManageNemsSubscriptionConfig>> _config = new();
    private readonly Mock<ILogger<ManageNemsSubscription>> _logger = new();
    private readonly Mock<ILogger<NemsSubscriptionManager>> _subscriptionManagerLogger = new();
    private readonly SetupRequest _setupRequest = new();
    private readonly NemsSubscriptionManager _nemsSubscriptionManager;
    private readonly ManageNemsSubscription _sut;

    public ManageNemsSubscriptionTests()
    {
        var config = new ManageNemsSubscriptionConfig
        {
            NemsFhirEndpoint = "NemsFhirEndpoint",
            NemsDeleteEndpoint = "NemsDeleteEndpoint",
            RetrievePdsDemographicURL = "RetrievePdsDemographicURL",
            SpineAccessToken = "SpineAccessToken",
            FromAsid = "FromAsid",
            ToAsid = "ToAsid",
            SubscriptionProfile = "SubscriptionProfile",
            SubscriptionCriteria = "SubscriptionCriteria",
            CallbackEndpoint = "CallbackEndpoint"
        };

        _config.Setup(x => x.Value).Returns(config);

        _nemsSubscriptionAccessor
            .Setup(x => x.Remove(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync(true);

        _nemsSubscriptionAccessor
            .Setup(x => x.GetSingle(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync(new NemsSubscription
            {
                NhsNumber = 1233456,
                SubscriptionId = new Guid("d3b8f5c2-4c1e-4f0a-9b6c-7e8f9d1a2b3c"),
            });

        _nemsSubscriptionAccessor
            .Setup(x => x.InsertSingle(It.IsAny<NemsSubscription>()))
            .ReturnsAsync(true);

        _httpClientFunction
            .Setup(x => x.SendNemsPost(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        _httpClientFunction
            .Setup(x => x.SendDelete(It.IsAny<string>()))
            .ReturnsAsync(true);
        
        var requestHandler = new Mock<IRequestHandler<NemsSubscription>>();

        _nemsSubscriptionManager = new NemsSubscriptionManager(
            _httpClientFunction.Object,
            _config.Object,
            _subscriptionManagerLogger.Object,
            _nemsSubscriptionAccessor.Object);

        _sut = new(_logger.Object,
                    _createResponse,
                    _nemsSubscriptionManager,
                    requestHandler.Object);
    }

    [TestMethod]
    public async Task Subscribe_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = _setupRequest.Setup(null, new NameValueCollection {{"NhsNumber", "1233456"}}, HttpMethod.Post);

        // Act
        var response = await _sut.Subscribe(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task Unsubscribe_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = _setupRequest.Setup(null, new NameValueCollection { { "NhsNumber", "1233456" } }, HttpMethod.Post);

        // Act
        var response = await _sut.Unsubscribe(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task Subscribe_InvalidNhsNumber_ReturnsBadRequest()
    {
        // Arrange
        var request = _setupRequest.Setup(null, new NameValueCollection { { "NhsNumber", "" } }, HttpMethod.Post);

        // Act
        var response = await _sut.Subscribe(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Unsubscribe_InvalidNhsNumber_ReturnsBadRequest()
    {
        // Arrange
        var request = _setupRequest.Setup(null, new NameValueCollection { { "NhsNumber", "" } }, HttpMethod.Post);

        // Act
        var response = await _sut.Unsubscribe(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Unsubscribe_SubscriptionNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = _setupRequest.Setup(null, new NameValueCollection { { "NhsNumber", "1233456" } }, HttpMethod.Post);

        _nemsSubscriptionAccessor
            .Setup(x => x.GetSingle(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync((NemsSubscription?)null);

        // Act
        var response = await _sut.Unsubscribe(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Subscribe_InsertFails_ReturnsInternalServerError()
    {
        // Arrange
        var request = _setupRequest.Setup(null, new NameValueCollection { { "NhsNumber", "1233456" } }, HttpMethod.Post);

        _nemsSubscriptionAccessor
            .Setup(x => x.InsertSingle(It.IsAny<NemsSubscription>()))
            .ReturnsAsync(false);

        // Act
        var response = await _sut.Subscribe(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [TestMethod]
    public async Task Subscribe_NemsPostFails_ReturnsInternalServerError()
    {
        // Arrange
        var request = _setupRequest.Setup(null, new NameValueCollection { { "NhsNumber", "1233456" } }, HttpMethod.Post);

        _httpClientFunction
            .Setup(x => x.SendNemsPost(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((HttpResponseMessage)null);

        // Act
        var response = await _sut.Subscribe(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [TestMethod]
    public async Task Unsubscribe_DeleteFromDatabaseFails_ReturnsInternalServerError()
    {
        // Arrange
        var request = _setupRequest.Setup(null, new NameValueCollection { { "NhsNumber", "1233456" } }, HttpMethod.Post);

        _nemsSubscriptionAccessor
            .Setup(x => x.Remove(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync(false);

        // Act
        var response = await _sut.Unsubscribe(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [TestMethod]
    public async Task Unsubscribe_DeleteFromNemsFails_ReturnsInternalServerError()
    {
        // Arrange
        var request = _setupRequest.Setup(null, new NameValueCollection { { "NhsNumber", "1233456" } }, HttpMethod.Post);

        _httpClientFunction
            .Setup(x => x.SendDelete(It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var response = await _sut.Unsubscribe(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
