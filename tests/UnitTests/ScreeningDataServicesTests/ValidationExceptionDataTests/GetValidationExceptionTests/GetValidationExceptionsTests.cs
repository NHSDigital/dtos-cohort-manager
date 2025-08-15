namespace NHS.CohortManager.Tests.UnitTests.ScreeningDataServicesTests;

using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Model;
using Moq;
using NHS.CohortManager.ScreeningDataServices;
using Common;
using System.Threading.Tasks;
using Model.Enums;
using Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Data.Database;

[TestClass]
public class GetValidationExceptionsTests
{
    private readonly Mock<ILogger<GetValidationExceptions>> _loggerMock = new();
    private readonly Mock<ICreateResponse> _createResponseMock = new();
    private readonly Mock<IValidationExceptionData> _validationDataMock = new();
    private readonly Mock<IHttpParserHelper> _httpParserHelperMock = new();
    private readonly Mock<IPaginationService<ValidationException>> _paginationServiceMock = new();
    private readonly Mock<FunctionContext> _contextMock = new();
    private readonly Mock<HttpRequestData> _requestMock;
    private readonly GetValidationExceptions _service;
    private readonly List<ValidationException> _exceptionList;
    private readonly ExceptionCategory _exceptionCategory;

    public GetValidationExceptionsTests()
    {
        _requestMock = new Mock<HttpRequestData>(_contextMock.Object);

        _exceptionList = new List<ValidationException>
        {
            new() { ExceptionId = 1, Category = 0 },
            new() { ExceptionId = 2, Category = 3 },
            new() { ExceptionId = 3, Category = 3 }
        };
        _exceptionCategory = ExceptionCategory.NBO;

        _service = new GetValidationExceptions(
            _loggerMock.Object,
            _createResponseMock.Object,
            _validationDataMock.Object,
            _httpParserHelperMock.Object,
            _paginationServiceMock.Object
        );

        SetupHttpResponse();
    }

    private void SetupHttpResponse()
    {
        _requestMock.Setup(r => r.CreateResponse()).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(_contextMock.Object);
            response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
            response.SetupProperty(r => r.StatusCode);
            response.SetupProperty(r => r.Body, new MemoryStream());
            return response.Object;
        });

        _createResponseMock.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string responseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                if (!string.IsNullOrEmpty(responseBody))
                {
                    response.WriteString(responseBody);
                }
                return response;
            });

        _createResponseMock.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), ""))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string responseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                return response;
            });
    }

    private void SetupQueryParameters(Dictionary<string, string> queryParams = null)
    {
        queryParams = queryParams ?? new Dictionary<string, string>();

        // Ensure default parameters exist
        if (!queryParams.ContainsKey("exceptionId")) queryParams["exceptionId"] = "0";
        if (!queryParams.ContainsKey("lastId")) queryParams["lastId"] = "0";
        if (!queryParams.ContainsKey("exceptionStatus")) queryParams["exceptionStatus"] = "All";
        if (!queryParams.ContainsKey("sortOrder")) queryParams["sortOrder"] = "Descending";
        if (!queryParams.ContainsKey("exceptionCategory")) queryParams["exceptionCategory"] = "NBO";

        var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        var url = $"https://localhost?{queryString}";

        _requestMock.Setup(r => r.Url).Returns(new Uri(url));

        var queryCollection = new System.Collections.Specialized.NameValueCollection();
        foreach (var kvp in queryParams)
        {
            queryCollection.Add(kvp.Key, kvp.Value);
        }

        _requestMock.Setup(r => r.Query).Returns(queryCollection);
    }

    [TestMethod]
    public async Task Run_NoExceptionIdQueryParameter_ReturnsAllExceptions()
    {
        // Arrange
        SetupQueryParameters();

        var paginatedResult = new PaginationResult<ValidationException>
        {
            Items = _exceptionList,
            HasNextPage = false,
            IsFirstPage = true,
            CurrentPage = 1,
            TotalItems = _exceptionList.Count,
            TotalPages = 1
        };

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "exceptionId")).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "lastId")).Returns(0);
        _validationDataMock.Setup(s => s.GetAllFilteredExceptions(ExceptionStatus.All, SortOrder.Descending, _exceptionCategory)).ReturnsAsync(_exceptionList);
        _paginationServiceMock.Setup(p => p.GetPaginatedResult(It.IsAny<IQueryable<ValidationException>>(), 0, It.IsAny<Func<ValidationException, int>>())).Returns(paginatedResult);

        // Act
        var result = await _service.Run(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _validationDataMock.Verify(v => v.GetAllFilteredExceptions(ExceptionStatus.All, SortOrder.Descending, _exceptionCategory), Times.Once);
        _paginationServiceMock.Verify(p => p.GetPaginatedResult(It.IsAny<IQueryable<ValidationException>>(), 0, It.IsAny<Func<ValidationException, int>>()), Times.Once);
    }

    [TestMethod]
    public async Task Run_ValidExceptionId_ReturnsExceptionById()
    {
        // Arrange
        var exceptionId = 1;
        SetupQueryParameters(new Dictionary<string, string> { { "exceptionId", exceptionId.ToString() } });

        var expectedException = _exceptionList.First(f => f.ExceptionId == exceptionId);

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "exceptionId")).Returns(exceptionId);
        _validationDataMock.Setup(s => s.GetExceptionById(exceptionId)).ReturnsAsync(expectedException);

        // Act
        var result = await _service.Run(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _validationDataMock.Verify(v => v.GetExceptionById(exceptionId), Times.Once);
    }

    [TestMethod]
    public async Task Run_ExceptionIdIsOutOfRange_ReturnsNoContent()
    {
        // Arrange
        var exceptionId = 999;
        SetupQueryParameters(new Dictionary<string, string> { { "exceptionId", exceptionId.ToString() } });

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "exceptionId")).Returns(exceptionId);
        _validationDataMock.Setup(s => s.GetExceptionById(exceptionId)).ReturnsAsync((ValidationException?)null);

        // Act
        var result = await _service.Run(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
        _validationDataMock.Verify(v => v.GetExceptionById(exceptionId), Times.Once);
    }

    [TestMethod]
    public async Task Run_NoExceptionsFound_ReturnsNoContent()
    {
        // Arrange
        SetupQueryParameters();

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "exceptionId")).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "lastId")).Returns(0);
        _validationDataMock.Setup(s => s.GetAllFilteredExceptions(ExceptionStatus.All, SortOrder.Descending, _exceptionCategory)).ReturnsAsync(new List<ValidationException>());

        // Act
        var result = await _service.Run(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
        _validationDataMock.Verify(v => v.GetAllFilteredExceptions(ExceptionStatus.All, SortOrder.Descending, _exceptionCategory), Times.Once);
    }

    [TestMethod]
    public async Task Run_ThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        SetupQueryParameters();

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "exceptionId")).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "lastId")).Returns(0);
        _validationDataMock.Setup(s => s.GetAllFilteredExceptions(ExceptionStatus.All, SortOrder.Descending, _exceptionCategory)).ThrowsAsync(new Exception("Simulated failure"));

        // Act
        var result = await _service.Run(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
        _validationDataMock.Verify(v => v.GetAllFilteredExceptions(ExceptionStatus.All, SortOrder.Descending, _exceptionCategory), Times.Once);
    }

    [TestMethod]
    public async Task Run_WithExceptionStatus_ReturnsFilteredExceptions()
    {
        // Arrange
        var exceptionStatus = ExceptionStatus.Raised;
        SetupQueryParameters(new Dictionary<string, string> { { "exceptionStatus", "Raised" } });

        var paginatedResult = new PaginationResult<ValidationException>
        {
            Items = _exceptionList,
            HasNextPage = false,
            IsFirstPage = true,
            CurrentPage = 1,
            TotalItems = _exceptionList.Count,
            TotalPages = 1
        };

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "exceptionId")).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "lastId")).Returns(0);
        _validationDataMock.Setup(s => s.GetAllFilteredExceptions(exceptionStatus, SortOrder.Descending, _exceptionCategory)).ReturnsAsync(_exceptionList);
        _paginationServiceMock.Setup(p => p.GetPaginatedResult(It.IsAny<IQueryable<ValidationException>>(), 0, It.IsAny<Func<ValidationException, int>>())).Returns(paginatedResult);

        // Act
        var result = await _service.Run(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _validationDataMock.Verify(v => v.GetAllFilteredExceptions(exceptionStatus, SortOrder.Descending, _exceptionCategory), Times.Once);
    }

    [TestMethod]
    public async Task Run_WithSortOrder_ReturnsOrderedExceptions()
    {
        // Arrange
        var sortOrder = SortOrder.Ascending;
        SetupQueryParameters(new Dictionary<string, string> { { "sortOrder", "Ascending" } });

        var paginatedResult = new PaginationResult<ValidationException>
        {
            Items = _exceptionList,
            HasNextPage = false,
            IsFirstPage = true,
            CurrentPage = 1,
            TotalItems = _exceptionList.Count,
            TotalPages = 1
        };

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "exceptionId")).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "lastId")).Returns(0);
        _validationDataMock.Setup(s => s.GetAllFilteredExceptions(ExceptionStatus.All, sortOrder, _exceptionCategory)).ReturnsAsync(_exceptionList);
        _paginationServiceMock.Setup(p => p.GetPaginatedResult(It.IsAny<IQueryable<ValidationException>>(), 0, It.IsAny<Func<ValidationException, int>>())).Returns(paginatedResult);

        // Act
        var result = await _service.Run(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _validationDataMock.Verify(v => v.GetAllFilteredExceptions(ExceptionStatus.All, sortOrder, _exceptionCategory), Times.Once);
    }

    [TestMethod]
    public async Task Run_WithLastId_ReturnsPaginatedResults()
    {
        // Arrange
        var lastId = 10;
        SetupQueryParameters(new Dictionary<string, string> { { "lastId", lastId.ToString() } });

        var paginatedResult = new PaginationResult<ValidationException>
        {
            Items = _exceptionList.Skip(1),
            HasNextPage = true,
            IsFirstPage = false,
            CurrentPage = 2,
            TotalItems = _exceptionList.Count,
            TotalPages = 2,
            LastResultId = lastId
        };

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "exceptionId")).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "lastId")).Returns(lastId);
        _validationDataMock.Setup(s => s.GetAllFilteredExceptions(ExceptionStatus.All, SortOrder.Descending, _exceptionCategory)).ReturnsAsync(_exceptionList);
        _paginationServiceMock.Setup(p => p.GetPaginatedResult(It.IsAny<IQueryable<ValidationException>>(), lastId, It.IsAny<Func<ValidationException, int>>())).Returns(paginatedResult);

        // Act
        var result = await _service.Run(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _paginationServiceMock.Verify(p => p.GetPaginatedResult(It.IsAny<IQueryable<ValidationException>>(), lastId, It.IsAny<Func<ValidationException, int>>()), Times.Once);
    }

    [TestMethod]
    public async Task Run_GetExceptionByIdThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var exceptionId = 1;
        SetupQueryParameters(new Dictionary<string, string> { { "exceptionId", exceptionId.ToString() } });
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "exceptionId")).Returns(exceptionId);
        _validationDataMock.Setup(s => s.GetExceptionById(exceptionId)).ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.Run(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
        _validationDataMock.Verify(v => v.GetExceptionById(exceptionId), Times.Once);
    }

    [TestMethod]
    public async Task Run_PaginationServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        SetupQueryParameters();

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "exceptionId")).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "lastId")).Returns(0);
        _validationDataMock.Setup(s => s.GetAllFilteredExceptions(ExceptionStatus.All, SortOrder.Descending, _exceptionCategory)).ReturnsAsync(_exceptionList);
        _paginationServiceMock.Setup(p => p.GetPaginatedResult(It.IsAny<IQueryable<ValidationException>>(), 0, It.IsAny<Func<ValidationException, int>>())).Throws(new Exception("Pagination error"));

        // Act
        var result = await _service.Run(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
        _paginationServiceMock.Verify(p => p.GetPaginatedResult(It.IsAny<IQueryable<ValidationException>>(), 0, It.IsAny<Func<ValidationException, int>>()), Times.Once);
    }
}
