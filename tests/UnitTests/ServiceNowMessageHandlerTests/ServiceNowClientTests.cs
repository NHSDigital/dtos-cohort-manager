namespace NHS.CohortManager.Tests.UnitTests.ServiceNowMessageHandlerTests;

using System.Net;
using System.Net.Http;
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
    private const string AccessTokenCacheKey = "AccessToken";

    public ServiceNowClientTests()
    {
        _configMock.Setup(x => x.Value).Returns(new ServiceNowMessageHandlerConfig
        {
            ServiceNowRefreshAccessTokenUrl = ServiceNowRefreshAccessTokenUrl,
            ServiceNowUpdateUrl = ServiceNowUpdateUrl,
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
    [DataRow(HttpStatusCode.NotFound)]
    [DataRow(HttpStatusCode.InternalServerError)]
    public async Task SendUpdate_WhenNoAccessTokenInCache_RefreshesAccessTokenAndCachesItAndSendsUpdateRequestAndReturnsResponse(
        HttpStatusCode updateResponseStatusCode)
    {
        // Arrange
        var sysId = "sysId-123";
        var payload = new ServiceNowUpdateRequestBody
        {
            State = 1,
            WorkNotes = "Note"
        };

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
            });
        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put && req.RequestUri.ToString() == $"{ServiceNowUpdateUrl}/{sysId}"),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(updateResponseStatusCode));

        // Act
        var response = await _serviceNowClient.SendUpdate(sysId, payload);

        // Assert
        Assert.IsNotNull(response);
        Assert.AreEqual(updateResponseStatusCode, response.StatusCode);
        _httpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.ToString() == ServiceNowRefreshAccessTokenUrl),
            ItExpr.IsAny<CancellationToken>()
        );
        _httpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put && req.RequestUri.ToString() == $"{ServiceNowUpdateUrl}/{sysId}"),
            ItExpr.IsAny<CancellationToken>()
        );
        Assert.AreEqual("101", _cache.Get(AccessTokenCacheKey));
    }

    [TestMethod]
    [DataRow(HttpStatusCode.NotFound)]
    [DataRow(HttpStatusCode.InternalServerError)]
    public async Task SendUpdate_WhenAccessTokenInCache_UsesCachedAccessTokenAndSendsUpdateRequestAndReturnsResponse(
        HttpStatusCode updateResponseStatusCode
    )
    {
        // Arrange
        var sysId = "sysId-123";
        var payload = new ServiceNowUpdateRequestBody
        {
            State = 1,
            WorkNotes = "Note"
        };

        _cache.Set(AccessTokenCacheKey, "101");

        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put && req.RequestUri.ToString() == $"{ServiceNowUpdateUrl}/{sysId}"),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(updateResponseStatusCode));

        // Act
        var response = await _serviceNowClient.SendUpdate(sysId, payload);

        // Assert
        Assert.IsNotNull(response);
        Assert.AreEqual(updateResponseStatusCode, response.StatusCode);
        _httpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put && req.RequestUri.ToString() == $"{ServiceNowUpdateUrl}/{sysId}"),
            ItExpr.IsAny<CancellationToken>()
        );
        _httpMessageHandler.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task SendUpdate_WhenFailsToRefreshAccessToken_DoesNotSendUpdateRequestAndReturnsNull()
    {
        // Arrange
        var sysId = "sysId-123";
        var payload = new ServiceNowUpdateRequestBody
        {
            State = 1,
            WorkNotes = "Note"
        };

        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.ToString() == ServiceNowRefreshAccessTokenUrl),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        // Act
        var response = await _serviceNowClient.SendUpdate(sysId, payload);

        // Assert
        Assert.IsNull(response);
        _httpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.ToString() == ServiceNowRefreshAccessTokenUrl),
            ItExpr.IsAny<CancellationToken>()
        );
        _httpMessageHandler.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task SendUpdate_WhenUpdateRequestReturnsUnauthorized_RefreshesAccessTokenAndRetriesUpdateRequestAndReturnsResponse()
    {
        // Arrange
        var sysId = "sysId-123";
        var payload = new ServiceNowUpdateRequestBody
        {
            State = 1,
            WorkNotes = "Note"
        };

        _cache.Set(AccessTokenCacheKey, "101");

        _httpMessageHandler
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put && req.RequestUri.ToString() == $"{ServiceNowUpdateUrl}/{sysId}"),
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
            });

        // Act
        var response = await _serviceNowClient.SendUpdate(sysId, payload);

        // Assert
        Assert.IsNotNull(response);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        _httpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Exactly(2),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put && req.RequestUri.ToString() == $"{ServiceNowUpdateUrl}/{sysId}"),
            ItExpr.IsAny<CancellationToken>()
        );
        _httpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.ToString() == ServiceNowRefreshAccessTokenUrl),
            ItExpr.IsAny<CancellationToken>()
        );
        Assert.AreEqual("102", _cache.Get(AccessTokenCacheKey));
        _httpMessageHandler.VerifyNoOtherCalls();
    }
}
