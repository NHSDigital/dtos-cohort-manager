namespace NHS.CohortManager.Tests.UnitTests.ServiceNowMessageHandlerTests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NHS.CohortManager.ServiceNowMessageService.Models;
using NHS.CohortManager.ServiceNowIntegrationService;
using Moq.Protected;

[TestClass]
public class ServiceNowMessageHandlerTests
{
    private Mock<IHttpClientFactory> _httpClientFactoryMock;
    private Mock<ILogger<SendServiceNowMessageFunction>> _loggerMock;
    private Mock<IOptions<ServiceNowMessageHandlerConfig>> _optionsMock;
    private Mock<ICreateResponse> _createResponseMock;
    private Mock<FunctionContext> _contextMock;
    private Mock<HttpRequestData> _httpRequestMock;
    private SendServiceNowMessageFunction _handler;
    private Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private HttpClient _httpClient;

    [TestInitialize]
    public void Setup()
    {
        _contextMock = new Mock<FunctionContext>();
        _httpRequestMock = new Mock<HttpRequestData>(_contextMock.Object);

        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);

        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(_httpClient);

        _loggerMock = new Mock<ILogger<SendServiceNowMessageFunction>>();
        _optionsMock = new Mock<IOptions<ServiceNowMessageHandlerConfig>>();
        _optionsMock.Setup(x => x.Value).Returns(new ServiceNowMessageHandlerConfig
        {
            EndpointPath = "api/now/table/incident",
            Definition = "change_request",
            AccessToken = "initial-token",
            UpdateEndpoint = "dummy-endpoint",
            ServiceNowBaseUrl = "instance.service-now.com",
            Profile = "prod"
        });

        _createResponseMock = new Mock<ICreateResponse>();

        _handler = new SendServiceNowMessageFunction(
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            _optionsMock.Object,
            _createResponseMock.Object
        );
    }

    [TestMethod]
    public async Task HandleSendServiceNowMessage_ReturnsSuccess_AfterTokenRefresh()
    {
        var requestBody = JsonSerializer.Serialize(new ServiceNowRequestModel
        {
            WorkNotes = "Retry this",
            State = 2
        });

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        _httpRequestMock.Setup(r => r.Body).Returns(stream);
        _httpRequestMock.Setup(r => r.Method).Returns("PUT");

        _httpMessageHandlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Success")
            });

        var expectedResponse = new Mock<HttpResponseData>(_contextMock.Object).Object;

        _createResponseMock
            .Setup(r => r.CreateHttpResponse(HttpStatusCode.OK, _httpRequestMock.Object, "Success"))
            .Returns(expectedResponse);

        var result = await _handler.Run(_httpRequestMock.Object, "base", "profile", "sysid");

        Assert.AreEqual(expectedResponse, result);
    }

    [TestMethod]
    public async Task HandleSendServiceNowMessage_ReturnsBadRequest_WhenMissingWorkNotes()
    {
        // Arrange: valid JSON with empty WorkNotes
        var invalidBody = JsonSerializer.Serialize(new
        {
            WorkNotes = "", // Simulates missing or empty value
            State = 2
        });

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(invalidBody));
        _httpRequestMock.Setup(r => r.Body).Returns(stream);
        _httpRequestMock.Setup(r => r.Method).Returns("PUT");

        var expectedResponse = new Mock<HttpResponseData>(_contextMock.Object).Object;

        _createResponseMock
            .Setup(x => x.CreateHttpResponse(HttpStatusCode.BadRequest, _httpRequestMock.Object, "Invalid request payload."))
            .Returns(expectedResponse);

        // Act
        var result = await _handler.Run(_httpRequestMock.Object, "base", "profile", "sysid");

        // Assert
        Assert.AreEqual(expectedResponse, result);
    }


    [TestMethod]
    public async Task HandleSendServiceNowMessage_ReturnsBadRequest_WhenBodyIsEmpty()
    {
        // Arrange: empty body
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(""));
        _httpRequestMock.Setup(r => r.Body).Returns(stream);
        _httpRequestMock.Setup(r => r.Method).Returns("PUT"); // Required for routing

        var expectedResponse = new Mock<HttpResponseData>(_contextMock.Object).Object;

        _createResponseMock
            .Setup(x => x.CreateHttpResponse(HttpStatusCode.BadRequest, _httpRequestMock.Object, "Request body is missing or empty."))
            .Returns(expectedResponse);

        // Act
        var result = await _handler.Run(_httpRequestMock.Object, "base", "profile", "sysid");

        // Assert
        Assert.AreEqual(expectedResponse, result);
    }

}


