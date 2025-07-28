namespace NHS.CohortManager.Tests.UnitTests.ScreeningDataServicesTests;

using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Model;
using Moq;
using System.Text.Json;
using NHS.CohortManager.ScreeningDataServices;
using Common;
using System.Threading.Tasks;
using Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using System.Text;
using ServiceLayer.TestUtilities;
using Microsoft.Extensions.Logging;
using Data.Database;

[TestClass]
public class UpdateExceptionServiceNowIdTests
{
    private readonly Mock<ILogger<GetValidationExceptions>> _loggerMock;
    private readonly Mock<ICreateResponse> _createResponseMock;
    private readonly Mock<IValidationExceptionData> _validationDataMock;
    private readonly Mock<IHttpParserHelper> _httpParserHelperMock;
    private readonly Mock<IPaginationService<ValidationException>> _paginationServiceMock;
    private readonly Mock<FunctionContext> _contextMock;
    private readonly GetValidationExceptions _service;

    public UpdateExceptionServiceNowIdTests()
    {
        _loggerMock = new Mock<ILogger<GetValidationExceptions>>();
        _createResponseMock = new Mock<ICreateResponse>();
        _validationDataMock = new Mock<IValidationExceptionData>();
        _httpParserHelperMock = new Mock<IHttpParserHelper>();
        _paginationServiceMock = new Mock<IPaginationService<ValidationException>>();
        _contextMock = new Mock<FunctionContext>();

        _service = new GetValidationExceptions(
            _loggerMock.Object,
            _createResponseMock.Object,
            _validationDataMock.Object,
            _httpParserHelperMock.Object,
            _paginationServiceMock.Object
        );
    }

    private Mock<HttpRequestData> CreateMockRequest(string requestBody)
    {
        var request = new Mock<HttpRequestData>(_contextMock.Object);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        request.Setup(r => r.Body).Returns(bodyStream);

        var response = new Mock<HttpResponseData>(_contextMock.Object);
        response.SetupProperty(r => r.StatusCode);
        request.Setup(r => r.CreateResponse()).Returns(response.Object);

        return request;
    }

    private void SetupCreateResponseMock(HttpStatusCode statusCode)
    {
        var mockResponse = new Mock<HttpResponseData>(_contextMock.Object);
        mockResponse.SetupProperty(r => r.StatusCode, statusCode);
        _createResponseMock.Setup(c => c.CreateHttpResponse(statusCode, It.IsAny<HttpRequestData>(), It.IsAny<string>())).Returns(mockResponse.Object);
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_ValidRequest_ReturnsOK()
    {
        // Arrange
        var request = new UpdateExceptionServiceNowIdRequest
        {
            ExceptionId = 123,
            ServiceNowId = "INC123456789"
        };
        var requestBody = JsonSerializer.Serialize(request);
        var mockRequest = CreateMockRequest(requestBody);

        _validationDataMock.Setup(v => v.UpdateExceptionServiceNowId(123, "INC123456789"))
            .ReturnsAsync(true);
        SetupCreateResponseMock(HttpStatusCode.OK);

        // Act
        var result = await _service.UpdateExceptionServiceNowId(mockRequest.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _validationDataMock.Verify(v => v.UpdateExceptionServiceNowId(123, "INC123456789"), Times.Once);
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_EmptyRequestBody_ReturnsBadRequest()
    {
        // Arrange
        var mockRequest = CreateMockRequest(string.Empty);
        SetupCreateResponseMock(HttpStatusCode.BadRequest);

        // Act
        var result = await _service.UpdateExceptionServiceNowId(mockRequest.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _createResponseMock.Verify(c => c.CreateHttpResponse(HttpStatusCode.BadRequest, It.IsAny<HttpRequestData>(), "Request body cannot be empty."), Times.Once);
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_NullRequestBody_ReturnsBadRequest()
    {
        // Arrange
        var mockRequest = CreateMockRequest("   ");
        SetupCreateResponseMock(HttpStatusCode.BadRequest);

        // Act
        var result = await _service.UpdateExceptionServiceNowId(mockRequest.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_InvalidJson_ReturnsInternalServerError()
    {
        // Arrange
        var mockRequest = CreateMockRequest("{invalid json}");
        SetupCreateResponseMock(HttpStatusCode.InternalServerError);

        // Act
        var result = await _service.UpdateExceptionServiceNowId(mockRequest.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_MissingExceptionId_ReturnsBadRequest()
    {
        // Arrange
        var request = new UpdateExceptionServiceNowIdRequest
        {
            ExceptionId = 0,
            ServiceNowId = "INC123456789"
        };
        var requestBody = JsonSerializer.Serialize(request);
        var mockRequest = CreateMockRequest(requestBody);
        SetupCreateResponseMock(HttpStatusCode.BadRequest);

        // Act
        var result = await _service.UpdateExceptionServiceNowId(mockRequest.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _createResponseMock.Verify(c => c.CreateHttpResponse(HttpStatusCode.BadRequest, It.IsAny<HttpRequestData>(), "Invalid request. ExceptionId and ServiceNowId are required."), Times.Once);
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_MissingServiceNowId_ReturnsBadRequest()
    {
        // Arrange
        var request = new UpdateExceptionServiceNowIdRequest
        {
            ExceptionId = 123,
            ServiceNowId = string.Empty
        };
        var requestBody = JsonSerializer.Serialize(request);
        var mockRequest = CreateMockRequest(requestBody);
        SetupCreateResponseMock(HttpStatusCode.BadRequest);

        // Act
        var result = await _service.UpdateExceptionServiceNowId(mockRequest.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_ServiceNowIdTooShort_ReturnsBadRequest()
    {
        // Arrange
        var request = new UpdateExceptionServiceNowIdRequest
        {
            ExceptionId = 123,
            ServiceNowId = "INC123"
        };
        var requestBody = JsonSerializer.Serialize(request);
        var mockRequest = CreateMockRequest(requestBody);
        SetupCreateResponseMock(HttpStatusCode.BadRequest);

        // Act
        var result = await _service.UpdateExceptionServiceNowId(mockRequest.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _createResponseMock.Verify(c => c.CreateHttpResponse(HttpStatusCode.BadRequest, It.IsAny<HttpRequestData>(), "ServiceNowID must be at least 9 characters long."), Times.Once);
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_ServiceNowIdContainsSpaces_ReturnsBadRequest()
    {
        // Arrange
        var request = new UpdateExceptionServiceNowIdRequest
        {
            ExceptionId = 123,
            ServiceNowId = "INC 123456789"
        };
        var requestBody = JsonSerializer.Serialize(request);
        var mockRequest = CreateMockRequest(requestBody);
        SetupCreateResponseMock(HttpStatusCode.BadRequest);

        // Act
        var result = await _service.UpdateExceptionServiceNowId(mockRequest.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _createResponseMock.Verify(c => c.CreateHttpResponse(HttpStatusCode.BadRequest, It.IsAny<HttpRequestData>(), "ServiceNowID cannot contain spaces."), Times.Once);
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_ServiceNowIdNotAlphanumeric_ReturnsBadRequest()
    {
        // Arrange
        var request = new UpdateExceptionServiceNowIdRequest
        {
            ExceptionId = 123,
            ServiceNowId = "INC123456789!"
        };
        var requestBody = JsonSerializer.Serialize(request);
        var mockRequest = CreateMockRequest(requestBody);
        SetupCreateResponseMock(HttpStatusCode.BadRequest);

        // Act
        var result = await _service.UpdateExceptionServiceNowId(mockRequest.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _createResponseMock.Verify(c => c.CreateHttpResponse(HttpStatusCode.BadRequest, It.IsAny<HttpRequestData>(), "ServiceNowID must contain only alphanumeric characters."), Times.Once);
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_DatabaseUpdateFails_ReturnsInternalServerError()
    {
        // Arrange
        var request = new UpdateExceptionServiceNowIdRequest
        {
            ExceptionId = 123,
            ServiceNowId = "INC123456789"
        };
        var requestBody = JsonSerializer.Serialize(request);
        var mockRequest = CreateMockRequest(requestBody);

        _validationDataMock.Setup(v => v.UpdateExceptionServiceNowId(123, "INC123456789")).ReturnsAsync(false);
        SetupCreateResponseMock(HttpStatusCode.InternalServerError);

        // Act
        var result = await _service.UpdateExceptionServiceNowId(mockRequest.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
        _createResponseMock.Verify(c => c.CreateHttpResponse(HttpStatusCode.InternalServerError, It.IsAny<HttpRequestData>(), "Failed to update ServiceNow ID or Exception with ID 123 not found."), Times.Once);
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_DatabaseThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new UpdateExceptionServiceNowIdRequest
        {
            ExceptionId = 123,
            ServiceNowId = "INC123456789"
        };
        var requestBody = JsonSerializer.Serialize(request);
        var mockRequest = CreateMockRequest(requestBody);

        var expectedException = new Exception("Database error");
        _validationDataMock.Setup(v => v.UpdateExceptionServiceNowId(123, "INC123456789")).ThrowsAsync(expectedException);
        SetupCreateResponseMock(HttpStatusCode.InternalServerError);

        // Act
        var result = await _service.UpdateExceptionServiceNowId(mockRequest.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
        _loggerMock.VerifyLogger(LogLevel.Error, "Error processing: UpdateExceptionServiceNowId update ServiceNow ID request", ex => ex == expectedException);
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_ValidServiceNowIdWithMinimumLengthOf9_ReturnsOK()
    {
        // Arrange
        var request = new UpdateExceptionServiceNowIdRequest
        {
            ExceptionId = 123,
            ServiceNowId = "INC123456"
        };
        var requestBody = JsonSerializer.Serialize(request);
        var mockRequest = CreateMockRequest(requestBody);

        _validationDataMock.Setup(v => v.UpdateExceptionServiceNowId(123, "INC123456")).ReturnsAsync(true);
        SetupCreateResponseMock(HttpStatusCode.OK);

        // Act
        var result = await _service.UpdateExceptionServiceNowId(mockRequest.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }
}
