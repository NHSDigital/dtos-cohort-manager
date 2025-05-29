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
using NHS.CohortManager.ServiceNowMessageService;

[TestClass]
public class ServiceNowMessageTests
{
    private Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private HttpClient _httpClient;
    private Mock<ILogger<ServiceNowMessage>> _mockLogger;
    private IConfiguration _configuration;

    [TestInitialize]
    public void Setup()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);

        _mockLogger = new Mock<ILogger<ServiceNowMessage>>();

        var configValues = new Dictionary<string, string>
        {
            { "ServiceNowBaseUrl", "https://example.com" },
            { "Profile", "ebz" },
            { "Definition", "CohortCaseUpdate" },
            { "AccessToken", "dummy-token" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        // Set environment variables for testing
        Environment.SetEnvironmentVariable("ServiceNowBaseUrl", "https://example.com");
        Environment.SetEnvironmentVariable("Profile", "ebz");
        Environment.SetEnvironmentVariable("Definition", "CohortCaseUpdate");
        Environment.SetEnvironmentVariable("AccessToken", "dummy-token");
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
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put &&
                    req.RequestUri.ToString().Contains("/ebz/CohortCaseUpdate/123") &&
                    req.Headers.Authorization.Scheme == "Bearer" &&
                    req.Headers.Authorization.Parameter == "dummy-token"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse)
            .Verifiable();

        var service = new ServiceNowMessage(_httpClient, _configuration, _mockLogger.Object);

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
    [ExpectedException(typeof(HttpRequestException))]
    public async Task SendServiceNowMessage_ShouldThrowException_WhenRequestFails()
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

        var service = new ServiceNowMessage(_httpClient, _configuration, _mockLogger.Object);

        // Act
        await service.SendServiceNowMessage("123", "Should fail");

        // Assert – handled by ExpectedException
    }
}
