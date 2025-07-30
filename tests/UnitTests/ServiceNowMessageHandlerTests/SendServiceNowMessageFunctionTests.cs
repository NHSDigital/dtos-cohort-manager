namespace NHS.CohortManager.Tests.UnitTests.ServiceNowMessageHandlerTests;

using Moq;
using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NHS.CohortManager.ServiceNowIntegrationService;
using NHS.CohortManager.ServiceNowIntegrationService.Models;

[TestClass]
public class SendServiceNowMessageFunctionTests
{
    private readonly Mock<ILogger<SendServiceNowMessageFunction>> _loggerMock = new();
    private readonly CreateResponse _createResponse = new();
    private readonly Mock<IServiceNowClient> _serviceNowClientMock = new();
    private readonly Mock<FunctionContext> _contextMock = new();
    private readonly Mock<HttpRequestData> _httpRequestMock;
    private readonly SendServiceNowMessageFunction _function;

    public SendServiceNowMessageFunctionTests()
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

        _function = new SendServiceNowMessageFunction(
            _loggerMock.Object,
            _createResponse,
            _serviceNowClientMock.Object
        );
    }

    [TestMethod]
    public async Task Run_WhenRequestBodyIsValidAndUpdateSuceeds_ReturnsOK()
    {
        // Arrange
        var sysId = "sysid-123";
        var request = new SendServiceNowMessageRequestBody
        {
            State = 2,
            WorkNotes = "Retry this"
        };
        var requestBodyJson = JsonSerializer.Serialize(request);
        var requestBodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBodyJson));
        _httpRequestMock.Setup(r => r.Body).Returns(requestBodyStream);

        var updateResponse = new HttpResponseMessage(HttpStatusCode.OK);
        _serviceNowClientMock.Setup(x => x.SendUpdate(sysId,
                It.Is<ServiceNowUpdateRequestBody>(x => x.State == request.State && x.WorkNotes == request.WorkNotes)))
            .ReturnsAsync(updateResponse);

        // Act
        var result = await _function.Run(_httpRequestMock.Object, sysId);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    [DataRow(HttpStatusCode.Unauthorized)]
    [DataRow(HttpStatusCode.InternalServerError)]
    public async Task Run_WhenRequestBodyIsValidButUpdateReturnsFailureResponse_ReturnsInternalServerError(HttpStatusCode statusCode)
    {
        // Arrange
        var sysId = "sysid-123";
        var request = new SendServiceNowMessageRequestBody
        {
            State = 2,
            WorkNotes = "Retry this"
        };
        var requestBodyJson = JsonSerializer.Serialize(request);
        var requestBodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBodyJson));
        _httpRequestMock.Setup(r => r.Body).Returns(requestBodyStream);

        var updateResponse = new HttpResponseMessage(statusCode);
        _serviceNowClientMock.Setup(x => x.SendUpdate(sysId,
                It.Is<ServiceNowUpdateRequestBody>(x => x.State == request.State && x.WorkNotes == request.WorkNotes)))
            .ReturnsAsync(updateResponse);

        // Act
        var result = await _function.Run(_httpRequestMock.Object, sysId);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_WhenRequestBodyIsValidButUpdateReturnsNull_ReturnsInternalServerError()
    {
        // Arrange
        var sysId = "sysid-123";
        var request = new SendServiceNowMessageRequestBody
        {
            State = 2,
            WorkNotes = "Retry this"
        };
        var requestBodyJson = JsonSerializer.Serialize(request);
        var requestBodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBodyJson));
        _httpRequestMock.Setup(r => r.Body).Returns(requestBodyStream);

        var updateResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
        _serviceNowClientMock.Setup(x => x.SendUpdate(sysId,
                It.Is<ServiceNowUpdateRequestBody>(x => x.State == request.State && x.WorkNotes == request.WorkNotes)))
            .ReturnsAsync((HttpResponseMessage)null);

        // Act
        var result = await _function.Run(_httpRequestMock.Object, sysId);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_WhenRequestBodyIsValidButUpdateThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var sysId = "sysid-123";
        var request = new SendServiceNowMessageRequestBody
        {
            State = 2,
            WorkNotes = "Retry this"
        };
        var requestBodyJson = JsonSerializer.Serialize(request);
        var requestBodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBodyJson));
        _httpRequestMock.Setup(r => r.Body).Returns(requestBodyStream);

        var updateResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
        _serviceNowClientMock.Setup(x => x.SendUpdate(sysId,
                It.Is<ServiceNowUpdateRequestBody>(x => x.State == request.State && x.WorkNotes == request.WorkNotes)))
            .ThrowsAsync(new HttpRequestException());

        // Act
        var result = await _function.Run(_httpRequestMock.Object, sysId);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_WhenRequestBodyIsInvalidJson_ReturnsBadRequest()
    {
        // Arrange
        var sysId = "sysid-123";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("Invalid json"));
        _httpRequestMock.Setup(r => r.Body).Returns(stream);

        // Act
        var result = await _function.Run(_httpRequestMock.Object, sysId);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _serviceNowClientMock.VerifyNoOtherCalls();
    }
}
