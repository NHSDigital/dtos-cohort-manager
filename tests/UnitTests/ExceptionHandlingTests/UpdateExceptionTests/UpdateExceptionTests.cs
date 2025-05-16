namespace NHS.CohortManager.Tests.UnitTests.UpdateExceptionTests;

using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;
using Model;
using Common;
using NHS.CohortManager.ExceptionService;
using System.Text.Json;
using System.Text;
using DataServices.Client;
using System.Linq.Expressions;

[TestClass]
public class UpdateExceptionTests
{
    private readonly Mock<ILogger<UpdateException>> _logger = new();
    private readonly Mock<FunctionContext> _context = new();
    private readonly Mock<HttpRequestData> _request;
    private readonly UpdateExceptionRequest _requestBody;
    private readonly UpdateException _function;
    private readonly Mock<ICreateResponse> _createResponse = new();
    private Mock<IDataServiceClient<ExceptionManagement>> _mockExceptionManagementDataService = new();

    public UpdateExceptionTests()
    {
        _request = new Mock<HttpRequestData>(_context.Object);

        _requestBody = new UpdateExceptionRequest() { ExceptionId = "1" };

        _function = new UpdateException(_logger.Object, _mockExceptionManagementDataService.Object, _createResponse.Object);

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
    public async Task Run_UpdateException_ReturnsNoContentRequest()
    {
        // Arrange
        SetUpRequestBody(string.Empty);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_UpdateException_ReturnsOk()
    {
        // Arrange
        var exceptionManagement = new ExceptionManagement
        {
            ExceptionId = 1,
            FileName = "xyz.parquet",
            NhsNumber = "1234567890",
            ServiceNowId = "INC00123456"
        };
        _mockExceptionManagementDataService.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ExceptionManagement, bool>>>())).ReturnsAsync(exceptionManagement);
        _mockExceptionManagementDataService.Setup(s => s.Update(It.IsAny<ExceptionManagement>())).ReturnsAsync(true);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.IsNotNull(result);
        _mockExceptionManagementDataService.Verify(v => v.Update(It.IsAny<ExceptionManagement>()), Times.Once);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_UpdateException_ReturnsInternalServerError()
    {
        // Arrange
        var exceptionManagement = new ExceptionManagement
        {
            ExceptionId = 1,
            FileName = "xyz.parquet",
            NhsNumber = "1234567890",
            ServiceNowId = "INC00123456"
        };
        _mockExceptionManagementDataService.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ExceptionManagement, bool>>>())).ReturnsAsync(exceptionManagement);
        _mockExceptionManagementDataService.Setup(s => s.Update(It.IsAny<ExceptionManagement>())).ReturnsAsync(false);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.IsNotNull(result);
        _mockExceptionManagementDataService.Verify(v => v.Update(It.IsAny<ExceptionManagement>()), Times.Once);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    private void SetUpRequestBody(string json)
    {
        var byteArray = Encoding.ASCII.GetBytes(json);
        var bodyStream = new MemoryStream(byteArray);

        _request.Setup(r => r.Body).Returns(bodyStream);
    }
}

