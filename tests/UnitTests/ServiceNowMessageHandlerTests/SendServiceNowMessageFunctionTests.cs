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
using Model;
using Model.Enums;

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
    public async Task Run_WhenRequestBodyIsAddRequestInProgressAndUpdateSuceeds_ReturnsOK()
    {
        // Arrange
        var caseNumber = "CS123";
        var request = new SendServiceNowMessageRequestBody
        {
            MessageType = ServiceNowMessageType.AddRequestInProgress
        };
        var requestBodyJson = JsonSerializer.Serialize(request);
        var requestBodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBodyJson));
        _httpRequestMock.Setup(r => r.Body).Returns(requestBodyStream);

        var updateResponse = new HttpResponseMessage(HttpStatusCode.OK);
        _serviceNowClientMock.Setup(x => x.SendUpdate(
                caseNumber,
                string.Format(string.Format(ServiceNowMessageTemplates.AddRequestInProgressMessageTemplate, caseNumber)),
                false))
            .ReturnsAsync(updateResponse)
            .Verifiable();

        // Act
        var result = await _function.Run(_httpRequestMock.Object, caseNumber);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _serviceNowClientMock.Verify();
        _serviceNowClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_WhenRequestBodyIsUnableToAddParticipantAndUpdateSuceeds_ReturnsOK()
    {
        // Arrange
        var caseNumber = "CS123";
        var request = new SendServiceNowMessageRequestBody
        {
            MessageType = ServiceNowMessageType.UnableToAddParticipant
        };
        var requestBodyJson = JsonSerializer.Serialize(request);
        var requestBodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBodyJson));
        _httpRequestMock.Setup(r => r.Body).Returns(requestBodyStream);

        var updateResponse = new HttpResponseMessage(HttpStatusCode.OK);
        _serviceNowClientMock.Setup(x => x.SendUpdate(
                caseNumber,
                string.Format(string.Format(ServiceNowMessageTemplates.UnableToAddParticipantMessageTemplate, caseNumber)),
                true))
            .ReturnsAsync(updateResponse)
            .Verifiable();

        // Act
        var result = await _function.Run(_httpRequestMock.Object, caseNumber);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _serviceNowClientMock.Verify();
        _serviceNowClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_WhenRequestBodyIsSuccessAndUpdateSuceeds_ReturnsOK()
    {
        // Arrange
        var caseNumber = "CS123";
        var request = new SendServiceNowMessageRequestBody
        {
            MessageType = ServiceNowMessageType.Success
        };
        var requestBodyJson = JsonSerializer.Serialize(request);
        var requestBodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBodyJson));
        _httpRequestMock.Setup(r => r.Body).Returns(requestBodyStream);

        var updateResponse = new HttpResponseMessage(HttpStatusCode.OK);
        _serviceNowClientMock.Setup(x => x.SendResolution(caseNumber,
                string.Format(string.Format(ServiceNowMessageTemplates.SuccessMessageTemplate, caseNumber))))
            .ReturnsAsync(updateResponse)
            .Verifiable();

        // Act
        var result = await _function.Run(_httpRequestMock.Object, caseNumber);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _serviceNowClientMock.Verify();
        _serviceNowClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    [DataRow(HttpStatusCode.Unauthorized)]
    [DataRow(HttpStatusCode.InternalServerError)]
    public async Task Run_WhenRequestBodyIsValidButUpdateReturnsFailureResponse_ReturnsInternalServerError(HttpStatusCode statusCode)
    {
        // Arrange
        var caseNumber = "CS123";
        var request = new SendServiceNowMessageRequestBody
        {
            MessageType = ServiceNowMessageType.AddRequestInProgress
        };
        var requestBodyJson = JsonSerializer.Serialize(request);
        var requestBodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBodyJson));
        _httpRequestMock.Setup(r => r.Body).Returns(requestBodyStream);

        var updateResponse = new HttpResponseMessage(statusCode);
        _serviceNowClientMock.Setup(x => x.SendUpdate(
                caseNumber,
                string.Format(string.Format(ServiceNowMessageTemplates.AddRequestInProgressMessageTemplate, caseNumber)),
                false))
            .ReturnsAsync(updateResponse)
            .Verifiable();

        // Act
        var result = await _function.Run(_httpRequestMock.Object, caseNumber);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
        _serviceNowClientMock.Verify();
        _serviceNowClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_WhenRequestBodyIsValidButUpdateReturnsNull_ReturnsInternalServerError()
    {
        // Arrange
        var caseNumber = "CS123";
        var request = new SendServiceNowMessageRequestBody
        {
            MessageType = ServiceNowMessageType.AddRequestInProgress
        };
        var requestBodyJson = JsonSerializer.Serialize(request);
        var requestBodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBodyJson));
        _httpRequestMock.Setup(r => r.Body).Returns(requestBodyStream);

        var updateResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
        _serviceNowClientMock.Setup(x => x.SendUpdate(
                caseNumber,
                string.Format(string.Format(ServiceNowMessageTemplates.AddRequestInProgressMessageTemplate, caseNumber)),
                false))
            .ReturnsAsync((HttpResponseMessage)null)
            .Verifiable();

        // Act
        var result = await _function.Run(_httpRequestMock.Object, caseNumber);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
        _serviceNowClientMock.Verify();
        _serviceNowClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_WhenRequestBodyIsValidButUpdateThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var caseNumber = "CS123";
        var request = new SendServiceNowMessageRequestBody
        {
            MessageType = ServiceNowMessageType.AddRequestInProgress
        };
        var requestBodyJson = JsonSerializer.Serialize(request);
        var requestBodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBodyJson));
        _httpRequestMock.Setup(r => r.Body).Returns(requestBodyStream);

        var updateResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
        _serviceNowClientMock.Setup(x => x.SendUpdate(
                caseNumber,
                string.Format(string.Format(ServiceNowMessageTemplates.AddRequestInProgressMessageTemplate, caseNumber)),
                false))
            .ThrowsAsync(new HttpRequestException())
            .Verifiable();

        // Act
        var result = await _function.Run(_httpRequestMock.Object, caseNumber);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
        _serviceNowClientMock.Verify();
        _serviceNowClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_WhenRequestBodyIsInvalid_ReturnsBadRequest()
    {
        // Arrange
        var caseNumber = "CS123";
        var request = new
        {
            MessageType = ""
        };
        var requestBodyJson = JsonSerializer.Serialize(request);
        var requestBodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBodyJson));
        _httpRequestMock.Setup(r => r.Body).Returns(requestBodyStream);

        // Act
        var result = await _function.Run(_httpRequestMock.Object, caseNumber);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _serviceNowClientMock.VerifyNoOtherCalls();
    }

    [DataTestMethod]
    [DataRow(ServiceNowMessageTemplates.UnableToAddParticipantMessageTemplate, "Action needed for {0}: We could not add this participant. This may be because the details entered do not match NHS records. Please check the participant's information and the reason for adding, then submit a new request if appropriate.")]
    [DataRow(ServiceNowMessageTemplates.AddRequestInProgressMessageTemplate, "Update on {0}: Thank you for your request to add a participant to the breast screening programme. There may be a slight delay while the information continues to be validated. Please allow time for this process to complete before following up on your request.")]
    [DataRow(ServiceNowMessageTemplates.SuccessMessageTemplate, "Update on {0}: Validation checks are complete and your request has been sent to BS Select for processing.")]
    public void ServiceNowMessageTemplates_ContainsExpectedWording(string actualTemplate, string expectedWording)
    {
        // Assert
        StringAssert.Contains(actualTemplate, expectedWording);
    }
}
