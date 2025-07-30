namespace NHS.CohortManager.Tests.UnitTests.ScreeningDataServicesTests;

using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Model;
using Moq;
using NHS.CohortManager.Tests.TestUtils;
using System.Text.Json;
using NHS.CohortManager.ScreeningDataServices;
using Common;
using System.Threading.Tasks;
using Model.Enums;
using Common.Interfaces;

[TestClass]
public class GetValidationExceptionsTests : DatabaseTestBaseSetup<GetValidationExceptions>
{
    private readonly List<ValidationException> _exceptionList;
    private readonly Dictionary<string, string> columnToClassPropertyMapping;
    private readonly Mock<PaginationService<ValidationException>> _paginationServiceMock = new();
    private readonly HttpResponseData _mockHttpResponseData;
    private static readonly Mock<IHttpParserHelper> _httpParserHelperMock = new();
    private readonly ExceptionCategory _exceptionCategory;


    public GetValidationExceptionsTests() : base((conn, logger, transaction, command, response) => null)
    {
        columnToClassPropertyMapping = new Dictionary<string, string> { { "EXCEPTION_ID", "ExceptionId" } };
        _exceptionList = new List<ValidationException>
        {
            new ValidationException { ExceptionId = 1, Category = 0 },
            new ValidationException { ExceptionId = 2, Category = 3 },
            new ValidationException { ExceptionId = 3, Category = 3 }
        };
        _exceptionCategory = ExceptionCategory.NBO;

        _service = new GetValidationExceptions(
            _loggerMock.Object,
            _createResponseMock.Object,
            _validationDataMock.Object,
            _httpParserHelperMock.Object,
            _paginationServiceMock.Object
        );

        var json = JsonSerializer.Serialize(_exceptionList);
        SetupRequest(json);
        CreateHttpResponseMock();
        SetupDataReader(_exceptionList, columnToClassPropertyMapping);

        _mockHttpResponseData = _request.Object.CreateResponse();
    }

    [TestMethod]
    public async Task Run_NoExceptionIdQueryParameter_ReturnsAllExceptions()
    {
        // Arrange
        var exceptionId = 0;
        _validationDataMock.Setup(s => s.GetAllFilteredExceptions(ExceptionStatus.All, SortOrder.Descending, _exceptionCategory)).ReturnsAsync(_exceptionList);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(It.IsAny<HttpRequestData>(), It.IsAny<string>())).Returns(exceptionId);
        SetupRequestWithQueryParams([]);

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _validationDataMock.Verify(v => v.GetAllFilteredExceptions(ExceptionStatus.All, SortOrder.Descending, _exceptionCategory), Times.Once);
    }

    [TestMethod]
    public async Task Run_ValidExceptionId_ReturnsExceptionById()
    {
        // Arrange
        var exceptionId = 1;
        _validationDataMock.Setup(s => s.GetExceptionById(exceptionId)).ReturnsAsync(_exceptionList.First(f => f.ExceptionId == exceptionId));
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(It.IsAny<HttpRequestData>(), It.IsAny<string>())).Returns(exceptionId);
        SetupRequestWithQueryParams(new Dictionary<string, string> { { "exceptionId", exceptionId.ToString() } });

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _validationDataMock.Verify(v => v.GetExceptionById(exceptionId), Times.Once);
    }

    [TestMethod]
    public async Task Run_ExceptionIdIsOutOfRange_ReturnsNoContent()
    {
        // Arrange
        var exceptionId = 999;
        _validationDataMock.Setup(s => s.GetExceptionById(exceptionId)).ReturnsAsync((ValidationException)null);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(It.IsAny<HttpRequestData>(), It.IsAny<string>())).Returns(exceptionId);

        SetupRequestWithQueryParams(new Dictionary<string, string> { { "exceptionId", exceptionId.ToString() } });

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
        _validationDataMock.Verify(v => v.GetExceptionById(exceptionId), Times.Once);
    }
    [TestMethod]
    public async Task Run_NoExceptionsFound_ReturnsNoContent()
    {
        // Arrange
        var exceptionId = 0;


        _validationDataMock.Setup(s => s.GetAllFilteredExceptions(ExceptionStatus.All, SortOrder.Descending, _exceptionCategory))
                        .ReturnsAsync(new List<ValidationException>());
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(It.IsAny<HttpRequestData>(), "exceptionId"))
                            .Returns(0);

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(It.IsAny<HttpRequestData>(), "lastId"))
                            .Returns(0);

        _mockHttpResponseData.StatusCode = HttpStatusCode.NoContent;

        _createResponseMock.Setup(r =>
            r.CreateHttpResponse(
                HttpStatusCode.NoContent,
                It.IsAny<HttpRequestData>(),
                It.IsAny<string>())
            ).Returns(_mockHttpResponseData);

        SetupRequestWithQueryParams([]);

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
        _validationDataMock.Verify(v => v.GetAllFilteredExceptions(ExceptionStatus.All, SortOrder.Descending, _exceptionCategory), Times.Once);
    }

    [TestMethod]
    public async Task Run_ThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var exceptionId = 0;

        // Simulate exception thrown from GetAllFilteredExceptions
        _validationDataMock.Setup(s => s.GetAllFilteredExceptions(ExceptionStatus.All, SortOrder.Descending, _exceptionCategory))
                        .ThrowsAsync(new Exception("Simulated failure"));

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(It.IsAny<HttpRequestData>(), "exceptionId"))
                            .Returns(exceptionId);

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(It.IsAny<HttpRequestData>(), "lastId"))
                            .Returns(0);

        // Setup the mock response with expected error status code
        _mockHttpResponseData.StatusCode = HttpStatusCode.InternalServerError;

        _createResponseMock.Setup(r =>
            r.CreateHttpResponse(
                HttpStatusCode.InternalServerError,
                It.IsAny<HttpRequestData>(),
                It.IsAny<string>())
            ).Returns(_mockHttpResponseData);

        SetupRequestWithQueryParams([]);

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
        _validationDataMock.Verify(v => v.GetAllFilteredExceptions(ExceptionStatus.All, SortOrder.Descending, _exceptionCategory), Times.Once);
    }
}
