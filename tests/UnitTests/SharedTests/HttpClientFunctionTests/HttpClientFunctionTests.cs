namespace NHS.CohortManager.Tests.UnitTests.HttpClientFunctionTests;

using Moq;
using Microsoft.Extensions.Logging;
using Common;
using System.Net;
using Moq.Protected;
using System.Security.Cryptography.X509Certificates;

[TestClass]
public class HttpClientFunctionTests
{
    private readonly Mock<ILogger<HttpClientFunction>> _logger = new();
    private readonly Mock<IHttpClientFactory> _factory = new();
    private readonly Mock<HttpMessageHandler> _httpMessageHandler = new();
    private HttpClientFunction? _function;
    private readonly string _mockUrl = "http://test.com";
    private readonly Dictionary<string, string> _mockParameters = new Dictionary<string, string>()
    {
        {"mock-key", "mock-value" }
    };
    private const string _mockContent = "mock content";
    private readonly HttpResponseMessage _mockResponse = new HttpResponseMessage
    {
        StatusCode = HttpStatusCode.OK,
        Content = new StringContent(_mockContent)
    };

    #region SendGet
    [TestMethod]
    public async Task Run_SendGetIsSuccessful_ReturnsOkResponse()
    {
        // Arrange
        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(_mockResponse);

        var httpClient = new HttpClient(_httpMessageHandler.Object);
        _factory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _function = new HttpClientFunction(_logger.Object, _factory.Object);

        // Act
        var result = await _function.SendGet(_mockUrl);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(_mockContent, result);
    }

    [TestMethod]
    public async Task Run_SendGetFails_LogsErrorWithoutNhsNumberAndThrowsException()
    {
        // Arrange
        var errorMessage = "There was an error";
        var nhsNumber = "1234567890";
        var mockUrl = $"{_mockUrl}?nhsNumber={nhsNumber}";

        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>()
            )
            .Throws(
                new Exception(errorMessage)
            );

        var httpClient = new HttpClient(_httpMessageHandler.Object);
        _factory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _function = new HttpClientFunction(_logger.Object, _factory.Object);

        // Act & Assert
        var result = await Assert.ThrowsExceptionAsync<Exception>(() => _function.SendGet(mockUrl));
        Assert.AreEqual(errorMessage, result.Message);

        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(errorMessage) && !v.ToString().Contains(nhsNumber)),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
    }

    [TestMethod]
    public async Task Run_SendGetWithParametersIsSuccessful_ReturnsOkResponse()
    {
        // Arrange
        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(_mockResponse);

        var httpClient = new HttpClient(_httpMessageHandler.Object);
        _factory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _function = new HttpClientFunction(_logger.Object, _factory.Object);

        // Act
        var result = await _function.SendGet(_mockUrl, _mockParameters);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(_mockContent, result);
    }

    [TestMethod]
    public async Task Run_SendPdsGetIsSuccessful_ReturnsOkResponse()
    {
        // Arrange
        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(
                new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK
                }
            );

        var httpClient = new HttpClient(_httpMessageHandler.Object);
        _factory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _function = new HttpClientFunction(_logger.Object, _factory.Object);

        // Act
        var result = await _function.SendPdsGet(_mockUrl);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_SendPdsGetFails_LogsErrorWithoutNhsNumberAndThrowsException()
    {
        // Arrange
        var errorMessage = "There was an error";
        var nhsNumber = "1234567890";
        var mockUrl = $"{_mockUrl}?nhsNumber={nhsNumber}";

        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>()
            )
            .Throws(
                new Exception(errorMessage)
            );

        var httpClient = new HttpClient(_httpMessageHandler.Object);
        _factory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _function = new HttpClientFunction(_logger.Object, _factory.Object);

        // Act & Assert
        var result = await Assert.ThrowsExceptionAsync<Exception>(() => _function.SendPdsGet(mockUrl));
        Assert.AreEqual(errorMessage, result.Message);

        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(errorMessage) && !v.ToString().Contains(nhsNumber)),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
    }
    #endregion

    #region SendPost
    [TestMethod]
    public async Task Run_SendPostIsSuccessful_ReturnsOkResponse()
    {
        // Arrange
        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(
                new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK
                }
            );

        var httpClient = new HttpClient(_httpMessageHandler.Object);
        _factory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _function = new HttpClientFunction(_logger.Object, _factory.Object);

        // Act
        var result = await _function.SendPost(_mockUrl, string.Empty);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_SendPostFails_LogsErrorAndThrowsException()
    {
        // Arrange
        var errorMessage = "There was an error";

        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>()
            )
            .Throws(
                new Exception(errorMessage)
            );

        var httpClient = new HttpClient(_httpMessageHandler.Object);
        _factory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _function = new HttpClientFunction(_logger.Object, _factory.Object);

        // Act & Assert
        var result = await Assert.ThrowsExceptionAsync<Exception>(() => _function.SendPost(_mockUrl, string.Empty));
        Assert.AreEqual(errorMessage, result.Message);

        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(errorMessage)),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
    }
    #endregion

    #region SendPut
    [TestMethod]
    public async Task Run_SendPutIsSuccessful_ReturnsOkResponse()
    {
        // Arrange
        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(
                new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK
                }
            );

        var httpClient = new HttpClient(_httpMessageHandler.Object);
        _factory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _function = new HttpClientFunction(_logger.Object, _factory.Object);

        // Act
        var result = await _function.SendPut(_mockUrl, string.Empty);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_SendPutFails_LogsErrorAndThrowsException()
    {
        // Arrange
        var errorMessage = "There was an error";

        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put),
                ItExpr.IsAny<CancellationToken>()
            )
            .Throws(
                new Exception(errorMessage)
            );

        var httpClient = new HttpClient(_httpMessageHandler.Object);
        _factory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _function = new HttpClientFunction(_logger.Object, _factory.Object);

        // Act & Assert
        var result = await Assert.ThrowsExceptionAsync<Exception>(() => _function.SendPut(_mockUrl, string.Empty));
        Assert.AreEqual(errorMessage, result.Message);

        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(errorMessage)),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
    }
    #endregion

    #region SendDelete
    [TestMethod]
    public async Task Run_SendDeleteIsSuccessful_ReturnsTrue()
    {
        // Arrange
        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Delete),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(
                new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK
                }
            );

        var httpClient = new HttpClient(_httpMessageHandler.Object);
        _factory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _function = new HttpClientFunction(_logger.Object, _factory.Object);

        // Act
        var result = await _function.SendDelete(_mockUrl);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(true, result);
    }

    [TestMethod]
    public async Task Run_SendDeleteFails_ReturnsFalse()
    {
        // Arrange
        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Delete),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(
                new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError
                }
            );

        var httpClient = new HttpClient(_httpMessageHandler.Object);
        _factory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _function = new HttpClientFunction(_logger.Object, _factory.Object);

        // Act
        var result = await _function.SendDelete(_mockUrl);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(false, result);
    }

    [TestMethod]
    public async Task Run_SendDeleteThrowsError_LogsErrorAndThrowsException()
    {
        // Arrange
        var errorMessage = "There was an error";

        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Delete),
                ItExpr.IsAny<CancellationToken>()
            )
            .Throws(
                new Exception(errorMessage)
            );

        var httpClient = new HttpClient(_httpMessageHandler.Object);
        _factory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _function = new HttpClientFunction(_logger.Object, _factory.Object);

        // Act & Assert
        var result = await Assert.ThrowsExceptionAsync<Exception>(() => _function.SendDelete(_mockUrl));
        Assert.AreEqual(errorMessage, result.Message);

        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(errorMessage)),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
    }
    #endregion

    #region GetResponseText
    [TestMethod]
    public async Task Run_GetResponseText_ReturnsContentAsString()
    {
        // Arrange
        _function = new HttpClientFunction(_logger.Object, _factory.Object);

        // Act
        var result = await _function.GetResponseText(_mockResponse);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(_mockContent, result);
    }
    #endregion

    #region NEMS Methods

    [TestMethod]
    public void GenerateNemsJwtToken_ValidParameters_ReturnsJwtToken()
    {
        // Arrange
        _function = new HttpClientFunction(_logger.Object, _factory.Object);
        var asid = "test-asid";
        var audience = "https://nems.endpoint";
        var scope = "patient/Subscription.write";

        // Act
        var result = _function.GenerateNemsJwtToken(asid, audience, scope);

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
    public void GenerateNemsJwtToken_ContainsExpectedClaims_ValidatesPayload()
    {
        // Arrange
        _function = new HttpClientFunction(_logger.Object, _factory.Object);
        var asid = "test-asid-123";
        var audience = "https://test.nhs.uk";
        var scope = "patient/Subscription.write";

        // Act
        var result = _function.GenerateNemsJwtToken(asid, audience, scope);

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
    public void GenerateNemsJwtToken_HasValidTimestamps_ExpiresInOneHour()
    {
        // Arrange
        _function = new HttpClientFunction(_logger.Object, _factory.Object);
        var beforeCall = DateTimeOffset.UtcNow;

        // Act
        var result = _function.GenerateNemsJwtToken("asid", "aud", "scope");

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

    [TestMethod]
    public async Task SendNemsPost_SuccessfulPost_ReturnsOkResponse()
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

        _function = new HttpClientFunction(_logger.Object, _factory.Object);

        // Act
        var result = await _function.SendNemsPost(_mockUrl, _mockContent, "jwt-token", "from-asid", "to-asid");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
    }

    [TestMethod]
    public async Task SendNemsPost_FailedPost_ReturnsErrorResponse()
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

        _function = new HttpClientFunction(_logger.Object, _factory.Object);

        // Act
        var result = await _function.SendNemsPost(_mockUrl, _mockContent, "jwt-token", "from-asid", "to-asid");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task SendNemsPost_ExceptionThrown_ThrowsException()
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

        _function = new HttpClientFunction(_logger.Object, _factory.Object);

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<Exception>(() => 
            _function.SendNemsPost(_mockUrl, _mockContent, "jwt-token", "from-asid", "to-asid"));
        
        Assert.AreEqual(errorMessage, exception.Message);
    }

    [TestMethod]
    public async Task SendNemsDelete_SuccessfulDelete_ReturnsOkResponse()
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

        _function = new HttpClientFunction(_logger.Object, _factory.Object);

        // Act
        var result = await _function.SendNemsDelete(_mockUrl, "jwt-token", "from-asid", "to-asid");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task SendNemsDelete_FailedDelete_ReturnsErrorResponse()
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

        _function = new HttpClientFunction(_logger.Object, _factory.Object);

        // Act
        var result = await _function.SendNemsDelete(_mockUrl, "jwt-token", "from-asid", "to-asid");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
    }

    [TestMethod]
    public async Task SendGet_NonOkResponse_ReturnsEmptyString()
    {
        // Arrange
        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        var httpClient = new HttpClient(_httpMessageHandler.Object);
        _factory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _function = new HttpClientFunction(_logger.Object, _factory.Object);

        // Act
        var result = await _function.SendGet(_mockUrl);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void RemoveURLQueryString_UrlWithQueryString_RemovesQueryString()
    {
        // Arrange
        _function = new HttpClientFunction(_logger.Object, _factory.Object);
        var urlWithQuery = "https://example.com/api?param1=value1&param2=value2";

        // Act - Using reflection to access private method
        var method = typeof(HttpClientFunction).GetMethod("RemoveURLQueryString", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (string)method.Invoke(null, new object[] { urlWithQuery });

        // Assert
        Assert.AreEqual("https://example.com/api", result);
    }

    [TestMethod]
    public void RemoveURLQueryString_UrlWithoutQueryString_ReturnsOriginalUrl()
    {
        // Arrange
        _function = new HttpClientFunction(_logger.Object, _factory.Object);
        var urlWithoutQuery = "https://example.com/api";

        // Act
        var method = typeof(HttpClientFunction).GetMethod("RemoveURLQueryString", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (string)method.Invoke(null, new object[] { urlWithoutQuery });

        // Assert
        Assert.AreEqual("https://example.com/api", result);
    }

    [TestMethod]
    public void RemoveURLQueryString_EmptyString_ReturnsEmptyString()
    {
        // Arrange
        _function = new HttpClientFunction(_logger.Object, _factory.Object);

        // Act
        var method = typeof(HttpClientFunction).GetMethod("RemoveURLQueryString", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (string)method.Invoke(null, new object[] { string.Empty });

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void RemoveURLQueryString_NullString_ReturnsNull()
    {
        // Arrange
        _function = new HttpClientFunction(_logger.Object, _factory.Object);

        // Act
        var method = typeof(HttpClientFunction).GetMethod("RemoveURLQueryString", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (string)method.Invoke(null, new object[] { null });

        // Assert
        Assert.IsNull(result);
    }

    #endregion
}
