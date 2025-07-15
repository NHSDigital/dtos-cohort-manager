namespace NHS.CohortManager.Tests.UnitTests.CreateExceptionTests;

using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;
using Model;
using Common;
using Data.Database;
using NHS.CohortManager.ExceptionService;
using System.Text.Json;
using System.Text;
using Microsoft.Identity.Client;
using Azure.Messaging.ServiceBus;
//using Microsoft.Extensions.Logging;

[TestClass]
public class CreateExceptionTests
{
    private readonly Mock<ILogger<CreateException>> _logger = new();
    private readonly Mock<FunctionContext> _context = new();
    private readonly Mock<HttpRequestData> _request;
    private readonly ValidationException _requestBody;
    private readonly CreateException _function;
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<IValidationExceptionData> _validationExceptionData = new();
    private readonly Mock<ServiceBusMessageActions> serviceBusMessageActions = new();


    ServiceBusReceivedMessage serviceBusMessage;

    public CreateExceptionTests()
    {
        _request = new Mock<HttpRequestData>(_context.Object);

        _requestBody = new ValidationException() { ExceptionId = 1 };

        _function = new CreateException(_logger.Object, _validationExceptionData.Object, _createResponse.Object);

        _request.Setup(r => r.CreateResponse()).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(_context.Object);
            response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
            response.SetupProperty(r => r.StatusCode);
            response.SetupProperty(r => r.Body, new MemoryStream());
            return response.Object;
        });

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string ResponseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                response.WriteString(ResponseBody);
                return response;
            });

        var json = JsonSerializer.Serialize(_requestBody);

        var serviceBusMessageBody = JsonSerializer.Serialize(new ValidationException());
        serviceBusMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
              body: new BinaryData(serviceBusMessageBody),
              messageId: $"id-{1}",
              partitionKey: "illustrative-partitionKey",
              correlationId: "illustrative-correlationId",
              contentType: "illustrative-contentType",
              replyTo: "illustrative-replyTo"
              );

        SetUpRequestBody(json);
    }

    [TestMethod]
    public async Task Run_EmptyRequest_ReturnBadRequest()
    {
        // Arrange
        SetUpRequestBody(string.Empty);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ExceptionRecordCreated_ReturnsOk()
    {
        // Arrange
        _validationExceptionData.Setup(s => s.Create(It.IsAny<ValidationException>())).ReturnsAsync(true);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.IsNotNull(result);
        _validationExceptionData.Verify(v => v.Create(It.IsAny<ValidationException>()), Times.Once);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ExceptionRecordFailedToCreate_ReturnsInternalServerError()
    {
        // Arrange
        _validationExceptionData.Setup(s => s.Create(It.IsAny<ValidationException>())).ReturnsAsync(false);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.IsNotNull(result);
        _validationExceptionData.Verify(v => v.Create(It.IsAny<ValidationException>()), Times.Once);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ExceptionRecordCreated_CompletesMessage()
    {
        _validationExceptionData.Setup(s => s.Create(It.IsAny<ValidationException>())).ReturnsAsync(true);

        await _function.Run(serviceBusMessage, serviceBusMessageActions.Object);

        serviceBusMessageActions.Verify(x => x.CompleteMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), CancellationToken.None), Times.Once);
        _logger.Verify(x => x.Log(It.Is<Microsoft.Extensions.Logging.LogLevel>(l => l == Microsoft.Extensions.Logging.LogLevel.Information),
              It.IsAny<EventId>(),
              It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("added exception to database and completed message successfully")),
              It.IsAny<Exception>(),
              It.IsAny<Func<It.IsAnyType, Exception, string>>()),
          Times.Once);
    }

    [TestMethod]
    public async Task Run_CreateReturnsFalse_SendSMessageToDeadLetter()
    {
        _validationExceptionData.Setup(s => s.Create(It.IsAny<ValidationException>())).ReturnsAsync(false);

        await _function.Run(serviceBusMessage, serviceBusMessageActions.Object);

        serviceBusMessageActions.Verify(x => x.DeadLetterMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), null, null, null, CancellationToken.None), Times.Once);
        _logger.Verify(x => x.Log(It.Is<Microsoft.Extensions.Logging.LogLevel>(l => l == Microsoft.Extensions.Logging.LogLevel.Error),
              It.IsAny<EventId>(),
              It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("could not create exception please see database for more details")),
              It.IsAny<Exception>(),
              It.IsAny<Func<It.IsAnyType, Exception, string>>()),
          Times.Once);
    }

    [TestMethod]
    public async Task Run_CannotParseMessageBody_SendSMessageToDeadLetter()
    {
        var serviceBusMessageBody = JsonSerializer.Serialize("dfgfdgfggf");
        serviceBusMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
              body: new BinaryData(serviceBusMessageBody),
              messageId: $"id-{1}",
              partitionKey: "illustrative-partitionKey",
              correlationId: "illustrative-correlationId",
              contentType: "illustrative-contentType",
              replyTo: "illustrative-replyTo"
              );

        await _function.Run(serviceBusMessage, serviceBusMessageActions.Object);

        serviceBusMessageActions.Verify(x => x.DeadLetterMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), null, null, null, CancellationToken.None), Times.Once);
        _logger.Verify(x => x.Log(It.Is<Microsoft.Extensions.Logging.LogLevel>(l => l == Microsoft.Extensions.Logging.LogLevel.Error),
              It.IsAny<EventId>(),
              It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("exception could not be added to service bus topic. See dead letter storage for more")),
              It.IsAny<Exception>(),
              It.IsAny<Func<It.IsAnyType, Exception, string>>()),
          Times.Once);
    }


    [TestMethod]
    public async Task Run_CreateThrowsAnError_SendSMessageToDeadLetter()
    {
        _validationExceptionData.Setup(s => s.Create(It.IsAny<ValidationException>())).Throws(new Exception("some new error"));

        await _function.Run(serviceBusMessage, serviceBusMessageActions.Object);

        serviceBusMessageActions.Verify(x => x.DeadLetterMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), null, null, null, CancellationToken.None), Times.Once);
        _logger.Verify(x => x.Log(It.Is<Microsoft.Extensions.Logging.LogLevel>(l => l == Microsoft.Extensions.Logging.LogLevel.Error),
              It.IsAny<EventId>(),
              It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("exception could not be added to service bus topic. See dead letter storage for more")),
              It.IsAny<Exception>(),
              It.IsAny<Func<It.IsAnyType, Exception, string>>()),
          Times.Once);
    }


    private void SetUpRequestBody(string json)
    {
        var byteArray = Encoding.ASCII.GetBytes(json);
        var bodyStream = new MemoryStream(byteArray);

        _request.Setup(r => r.Body).Returns(bodyStream);
    }
}

