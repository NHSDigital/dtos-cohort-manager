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

[TestClass]
public class CreateExceptionTests
{
    private readonly Mock<ILogger<CreateExceptionRecord>> _logger = new();
    private readonly Mock<FunctionContext> _context = new();
    private readonly Mock<HttpRequestData> _request;
    private readonly ValidationException _requestBody;
    private readonly CreateExceptionRecord _function;
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<IValidationExceptionData> _validationExceptionData = new();

    public CreateExceptionTests()
    {
        _request = new Mock<HttpRequestData>(_context.Object);

        _requestBody = new ValidationException() { ExceptionId = 1 };

        _function = new CreateExceptionRecord(_logger.Object, _validationExceptionData.Object, _createResponse.Object);

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
    public async Task Run_ExceptionRecordCreated_ReturnsCreated()
    {
        // Arrange
        _validationExceptionData.Setup(s => s.Create(It.IsAny<ValidationException>())).Returns(true);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.IsNotNull(result);
        _validationExceptionData.Verify(v => v.Create(It.IsAny<ValidationException>()), Times.Once);
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ExceptionRecordFailedToCreate_ReturnsInternalServerError()
    {
        // Arrange
        _validationExceptionData.Setup(s => s.Create(It.IsAny<ValidationException>())).Returns(false);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.IsNotNull(result);
        _validationExceptionData.Verify(v => v.Create(It.IsAny<ValidationException>()), Times.Once);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    private void SetUpRequestBody(string json)
    {
        var byteArray = Encoding.ASCII.GetBytes(json);
        var bodyStream = new MemoryStream(byteArray);

        _request.Setup(r => r.Body).Returns(bodyStream);
    }
}

