namespace NHS.CohortManager.Tests.UnitTests.ServiceNowMessageHandlerTests;

using Moq;
using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NHS.CohortManager.ServiceNowIntegrationService;

[TestClass]
public class ServiceNowMessageHandlerTests
{
    private readonly Mock<ILogger<ServiceNowMessageHandler>> _loggerMock = new();
    private readonly Mock<IOptions<ServiceNowMessageHandlerConfig>> _configMock = new();
    private readonly CreateResponse _createResponse = new();
    private readonly Mock<FunctionContext> _contextMock = new();
    private readonly Mock<HttpRequestData> _httpRequestMock;
    private readonly ServiceNowMessageHandler _function;
    private readonly Mock<IHttpClientFunction> _httpClientFunctionMock = new();

    public ServiceNowMessageHandlerTests()
    {
        _httpRequestMock = new Mock<HttpRequestData>(_contextMock.Object);
        _httpRequestMock.Setup(r => r.CreateResponse()).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(_contextMock.Object);
            response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
            response.SetupProperty(r => r.StatusCode);
            response.SetupProperty(r => r.Body, new MemoryStream());
            return response.Object;
        });

        _configMock.Setup(x => x.Value).Returns(new ServiceNowMessageHandlerConfig
        {
            EndpointPath = "api/now/table/incident",
            Definition = "change_request",
            AccessToken = "initial-token",
            UpdateEndpoint = "dummy-endpoint",
            ServiceNowUpdateUrl = "instance.service-now.com",
            Profile = "prod"
        });

        _function = new ServiceNowMessageHandler(
            _httpClientFunctionMock.Object,
            _loggerMock.Object,
            _configMock.Object,
            _createResponse
        );
    }

    [TestMethod]
    public async Task HandleSendServiceNowMessage_ReturnsSuccess_AfterTokenRefresh()
    {
        // Arrange
        var sysId = "sysid-123";
        var requestBodyJson = JsonSerializer.Serialize(new ServiceNowRequestModel
        {
            WorkNotes = "Retry this",
            State = 2
        });
        var requestBodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBodyJson));
        _httpRequestMock.Setup(r => r.Body).Returns(requestBodyStream);

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        _httpClientFunctionMock.Setup(x => x.SendServiceNowPut($"{_configMock.Object.Value.ServiceNowUpdateUrl}/{sysId}", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _function.SendServiceNowMessage(_httpRequestMock.Object, sysId);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task HandleSendServiceNowMessage_ReturnsBadRequest_WhenMissingWorkNotes()
    {
        // Arrange
        var sysId = "sysid-123";
        var invalidBody = JsonSerializer.Serialize(new
        {
            WorkNotes = "",
            State = 2
        });

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(invalidBody));
        _httpRequestMock.Setup(r => r.Body).Returns(stream);

        // Act
        var result = await _function.SendServiceNowMessage(_httpRequestMock.Object, sysId);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }


    [TestMethod]
    public async Task HandleSendServiceNowMessage_ReturnsBadRequest_WhenBodyIsEmpty()
    {
        // Arrange
        var sysId = "sysid-123";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(""));
        _httpRequestMock.Setup(r => r.Body).Returns(stream);

        // Act
        var result = await _function.SendServiceNowMessage(_httpRequestMock.Object, sysId);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }
}
