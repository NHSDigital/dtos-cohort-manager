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
    private readonly Dictionary<string, string> _mockHeaders = new Dictionary<string, string>()
    {
        {"Mock-Header", "mock-header" }
    };

    #region GetAsync
    [TestMethod]
    public async Task Run_GetAsyncIsSuccessful_ReturnsOkResponse()
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
        var result = await _function.GetAsync(_mockUrl, _mockHeaders);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_GetAsyncFails_LogsErrorWithoutNhsNumberAndThrowsException()
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
        var result = await Assert.ThrowsExceptionAsync<Exception>(() => _function.GetAsync(mockUrl, _mockHeaders));
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

    #region PostAsync
    [TestMethod]
    public async Task Run_PostAsyncIsSuccessful_ReturnsOkResponse()
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
        var result = await _function.PostAsync(_mockUrl, string.Empty);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_PostAsyncFails_LogsErrorAndThrowsException()
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
        var result = await Assert.ThrowsExceptionAsync<Exception>(() => _function.PostAsync(_mockUrl, string.Empty));
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

    #region PutAsync
    [TestMethod]
    public async Task Run_PutAsyncIsSuccessful_ReturnsOkResponse()
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
        var result = await _function.PutAsync(_mockUrl, string.Empty);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_PutAsyncFails_LogsErrorAndThrowsException()
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
        var result = await Assert.ThrowsExceptionAsync<Exception>(() => _function.PutAsync(_mockUrl, string.Empty));
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

    #region DeleteAsync
    [TestMethod]
    public async Task Run_DeleteAsyncIsSuccessful_ReturnsOkResponse()
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
        var result = await _function.DeleteAsync(_mockUrl);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_DeleteAsyncFails_LogsErrorAndThrowsException()
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
        var result = await Assert.ThrowsExceptionAsync<Exception>(() => _function.DeleteAsync(_mockUrl));
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
}
