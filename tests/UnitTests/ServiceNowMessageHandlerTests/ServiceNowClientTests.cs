namespace NHS.CohortManager.Tests.UnitTests.ServiceNowMessageHandlerTests;

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using NHS.CohortManager.ServiceNowIntegrationService;
using NHS.CohortManager.ServiceNowIntegrationService.Models;

[TestClass]
public class ServiceNowClientTests
{
    private readonly MemoryCache _cache;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<ILogger<ServiceNowClient>> _loggerMock = new();
    private readonly Mock<IOptions<ServiceNowMessageHandlerConfig>> _configMock = new();
    private readonly ServiceNowClient _serviceNowClient;
    private readonly Mock<HttpMessageHandler> _httpMessageHandler;
    private const string ServiceNowRefreshAccessTokenUrl = "https://www.example.net/refresh";
    private const string ServiceNowUpdateUrl = "https://www.example.net/update";
    private const string ServiceNowResolutionUrl = "https://www.example.net/resolution";
    private const string AccessTokenCacheKey = "AccessToken";

    public ServiceNowClientTests()
    {
        _configMock.Setup(x => x.Value).Returns(new ServiceNowMessageHandlerConfig
        {
            ServiceNowRefreshAccessTokenUrl = ServiceNowRefreshAccessTokenUrl,
            ServiceNowUpdateUrl = ServiceNowUpdateUrl,
            ServiceNowResolutionUrl = ServiceNowResolutionUrl,
            ServiceNowClientId = "123",
            ServiceNowClientSecret = "ABC",
            ServiceNowRefreshToken = "DEF",
            ServiceBusConnectionString_client_internal = "Endpoint=",
            ServiceNowParticipantManagementTopic = "servicenow-participant-management-topic"
        });
        _cache = new MemoryCache(new MemoryCacheOptions());
        _serviceNowClient = new ServiceNowClient(
            _cache, _httpClientFactoryMock.Object, _loggerMock.Object, _configMock.Object);

        _httpMessageHandler = new Mock<HttpMessageHandler>();

        var httpClient = new HttpClient(_httpMessageHandler.Object);
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClient);
    }

    [TestMethod]
    [DataRow(HttpStatusCode.OK)]
    [DataRow(HttpStatusCode.NotFound)]
    [DataRow(HttpStatusCode.InternalServerError)]
    public async Task SendUpdate_WhenNoAccessTokenInCache_RefreshesAccessTokenAndCachesItAndSendsUpdateRequestAndReturnsResponse(
        HttpStatusCode updateResponseStatusCode)
    {
        // Arrange
        var caseNumber = "CS123";

        var jsonResponse = JsonSerializer.Serialize(new ServiceNowRefreshAccessTokenResponseBody { AccessToken = "101" });
        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.ToString() == ServiceNowRefreshAccessTokenUrl),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            }).Verifiable();
        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put && req.RequestUri.ToString() == $"{ServiceNowUpdateUrl}/{caseNumber}"),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(updateResponseStatusCode)).Verifiable();

        // Act
        var response = await _serviceNowClient.SendUpdate(caseNumber, "Note", false);

        // Assert
        Assert.IsNotNull(response);
        Assert.AreEqual(updateResponseStatusCode, response.StatusCode);
        _httpMessageHandler.Verify();
        Assert.AreEqual("101", _cache.Get(AccessTokenCacheKey));
    }

    [TestMethod]
    [DataRow(HttpStatusCode.OK)]
    [DataRow(HttpStatusCode.NotFound)]
    [DataRow(HttpStatusCode.InternalServerError)]
    public async Task SendUpdate_WhenAccessTokenInCache_UsesCachedAccessTokenAndSendsUpdateRequestAndReturnsResponse(
        HttpStatusCode updateResponseStatusCode)
    {
        // Arrange
        var caseNumber = "CS123";

        _cache.Set(AccessTokenCacheKey, "101");

        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put && req.RequestUri.ToString() == $"{ServiceNowUpdateUrl}/{caseNumber}"),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(updateResponseStatusCode)).Verifiable();

        // Act
        var response = await _serviceNowClient.SendUpdate(caseNumber, "Note");

        // Assert
        Assert.IsNotNull(response);
        Assert.AreEqual(updateResponseStatusCode, response.StatusCode);
        _httpMessageHandler.Verify();
        _httpMessageHandler.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task SendUpdate_WhenFailsToRefreshAccessToken_DoesNotSendUpdateRequestAndReturnsNull()
    {
        // Arrange
        var caseNumber = "CS123";

        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.ToString() == ServiceNowRefreshAccessTokenUrl),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized)).Verifiable();

        // Act
        var response = await _serviceNowClient.SendUpdate(caseNumber, "Note");

        // Assert
        Assert.IsNull(response);
        _httpMessageHandler.Verify();
        _httpMessageHandler.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task SendUpdate_WhenUpdateRequestReturnsUnauthorized_RefreshesAccessTokenAndRetriesUpdateRequestAndReturnsResponse()
    {
        // Arrange
        var caseNumber = "CS123";

        _cache.Set(AccessTokenCacheKey, "101");

        _httpMessageHandler
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put && req.RequestUri.ToString() == $"{ServiceNowUpdateUrl}/{caseNumber}"),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var jsonResponse = JsonSerializer.Serialize(new ServiceNowRefreshAccessTokenResponseBody { AccessToken = "102" });
        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.ToString() == ServiceNowRefreshAccessTokenUrl),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            }).Verifiable();

        // Act
        var response = await _serviceNowClient.SendUpdate(caseNumber, "Note");

        // Assert
        Assert.IsNotNull(response);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("102", _cache.Get(AccessTokenCacheKey));
        _httpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Exactly(2),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put && req.RequestUri.ToString() == $"{ServiceNowUpdateUrl}/{caseNumber}"),
            ItExpr.IsAny<CancellationToken>()
        );
        _httpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.ToString() == ServiceNowRefreshAccessTokenUrl),
            ItExpr.IsAny<CancellationToken>()
        );
        _httpMessageHandler.VerifyNoOtherCalls();
    }

    [TestMethod]
    [DataRow(HttpStatusCode.OK)]
    [DataRow(HttpStatusCode.NotFound)]
    [DataRow(HttpStatusCode.InternalServerError)]
    public async Task SendResolution_WhenAccessTokenInCache_UsesCachedAccessTokenAndSendsResolutionRequestAndReturnsResponse(
        HttpStatusCode updateResponseStatusCode)
    {
        // Arrange
        var caseNumber = "CS123";

        _cache.Set(AccessTokenCacheKey, "101");

        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put && req.RequestUri.ToString() == $"{ServiceNowResolutionUrl}/{caseNumber}"),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(updateResponseStatusCode)).Verifiable();

        // Act
        var response = await _serviceNowClient.SendResolution(caseNumber, "Note");

        // Assert
        Assert.IsNotNull(response);
        Assert.AreEqual(response.StatusCode, updateResponseStatusCode);
        _httpMessageHandler.Verify();
        _httpMessageHandler.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task SendUpdate_WhenNeedsAttentionIsFalse_SendExpectedUpdateRequestBody()
    {
        // Arrange
        var caseNumber = "CS123";

        _cache.Set(AccessTokenCacheKey, "101");

        string? updateRequestBodyJsonString = null;

        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put && req.RequestUri.ToString() == $"{ServiceNowUpdateUrl}/{caseNumber}"),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
            .Callback<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
            {
                updateRequestBodyJsonString = await request.Content!.ReadAsStringAsync();
            }).Verifiable();

        // Act
        var response = await _serviceNowClient.SendUpdate(caseNumber, "Note");

        // Assert
        Assert.AreEqual("{\"state\":10,\"work_notes\":\"Note\",\"needs_attention\":false}", updateRequestBodyJsonString);
    }

    [TestMethod]
    public async Task SendUpdate_WhenNeedsAttentionIsTrue_SendExpectedUpdateRequestBody()
    {
        // Arrange
        var caseNumber = "CS123";

        _cache.Set(AccessTokenCacheKey, "101");

        string? updateRequestBodyJsonString = null;

        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put && req.RequestUri.ToString() == $"{ServiceNowUpdateUrl}/{caseNumber}"),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
            .Callback<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
            {
                updateRequestBodyJsonString = await request.Content!.ReadAsStringAsync();
            }).Verifiable();

        // Act
        var response = await _serviceNowClient.SendUpdate(caseNumber, "Note", true);

        // Assert
        Assert.AreEqual($"{{\"state\":10,\"work_notes\":\"Note\",\"needs_attention\":true,\"assignment_group\":\"{_configMock.Object.Value.ServiceNowAssignmentGroup}\"}}",
            updateRequestBodyJsonString);
    }

    [TestMethod]
    public async Task SendResolution_SendExpectedUpdateRequestBody()
    {
        // Arrange
        var caseNumber = "CS123";

        _cache.Set(AccessTokenCacheKey, "101");

        string? resolutionRequestBodyJsonString = null;

        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put && req.RequestUri.ToString() == $"{ServiceNowResolutionUrl}/{caseNumber}"),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
            .Callback<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
            {
                resolutionRequestBodyJsonString = await request.Content!.ReadAsStringAsync();
            }).Verifiable();

        // Act
        var response = await _serviceNowClient.SendResolution(caseNumber, "Note");

        // Assert
        Assert.AreEqual("{\"state\":6,\"resolution_code\":\"28\",\"close_notes\":\"Note\"}", resolutionRequestBodyJsonString);
    }
}
