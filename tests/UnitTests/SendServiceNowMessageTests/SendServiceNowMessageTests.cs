namespace NHS.CohortManager.Tests.UnitTests.SendServiceNowMessageTests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Text;
using Moq.Protected;
using Common;
using NHS.CohortManager.ServiceNowMessageService;

[TestClass]
public class ServiceNowMessageTests
{
    private Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private HttpClient _httpClient;
    private Mock<ILogger<SendServiceNowMessageFunction>> _mockLogger;
    private IConfiguration _configuration;
    private Mock<IHttpClientFactory> _mockHttpClientFactory;
    private Mock<ILoggerFactory> _mockLoggerFactory;
    private Mock<ICreateResponse> _createResponse;

    [TestInitialize]
    public void Setup()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://example.com")
        };

        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(_httpClient);

        _mockLoggerFactory = new Mock<ILoggerFactory>();
        var mockLogger = new Mock<ILogger<SendServiceNowMessageFunction>>();
        _mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(mockLogger.Object);

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "ServiceNowBaseUrl", "https://example.com" },
                { "Profile", "ebz" },
                { "Definition", "CohortCaseUpdate" },
                { "AccessToken", "dummy-token" }
            })
            .Build();

        _createResponse = new Mock<ICreateResponse>();
    }



    [TestMethod]
    public async Task SendServiceNowMessage_ShouldReturnSuccess_WhenValidRequestIsMade()
    {
        // Arrange
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{ \"result\": \"success\" }", Encoding.UTF8, "application/json")
        };

    _mockHttpMessageHandler
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"result\":\"success\"}")
        });

        var service = new SendServiceNowMessageFunction(_mockHttpClientFactory.Object, _mockLoggerFactory.Object, _configuration,
    _createResponse.Object);

        // Act
        var result = await service.SendServiceNowMessage("123", "Work notes test");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        var body = await result.Content.ReadAsStringAsync();
        Assert.IsTrue(body.Contains("success"));

        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [TestMethod]
    public async Task SendServiceNowMessage_ShouldReturnInternalServerError_WhenRequestFails()
    {
        // Arrange
        var failedResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Bad Request")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(failedResponse)
            .Verifiable();

        var service = new SendServiceNowMessageFunction(
            _mockHttpClientFactory.Object,
            _mockLoggerFactory.Object,
            _configuration,
            _createResponse.Object);

        // Act
        var result = await service.SendServiceNowMessage("123", "Should fail");

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
        var content = await result.Content.ReadAsStringAsync();
        Assert.IsTrue(content.Contains("error", StringComparison.OrdinalIgnoreCase));
    }

}
