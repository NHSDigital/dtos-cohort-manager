namespace NHS.CohortManager.Tests.UnitTests.HttpClientFunctionTests;

using Moq;
using Microsoft.Extensions.Logging;
using Common;
using System.Net;
using Moq.Protected;

[TestClass]
public class HttpClientFunctionTests
{
    private readonly Mock<ILogger<HttpClientFunction>> _logger = new();
    private readonly Mock<IHttpClientFactory> _factory = new();
    private readonly Mock<HttpMessageHandler> _httpMessageHandler = new();
    private HttpClientFunction? _function;
    private readonly string _mockUrl = "http://test.com";

    private readonly string _mockToken = "some-fake-token";
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
        var result = await _function.SendPdsGet(_mockUrl, _mockToken);

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
        var result = await Assert.ThrowsExceptionAsync<Exception>(() => _function.SendPdsGet(mockUrl, _mockToken));
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

    #region RemoveURLQueryString
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