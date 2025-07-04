namespace NHS.CohortManager.Tests.UnitTests.HttpClientFunctionTests;

using Moq;
using Microsoft.Extensions.Logging;
using Common;
using System.Net;
using Moq.Protected;
using System.Security.Cryptography.X509Certificates;

[TestClass]
public class NemsHttpClientFunctionTests
{
    private readonly Mock<ILogger<NemsHttpClientFunction>> _nemsLogger = new();
    private readonly Mock<INemsHttpClientProvider> _nemsHttpClientProvider = new();
    private readonly Mock<HttpMessageHandler> _httpMessageHandler = new();
    private NemsHttpClientFunction? _nemsFunction;
    private readonly string _mockUrl = "http://test.com";
    private const string _mockContent = "mock content";

    #region JWT Token Generation
    [TestMethod]
    public void GenerateJwtToken_ValidParameters_ReturnsJwtToken()
    {
        // Arrange
        _nemsFunction = new NemsHttpClientFunction(_nemsLogger.Object, _nemsHttpClientProvider.Object);
        var asid = "test-asid";
        var audience = "https://nems.endpoint";
        var scope = "patient/Subscription.write";

        // Act
        var result = _nemsFunction.GenerateJwtToken(asid, audience, scope);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains('.'));
        Assert.IsTrue(result.EndsWith('.'));
        
        // JWT should have 3 parts (header.payload.signature) - signature is empty
        var parts = result.Split('.');
        Assert.AreEqual(3, parts.Length);
        Assert.AreEqual(string.Empty, parts[2]); // Empty signature for unsigned JWT
    }

    [TestMethod]
    public void GenerateJwtToken_ContainsExpectedClaims_ValidatesPayload()
    {
        // Arrange
        _nemsFunction = new NemsHttpClientFunction(_nemsLogger.Object, _nemsHttpClientProvider.Object);
        var asid = "test-asid-123";
        var audience = "https://test.nhs.uk";
        var scope = "patient/Subscription.write";

        // Act
        var result = _nemsFunction.GenerateJwtToken(asid, audience, scope);

        // Assert
        var parts = result.Split('.');
        var payloadJson = System.Text.Encoding.UTF8.GetString(
            Convert.FromBase64String(parts[1].PadRight((parts[1].Length + 3) & ~3, '=')));
        
        // Verify payload contains expected claims
        Assert.IsTrue(payloadJson.Contains("\"iss\":\"https://nems.nhs.uk\""));
        Assert.IsTrue(payloadJson.Contains($"\"sub\":\"https://fhir.nhs.uk/Id/accredited-system|{asid}\""));
        Assert.IsTrue(payloadJson.Contains($"\"aud\":\"{audience}\""));
        Assert.IsTrue(payloadJson.Contains($"\"scope\":\"{scope}\""));
        Assert.IsTrue(payloadJson.Contains("\"reason_for_request\":\"directcare\""));
    }

    [TestMethod]
    public void GenerateJwtToken_HasValidTimestamps_ExpiresInOneHour()
    {
        // Arrange
        _nemsFunction = new NemsHttpClientFunction(_nemsLogger.Object, _nemsHttpClientProvider.Object);
        var beforeCall = DateTimeOffset.UtcNow;

        // Act
        var result = _nemsFunction.GenerateJwtToken("asid", "aud", "scope");

        // Assert
        var afterCall = DateTimeOffset.UtcNow;
        var parts = result.Split('.');
        var payloadJson = System.Text.Encoding.UTF8.GetString(
            Convert.FromBase64String(parts[1].PadRight((parts[1].Length + 3) & ~3, '=')));
        
        // Should contain iat and exp claims
        Assert.IsTrue(payloadJson.Contains("\"iat\":"));
        Assert.IsTrue(payloadJson.Contains("\"exp\":"));
        
        // Parse and verify timestamps are reasonable
        var iatMatch = System.Text.RegularExpressions.Regex.Match(payloadJson, "\"iat\":(\\d+)");
        var expMatch = System.Text.RegularExpressions.Regex.Match(payloadJson, "\"exp\":(\\d+)");
        
        Assert.IsTrue(iatMatch.Success);
        Assert.IsTrue(expMatch.Success);
        
        var iat = long.Parse(iatMatch.Groups[1].Value);
        var exp = long.Parse(expMatch.Groups[1].Value);
        
        // iat should be around now
        Assert.IsTrue(iat >= beforeCall.ToUnixTimeSeconds());
        Assert.IsTrue(iat <= afterCall.ToUnixTimeSeconds());
        
        // exp should be about 1 hour (3600 seconds) after iat
        Assert.AreEqual(3600, exp - iat);
    }
    #endregion

    #region SendSubscriptionPost
    [TestMethod]
    public async Task SendSubscriptionPost_SuccessfulPost_ReturnsOkResponse()
    {
        // Arrange
        var successResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Headers = { Location = new Uri("https://nems.endpoint/Subscription/123") }
        };

        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && 
                    req.Headers.Authorization != null &&
                    req.Headers.Contains("fromASID") &&
                    req.Headers.Contains("toASID")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(successResponse);

        var httpClient = new HttpClient(_httpMessageHandler.Object);
        _nemsHttpClientProvider.Setup(p => p.CreateClient(null, false)).Returns(httpClient);
        _nemsFunction = new NemsHttpClientFunction(_nemsLogger.Object, _nemsHttpClientProvider.Object);

        // Act
        var request = new NemsSubscriptionPostRequest
        {
            Url = _mockUrl,
            SubscriptionJson = _mockContent,
            JwtToken = "jwt-token",
            FromAsid = "from-asid",
            ToAsid = "to-asid",
            ClientCertificate = null!,
            BypassCertValidation = false
        };
        var result = await _nemsFunction.SendSubscriptionPost(request);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
    }

    [TestMethod]
    public async Task SendSubscriptionPost_FailedPost_ReturnsErrorResponse()
    {
        // Arrange
        var errorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Error content")
        };

        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(errorResponse);

        var httpClient = new HttpClient(_httpMessageHandler.Object);
        _nemsHttpClientProvider.Setup(p => p.CreateClient(null, false)).Returns(httpClient);
        _nemsFunction = new NemsHttpClientFunction(_nemsLogger.Object, _nemsHttpClientProvider.Object);

        // Act
        var request = new NemsSubscriptionPostRequest
        {
            Url = _mockUrl,
            SubscriptionJson = _mockContent,
            JwtToken = "jwt-token",
            FromAsid = "from-asid",
            ToAsid = "to-asid",
            ClientCertificate = null!,
            BypassCertValidation = false
        };
        var result = await _nemsFunction.SendSubscriptionPost(request);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task SendSubscriptionPost_ExceptionThrown_ThrowsException()
    {
        // Arrange
        var errorMessage = "Network error";

        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new Exception(errorMessage));

        var httpClient = new HttpClient(_httpMessageHandler.Object);
        _nemsHttpClientProvider.Setup(p => p.CreateClient(null, false)).Returns(httpClient);
        _nemsFunction = new NemsHttpClientFunction(_nemsLogger.Object, _nemsHttpClientProvider.Object);

        // Act & Assert
        var request = new NemsSubscriptionPostRequest
        {
            Url = _mockUrl,
            SubscriptionJson = _mockContent,
            JwtToken = "jwt-token",
            FromAsid = "from-asid",
            ToAsid = "to-asid",
            ClientCertificate = null!,
            BypassCertValidation = false
        };
        var exception = await Assert.ThrowsExceptionAsync<Exception>(() => 
            _nemsFunction.SendSubscriptionPost(request));
        
        Assert.AreEqual(errorMessage, exception.Message);
    }

    [TestMethod]
    public async Task SendSubscriptionPost_WithClientCertificate_UsesNemsProvider()
    {
        // Arrange
        var successResponse = new HttpResponseMessage(HttpStatusCode.Created);
        var mockCertificate = new X509Certificate2();

        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(successResponse);

        var httpClient = new HttpClient(_httpMessageHandler.Object);
        _nemsHttpClientProvider.Setup(p => p.CreateClient(mockCertificate, false)).Returns(httpClient);
        _nemsFunction = new NemsHttpClientFunction(_nemsLogger.Object, _nemsHttpClientProvider.Object);

        // Act
        var request = new NemsSubscriptionPostRequest
        {
            Url = _mockUrl,
            SubscriptionJson = _mockContent,
            JwtToken = "jwt-token",
            FromAsid = "from-asid",
            ToAsid = "to-asid",
            ClientCertificate = mockCertificate,
            BypassCertValidation = false
        };
        var result = await _nemsFunction.SendSubscriptionPost(request);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _nemsHttpClientProvider.Verify(p => p.CreateClient(mockCertificate, false), Times.Once);
    }
    #endregion

    #region SendSubscriptionDelete
    [TestMethod]
    public async Task SendSubscriptionDelete_SuccessfulDelete_ReturnsOkResponse()
    {
        // Arrange
        var successResponse = new HttpResponseMessage(HttpStatusCode.OK);

        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Delete &&
                    req.Headers.Authorization != null &&
                    req.Headers.Contains("fromASID") &&
                    req.Headers.Contains("toASID")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(successResponse);

        var httpClient = new HttpClient(_httpMessageHandler.Object);
        _nemsHttpClientProvider.Setup(p => p.CreateClient(null, false)).Returns(httpClient);
        _nemsFunction = new NemsHttpClientFunction(_nemsLogger.Object, _nemsHttpClientProvider.Object);

        // Act
        var request = new NemsSubscriptionRequest
        {
            Url = _mockUrl,
            JwtToken = "jwt-token",
            FromAsid = "from-asid",
            ToAsid = "to-asid",
            ClientCertificate = null!,
            BypassCertValidation = false
        };
        var result = await _nemsFunction.SendSubscriptionDelete(request);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task SendSubscriptionDelete_FailedDelete_ReturnsErrorResponse()
    {
        // Arrange
        var errorResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("Not found")
        };

        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Delete),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(errorResponse);

        var httpClient = new HttpClient(_httpMessageHandler.Object);
        _nemsHttpClientProvider.Setup(p => p.CreateClient(null, false)).Returns(httpClient);
        _nemsFunction = new NemsHttpClientFunction(_nemsLogger.Object, _nemsHttpClientProvider.Object);

        // Act
        var request = new NemsSubscriptionRequest
        {
            Url = _mockUrl,
            JwtToken = "jwt-token",
            FromAsid = "from-asid",
            ToAsid = "to-asid",
            ClientCertificate = null!,
            BypassCertValidation = false
        };
        var result = await _nemsFunction.SendSubscriptionDelete(request);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
    }

    [TestMethod]
    public async Task SendSubscriptionDelete_WithClientCertificate_UsesNemsProvider()
    {
        // Arrange
        var successResponse = new HttpResponseMessage(HttpStatusCode.OK);
        var mockCertificate = new X509Certificate2();

        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Delete),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(successResponse);

        var httpClient = new HttpClient(_httpMessageHandler.Object);
        _nemsHttpClientProvider.Setup(p => p.CreateClient(mockCertificate, true)).Returns(httpClient);
        _nemsFunction = new NemsHttpClientFunction(_nemsLogger.Object, _nemsHttpClientProvider.Object);

        // Act
        var request = new NemsSubscriptionRequest
        {
            Url = _mockUrl,
            JwtToken = "jwt-token",
            FromAsid = "from-asid",
            ToAsid = "to-asid",
            ClientCertificate = mockCertificate,
            BypassCertValidation = true
        };
        var result = await _nemsFunction.SendSubscriptionDelete(request);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _nemsHttpClientProvider.Verify(p => p.CreateClient(mockCertificate, true), Times.Once);
    }
    #endregion

    #region NEMS Headers Validation
    [TestMethod]
    public async Task SendSubscriptionPost_SetsRequiredNemsHeaders()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var successResponse = new HttpResponseMessage(HttpStatusCode.Created);

        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((request, token) => capturedRequest = request)
            .ReturnsAsync(successResponse);

        var httpClient = new HttpClient(_httpMessageHandler.Object);
        _nemsHttpClientProvider.Setup(p => p.CreateClient(null, false)).Returns(httpClient);
        _nemsFunction = new NemsHttpClientFunction(_nemsLogger.Object, _nemsHttpClientProvider.Object);

        // Act
        var request = new NemsSubscriptionPostRequest
        {
            Url = _mockUrl,
            SubscriptionJson = _mockContent,
            JwtToken = "test-jwt",
            FromAsid = "test-from-asid",
            ToAsid = "test-to-asid",
            ClientCertificate = null!,
            BypassCertValidation = false
        };
        await _nemsFunction.SendSubscriptionPost(request);

        // Assert
        Assert.IsNotNull(capturedRequest);
        Assert.AreEqual("Bearer", capturedRequest.Headers.Authorization?.Scheme);
        Assert.AreEqual("test-jwt", capturedRequest.Headers.Authorization?.Parameter);
        Assert.IsTrue(capturedRequest.Headers.Contains("fromASID"));
        Assert.IsTrue(capturedRequest.Headers.Contains("toASID"));
        Assert.IsTrue(capturedRequest.Headers.Contains("InteractionID"));
        
        var fromAsidValues = capturedRequest.Headers.GetValues("fromASID");
        Assert.AreEqual("test-from-asid", fromAsidValues.First());
        
        var toAsidValues = capturedRequest.Headers.GetValues("toASID");
        Assert.AreEqual("test-to-asid", toAsidValues.First());
        
        var interactionIdValues = capturedRequest.Headers.GetValues("InteractionID");
        Assert.AreEqual("urn:nhs:names:services:clinicals-sync:SubscriptionsApiPost", interactionIdValues.First());
    }

    [TestMethod]
    public async Task SendSubscriptionDelete_SetsRequiredNemsHeaders()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var successResponse = new HttpResponseMessage(HttpStatusCode.OK);

        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((request, token) => capturedRequest = request)
            .ReturnsAsync(successResponse);

        var httpClient = new HttpClient(_httpMessageHandler.Object);
        _nemsHttpClientProvider.Setup(p => p.CreateClient(null, false)).Returns(httpClient);
        _nemsFunction = new NemsHttpClientFunction(_nemsLogger.Object, _nemsHttpClientProvider.Object);

        // Act
        var request = new NemsSubscriptionRequest
        {
            Url = _mockUrl,
            JwtToken = "test-jwt",
            FromAsid = "test-from-asid",
            ToAsid = "test-to-asid",
            ClientCertificate = null!,
            BypassCertValidation = false
        };
        await _nemsFunction.SendSubscriptionDelete(request);

        // Assert
        Assert.IsNotNull(capturedRequest);
        Assert.AreEqual("Bearer", capturedRequest.Headers.Authorization?.Scheme);
        Assert.AreEqual("test-jwt", capturedRequest.Headers.Authorization?.Parameter);
        
        var interactionIdValues = capturedRequest.Headers.GetValues("InteractionID");
        Assert.AreEqual("urn:nhs:names:services:clinicals-sync:SubscriptionsApiDelete", interactionIdValues.First());
    }
    #endregion
}