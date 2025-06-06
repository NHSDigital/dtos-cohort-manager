namespace NHS.CohortManager.Tests.UnitTests.SendServiceNowMessageTests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Text;
using Moq.Protected;
using Common;
using NHS.CohortManager.ServiceNowIntegrationService;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;




[TestClass]
public class ServiceNowMessageHandlerTests
{
    private Mock<IHttpClientFactory> _httpClientFactoryMock;
    private Mock<ILogger<ServiceNowMessageHandler>> _loggerMock;
    private Mock<IOptions<SendServiceNowMsgConfig>> _optionsMock;
    private Mock<ICreateResponse> _createResponseMock;
    private Mock<HttpRequestData> _httpRequestMock;
    private Mock<FunctionContext> _contextMock;
    private HttpClient _httpClient;
    private ServiceNowMessageHandler _handler;

    [TestInitialize]
    public void Setup()
    {
        var handler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(handler.Object);

        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(_httpClient);

        _loggerMock = new Mock<ILogger<ServiceNowMessageHandler>>();
        _createResponseMock = new Mock<ICreateResponse>();
        _optionsMock = new Mock<IOptions<SendServiceNowMsgConfig>>();
        _contextMock = new Mock<FunctionContext>();
        _httpRequestMock = new Mock<HttpRequestData>(_contextMock.Object);

        _optionsMock.Setup(x => x.Value).Returns(new SendServiceNowMsgConfig
        {
            EndpointPath = "api/now/table/incident",
            Definition = "change_request",
            AccessToken = "dummy-token",
            UpdateEndpoint = "endpoint",
            ServiceNowBaseUrl = "instance.service-now.com",
            Profile = "prod",
        });

        _handler = new ServiceNowMessageHandler(
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            _optionsMock.Object,
            _createResponseMock.Object
        );
    }

    [TestMethod]
    public async Task Run_ReturnsBadRequest_WhenBodyIsEmpty()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("")); // simulate empty body
        _httpRequestMock.Setup(r => r.Body).Returns(stream);
        var expectedResponse = new Mock<HttpResponseData>(_contextMock.Object).Object;

        _createResponseMock
            .Setup(x => x.CreateHttpResponse(HttpStatusCode.BadRequest, _httpRequestMock.Object, It.IsAny<string>()))
            .Returns(expectedResponse);

        var result = await _handler.Run(_httpRequestMock.Object, "base", "profile", "sysid");

        Assert.AreEqual(expectedResponse, result);
    }

    [TestMethod]
    public async Task Run_ReturnsInternalServerError_OnInvalidJson()
    {
        // Arrange
        var invalidJson = "{ not json ";
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(invalidJson));
        _httpRequestMock.Setup(r => r.Body).Returns(bodyStream);

        var expectedResponse = new Mock<HttpResponseData>(_contextMock.Object).Object;

        _createResponseMock
            .Setup(x => x.CreateHttpResponse(HttpStatusCode.InternalServerError, _httpRequestMock.Object, "ServiceNow update failed."))
            .Returns(expectedResponse);

        // Act
        var result = await _handler.Run(_httpRequestMock.Object, "base", "profile", "sysid");

        // Assert
        Assert.AreEqual(expectedResponse, result);
    }

}




