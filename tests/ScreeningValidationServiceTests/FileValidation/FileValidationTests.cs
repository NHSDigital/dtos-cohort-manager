namespace NHS.CohortManager.Tests.ScreeningValidationServiceTests;

using System.Net;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using NHS.CohortManager.ScreeningValidationService;
using Model;
using Common;

[TestClass]
public class FileValidationTests
{
    private readonly Mock<ILogger<FileValidation>> _logger = new();
    private readonly Mock<FunctionContext> _context = new();
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly Mock<HttpWebResponse> _webResponse = new();
    private readonly Mock<HttpRequestData> _request;
    private readonly FileValidationRequestBody _requestBody;
    private readonly FileValidation _function;



    public FileValidationTests()
    {
        _request = new Mock<HttpRequestData>(_context.Object);

        _requestBody = new FileValidationRequestBody()
        {
            ExceptionMessage = "There was an exception.",
            FileName = "test-file.csv"
        };

        _function = new FileValidation(_logger.Object, _callFunction.Object);

        _request.Setup(r => r.CreateResponse()).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(_context.Object);
            response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
            response.SetupProperty(r => r.StatusCode);
            response.SetupProperty(r => r.Body, new MemoryStream());
            return response.Object;
        });
    }

    [TestMethod]
    public async Task Run_Should_Return_BadRequest_When_Request_Body_Empty()
    {
        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_Should_Return_BadRequest_When_Request_Body_Invalid()
    {
        // Arrange
        SetUpRequestBody("Invalid request body");

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_Should_Return_OK_And_Log_Request_When_Request_Body_Valid()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        _callFunction.Setup(call => call.SendPost(It.IsAny<string>(), It.IsAny<string>()))
                            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        _logger.Verify(log =>
            log.Log(
            LogLevel.Information,
            0,
            It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("File validation exception")),
            null,
            (Func<object, Exception, string>)It.IsAny<object>()
            ));

        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    private void SetUpRequestBody(string json)
    {
        var byteArray = Encoding.ASCII.GetBytes(json);
        var bodyStream = new MemoryStream(byteArray);

        _request.Setup(r => r.Body).Returns(bodyStream);
    }
}
