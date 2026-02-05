namespace NHS.CohortManager.Tests.UnitTests.ScreeningDataServicesTests;

using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Model;
using Moq;
using NHS.CohortManager.ScreeningDataServices;
using Common;
using System.Threading.Tasks;
using Model.Enums;
using Model.DTO;
using Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Data.Database;
using System.Text.Json;
using System.IO;
using System.Text;
using NHS.CohortManager.Tests.TestUtils;
using FluentAssertions;
using Model.Pagination;

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
            new() { ExceptionId = 1, CohortName = "Cohort1", DateCreated = DateTime.UtcNow.Date, NhsNumber = "1111111111", RuleDescription = "RuleA", Category = 3, ServiceNowId = "ServiceNow1", ServiceNowCreatedDate = DateTime.UtcNow.Date },
            new() { ExceptionId = 2, CohortName = "Cohort2", DateCreated = DateTime.UtcNow.Date.AddDays(-1), NhsNumber = "2222222222", RuleDescription = "RuleB", Category = 3, ServiceNowId = "ServiceNow2", ServiceNowCreatedDate = DateTime.UtcNow.Date.AddDays(-1) },
            new() { ExceptionId = 3, CohortName = "Cohort3", DateCreated = DateTime.UtcNow.Date.AddDays(-2), NhsNumber = "3333333333", RuleDescription = "RuleC", Category = 3, ServiceNowId = null, ServiceNowCreatedDate = null },
            new() { ExceptionId = 4, CohortName = "Cohort4", DateCreated = DateTime.Today.AddDays(-3), NhsNumber = "4444444444", RuleDescription = "RuleD", Category = 3, ServiceNowId = null, ServiceNowCreatedDate = null },
            new() { ExceptionId = 5, CohortName = "Cohort5", DateCreated = DateTime.UtcNow.Date, NhsNumber = "5555555555", RuleDescription = "Confusion Rule", Category = 12, ServiceNowId = null, ServiceNowCreatedDate = null },
            new() { ExceptionId = 6, CohortName = "Cohort6", DateCreated = DateTime.UtcNow.Date.AddDays(-1), NhsNumber = "6666666666", RuleDescription = "Superseded Rule", Category = 13, ServiceNowId = null, ServiceNowCreatedDate = null }
        };
        _exceptionCategory = ExceptionCategory.NBO;
        _service = new GetValidationExceptions(_loggerMock.Object, _createResponseMock.Object, _validationDataMock.Object, _httpParserHelperMock.Object, _paginationServiceMock.Object);

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
                    var bytes = Encoding.UTF8.GetBytes(responseBody);
                    response.Body.Write(bytes, 0, bytes.Length);
                    response.Body.Position = 0;
                }
                return response;
            });

        _createResponseMock.Setup(x => x.CreateHttpResponseWithHeaders(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string responseBody, Dictionary<string, string> headers) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                foreach (var header in headers)
                {
                    response.Headers.Add(header.Key, header.Value);
                }
                if (!string.IsNullOrEmpty(responseBody))
                {
                    var bytes = Encoding.UTF8.GetBytes(responseBody);
                    response.Body.Write(bytes, 0, bytes.Length);
                    response.Body.Position = 0;
                }
                return response;
            });
    }

    private void SetupQueryParameters(Dictionary<string, string>? queryParams = null)
    {
        queryParams = queryParams ?? new Dictionary<string, string>();

        var queryCollection = new System.Collections.Specialized.NameValueCollection();
        foreach (var kvp in queryParams)
        {
            queryCollection.Add(kvp.Key, kvp.Value);
        }

        _requestMock.Setup(r => r.Query).Returns(queryCollection);

        var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        var url = $"https://localhost?{queryString}";
        _requestMock.Setup(r => r.Url).Returns(new Uri(url));
    }

    private static T? DeserializeResponseBody<T>(HttpResponseData response)
    {
        if (response.Body.Length == 0) return default(T);

        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body);
        var content = reader.ReadToEnd();
        return JsonSerializer.Deserialize<T>(content);
    }

    [TestMethod]
    public async Task Run_NoExceptionIdQueryParameter_ReturnsAllExceptions()
    {
        // Arrange
        SetupQueryParameters();

        var paginatedResult = new PaginationResult<ValidationException>
        {
            Items = _exceptionList.Take(3).ToList(),
            HasNextPage = false,
            HasPreviousPage = false,
            CurrentPage = 1,
            TotalItems = 3,
            TotalPages = 1,
        };

        var expectedHeaders = new Dictionary<string, string> { { "X-Total-Count", "3" } };

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "exceptionId", 0)).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "page", 0)).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "pageSize", 0)).Returns(10);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsDateTime(_requestMock.Object, "reportDate")).Returns((DateTime?)null);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsBool(_requestMock.Object, "isReport", false)).Returns(false);
        _validationDataMock.Setup(s => s.GetFilteredExceptions(ExceptionStatus.All, SortOrder.Descending, _exceptionCategory, It.IsAny<SortBy?>(), It.IsAny<int?>(), It.IsAny<DateTime?>())).ReturnsAsync(_exceptionList);
        _paginationServiceMock.Setup(p => p.GetPaginatedResult(It.IsAny<IQueryable<ValidationException>>(), It.IsAny<int>(), It.IsAny<int>())).Throws(new Exception("Pagination error"));

        // Act
        var result = await _service.Run(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
        _loggerMock.VerifyLogger(LogLevel.Error, "Error processing: GetValidationExceptions validation exceptions request");
    }

    [TestMethod]
    public async Task Run_WhenIsReportTrueWithValidDate_ReturnsReportExceptions()
    {
        // Arrange
        var reportDate = DateTime.Now.Date.AddDays(-1);
        var exceptionCategory = ExceptionCategory.NBO;
        var reportExceptions = _exceptionList.Where(e => e.Category == 12 || e.Category == 13).ToList();
        SetupQueryParameters(new Dictionary<string, string> { { "isReport", "true" }, { "reportDate", reportDate.ToString("yyyy-MM-dd") } });

        var paginatedResult = new PaginationResult<ValidationException>
        {
            Items = reportExceptions,
            HasNextPage = false,
            HasPreviousPage = false,
            CurrentPage = 1,
            TotalItems = reportExceptions.Count,
            TotalPages = 1,
        };

        var expectedHeaders = new Dictionary<string, string> { { "X-Total-Count", "2" } };

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "exceptionId", 0)).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "page", 0)).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "pageSize", 0)).Returns(10);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsBool(_requestMock.Object, "isReport", false)).Returns(true);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsDateTime(_requestMock.Object, "reportDate")).Returns(reportDate);
        _validationDataMock.Setup(s => s.GetReportExceptions(reportDate, exceptionCategory)).ReturnsAsync(reportExceptions);
        _paginationServiceMock.Setup(p => p.GetPaginatedResult(It.IsAny<IQueryable<ValidationException>>(), It.IsAny<int>(), It.IsAny<int>())).Returns(paginatedResult);
        _paginationServiceMock.Setup(p => p.AddNavigationHeaders(_requestMock.Object, paginatedResult)).Returns(expectedHeaders);

        // Act
        var result = await _service.Run(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

        var responseData = DeserializeResponseBody<PaginationResult<ValidationException>>(result);
        Assert.IsNotNull(responseData);
        Assert.AreEqual(2, responseData.Items.Count());
        responseData.Items.Should().OnlyContain(item => item.Category == 12 || item.Category == 13);
    }

    [TestMethod]
    public async Task Run_WhenIsReportTrueWithFutureDate_ReturnsBadRequest()
    {
        // Arrange
        var futureDate = DateTime.Now.Date.AddDays(1);
        SetupQueryParameters(new Dictionary<string, string> { { "isReport", "true" }, { "reportDate", futureDate.ToString("yyyy-MM-dd") } });

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "exceptionId", 0)).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "page", 0)).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsBool(_requestMock.Object, "isReport", false)).Returns(true);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsDateTime(_requestMock.Object, "reportDate")).Returns(futureDate);

        // Act
        var result = await _service.Run(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_WhenIsReportTrueWithNullDate_ReturnsReportExceptions()
    {
        // Arrange
        var exceptionCategory = ExceptionCategory.NBO;
        var allReportExceptions = _exceptionList.Where(e => e.Category == 12 || e.Category == 13).ToList();
        SetupQueryParameters(new Dictionary<string, string> { { "isReport", "true" } });

        var paginatedResult = new PaginationResult<ValidationException>
        {
            Items = allReportExceptions,
            HasNextPage = false,
            HasPreviousPage = false,
            CurrentPage = 1,
            TotalItems = allReportExceptions.Count,
            TotalPages = 1,
        };

        var expectedHeaders = new Dictionary<string, string> { { "X-Total-Count", allReportExceptions.Count.ToString() } };

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "exceptionId", 0)).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "page", 0)).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "pageSize", 0)).Returns(10);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsBool(_requestMock.Object, "isReport", false)).Returns(true);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsDateTime(_requestMock.Object, "reportDate")).Returns((DateTime?)null);
        _validationDataMock.Setup(s => s.GetReportExceptions(null, exceptionCategory)).ReturnsAsync(allReportExceptions);
        _paginationServiceMock.Setup(p => p.GetPaginatedResult(It.IsAny<IQueryable<ValidationException>>(), It.IsAny<int>(), It.IsAny<int>())).Returns(paginatedResult);
        _paginationServiceMock.Setup(p => p.AddNavigationHeaders(_requestMock.Object, paginatedResult)).Returns(expectedHeaders);

        // Act
        var result = await _service.Run(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

        var responseData = DeserializeResponseBody<PaginationResult<ValidationException>>(result);
        Assert.IsNotNull(responseData);
        Assert.IsTrue(responseData.Items.Any());
        responseData.Items.Should().OnlyContain(item => item.Category == 12 || item.Category == 13);
    }

    [TestMethod]
    public async Task Run_WhenIsReportTrueWithTodaysDate_ReturnsReportExceptions()
    {
        // Arrange
        var todaysDate = DateTime.Now.Date;
        var exceptionCategory = ExceptionCategory.NBO;
        var todaysReportExceptions = _exceptionList.Where(e => e.DateCreated!.Value.Date == todaysDate && (e.Category == 12 || e.Category == 13)).ToList();
        SetupQueryParameters(new Dictionary<string, string> { { "isReport", "true" }, { "reportDate", todaysDate.ToString("yyyy-MM-dd") } });

        var paginatedResult = new PaginationResult<ValidationException>
        {
            Items = todaysReportExceptions,
            HasNextPage = false,
            HasPreviousPage = false,
            CurrentPage = 1,
            TotalItems = todaysReportExceptions.Count,
            TotalPages = 1,
        };

        var expectedHeaders = new Dictionary<string, string> { { "X-Total-Count", todaysReportExceptions.Count.ToString() } };

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "exceptionId", 0)).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "page", 0)).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "pageSize", 0)).Returns(10);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsBool(_requestMock.Object, "isReport", false)).Returns(true);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsDateTime(_requestMock.Object, "reportDate")).Returns(todaysDate);
        _validationDataMock.Setup(s => s.GetReportExceptions(todaysDate, exceptionCategory)).ReturnsAsync(todaysReportExceptions);
        _paginationServiceMock.Setup(p => p.GetPaginatedResult(It.IsAny<IQueryable<ValidationException>>(), It.IsAny<int>(), It.IsAny<int>())).Returns(paginatedResult);
        _paginationServiceMock.Setup(p => p.AddNavigationHeaders(_requestMock.Object, paginatedResult)).Returns(expectedHeaders);

        // Act
        var result = await _service.Run(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

        var responseData = DeserializeResponseBody<PaginationResult<ValidationException>>(result);
        Assert.IsNotNull(responseData);
        responseData.Items.Should().OnlyContain(item => item.Category == 12 || item.Category == 13);
    }

    [TestMethod]
    public async Task Run_WhenIsReportTrueThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var reportDate = DateTime.Now.Date.AddDays(-1);
        SetupQueryParameters(new Dictionary<string, string> { { "isReport", "true" }, { "reportDate", reportDate.ToString("yyyy-MM-dd") } });

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "exceptionId", 0)).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "page", 0)).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsBool(_requestMock.Object, "isReport", false)).Returns(true);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsDateTime(_requestMock.Object, "reportDate")).Returns(reportDate);
        _validationDataMock.Setup(s => s.GetReportExceptions(reportDate, It.IsAny<ExceptionCategory>())).ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.Run(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
        _loggerMock.VerifyLogger(LogLevel.Error, "Error processing: GetValidationExceptions validation exceptions request");
    }

    [TestMethod]
    public async Task Run_WithDifferentExceptionCategory_ReturnsCorrectExceptions()
    {
        // Arrange
        var exceptionCategory = ExceptionCategory.NBO;
        SetupQueryParameters(new Dictionary<string, string> { { "exceptionCategory", "BO" } });

        var boExceptions = _exceptionList.Where(e => e.Category == 10).ToList();
        var paginatedResult = new PaginationResult<ValidationException>
        {
            Items = boExceptions,
            HasNextPage = false,
            HasPreviousPage = false,
            CurrentPage = 1,
            TotalItems = boExceptions.Count,
            TotalPages = 1,
        };

        var expectedHeaders = new Dictionary<string, string> { { "X-Total-Count", boExceptions.Count.ToString() } };

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "exceptionId", 0)).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "page", 0)).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "pageSize", 0)).Returns(10);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsDateTime(_requestMock.Object, "reportDate")).Returns((DateTime?)null);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsBool(_requestMock.Object, "isReport", false)).Returns(false);
        _validationDataMock.Setup(s => s.GetFilteredExceptions(ExceptionStatus.All, SortOrder.Descending, exceptionCategory, It.IsAny<SortBy?>(), It.IsAny<int?>(), It.IsAny<DateTime?>())).ReturnsAsync(boExceptions);
        _paginationServiceMock.Setup(p => p.GetPaginatedResult(It.IsAny<IQueryable<ValidationException>>(), It.IsAny<int>(), It.IsAny<int>())).Returns(paginatedResult);
        _paginationServiceMock.Setup(p => p.AddNavigationHeaders(_requestMock.Object, paginatedResult)).Returns(expectedHeaders);

        // Act
        var result = await _service.Run(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

        var responseData = DeserializeResponseBody<PaginationResult<ValidationException>>(result);
        Assert.IsNotNull(responseData);
    }

    [TestMethod]
    public async Task Run_WithMultipleFilters_AppliesAllCorrectly()
    {
        // Arrange
        var exceptionStatus = ExceptionStatus.Raised;
        var sortOrder = SortOrder.Ascending;
        var exceptionCategory = ExceptionCategory.NBO;
        var pageNumber = 2;

        SetupQueryParameters(new Dictionary<string, string>
    {
        { "exceptionStatus", "Raised" },
        { "sortOrder", "Ascending" },
        { "exceptionCategory", "NBO" },
        { "page", pageNumber.ToString() }
    });

        var filteredAndSortedExceptions = _exceptionList.Where(e => e.ServiceNowId != null).OrderBy(e => e.DateCreated).ToList();

        var paginatedResult = new PaginationResult<ValidationException>
        {
            Items = filteredAndSortedExceptions.Skip(1).Take(1).ToList(),
            HasNextPage = false,
            HasPreviousPage = true,
            CurrentPage = pageNumber,
            TotalItems = filteredAndSortedExceptions.Count,
            TotalPages = 2,
        };

        var expectedHeaders = new Dictionary<string, string>
        {
            { "X-Total-Count", filteredAndSortedExceptions.Count.ToString() },
            { "X-Page", pageNumber.ToString() }
        };

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "exceptionId", 0)).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "page", 0)).Returns(pageNumber);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "pageSize", 0)).Returns(10);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsDateTime(_requestMock.Object, "reportDate")).Returns((DateTime?)null);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsBool(_requestMock.Object, "isReport", false)).Returns(false);
        _validationDataMock.Setup(s => s.GetFilteredExceptions(exceptionStatus, sortOrder, exceptionCategory, It.IsAny<SortBy?>(), It.IsAny<int?>(), It.IsAny<DateTime?>())).ReturnsAsync(filteredAndSortedExceptions);
        _paginationServiceMock.Setup(p => p.GetPaginatedResult(It.IsAny<IQueryable<ValidationException>>(), It.IsAny<int>(), It.IsAny<int>())).Returns(paginatedResult);
        _paginationServiceMock.Setup(p => p.AddNavigationHeaders(_requestMock.Object, paginatedResult)).Returns(expectedHeaders);

        // Act
        var result = await _service.Run(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

        var responseData = DeserializeResponseBody<PaginationResult<ValidationException>>(result);
        Assert.IsNotNull(responseData);
        Assert.AreEqual(pageNumber, responseData.CurrentPage);
        Assert.AreEqual(1, responseData.Items.Count());
        Assert.IsTrue(responseData.HasPreviousPage);
        Assert.IsFalse(responseData.HasNextPage);
    }

    [TestMethod]
    public async Task Run_ValidExceptionId_ReturnsExceptionById()
    {
        // Arrange
        var exceptionId = 1;
        SetupQueryParameters(new Dictionary<string, string> { { "exceptionId", exceptionId.ToString() } });

        var expectedException = _exceptionList.First(f => f.ExceptionId == exceptionId);

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "exceptionId", 0)).Returns(exceptionId);
        _validationDataMock.Setup(s => s.GetExceptionById(exceptionId)).ReturnsAsync(expectedException);

        // Act
        var result = await _service.Run(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

        var responseData = DeserializeResponseBody<ValidationException>(result);
        Assert.IsNotNull(responseData);
        Assert.AreEqual(exceptionId, responseData.ExceptionId);
        Assert.AreEqual("Cohort1", responseData.CohortName);
        Assert.AreEqual("1111111111", responseData.NhsNumber);
        Assert.AreEqual("RuleA", responseData.RuleDescription);
        Assert.AreEqual("ServiceNow1", responseData.ServiceNowId);
    }

    [TestMethod]
    public async Task Run_ExceptionIdIsOutOfRange_ReturnsNoContent()
    {
        // Arrange
        var exceptionId = 999;
        SetupQueryParameters(new Dictionary<string, string> { { "exceptionId", exceptionId.ToString() } });

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "exceptionId", 0)).Returns(exceptionId);
        _validationDataMock.Setup(s => s.GetExceptionById(exceptionId)).ReturnsAsync((ValidationException?)null);

        // Act
        var result = await _service.Run(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
        Assert.AreEqual(0, result.Body.Length);
    }

    [TestMethod]
    public async Task Run_NoExceptionsFound_ReturnsOkWithEmptyList()
    {
        // Arrange
        SetupQueryParameters();

        var emptyPaginatedResult = new PaginationResult<ValidationException>
        {
            Items = [],
            HasNextPage = false,
            HasPreviousPage = false,
            CurrentPage = 1,
            TotalItems = 0,
            TotalPages = 0,
        };

        var expectedHeaders = new Dictionary<string, string> { { "X-Total-Count", "0" } };

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "exceptionId", 0)).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "page", 0)).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "pageSize", 0)).Returns(10);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsDateTime(_requestMock.Object, "reportDate")).Returns((DateTime?)null);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsBool(_requestMock.Object, "isReport", false)).Returns(false);
        _validationDataMock.Setup(s => s.GetFilteredExceptions(ExceptionStatus.All, SortOrder.Descending, _exceptionCategory, It.IsAny<SortBy?>(), It.IsAny<int?>(), It.IsAny<DateTime?>())).ReturnsAsync(new List<ValidationException>());
        _paginationServiceMock.Setup(p => p.GetPaginatedResult(It.IsAny<IQueryable<ValidationException>>(), It.IsAny<int>(), It.IsAny<int>())).Returns(emptyPaginatedResult);
        _paginationServiceMock.Setup(p => p.AddNavigationHeaders(_requestMock.Object, emptyPaginatedResult)).Returns(expectedHeaders);

        // Act
        var result = await _service.Run(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

        var responseData = DeserializeResponseBody<PaginationResult<ValidationException>>(result);
        Assert.IsNotNull(responseData);
        Assert.AreEqual(0, responseData.Items.Count());
        Assert.AreEqual(0, responseData.TotalItems);
        Assert.AreEqual(0, responseData.TotalPages);
    }

    [TestMethod]
    public async Task Run_ThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        SetupQueryParameters();
        var exceptionMessage = "Simulated failure";

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "exceptionId", 0)).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "page", 0)).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsDateTime(_requestMock.Object, "reportDate")).Returns((DateTime?)null);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsBool(_requestMock.Object, "isReport", false)).Returns(false);
        _validationDataMock.Setup(s => s.GetFilteredExceptions(ExceptionStatus.All, SortOrder.Descending, _exceptionCategory, It.IsAny<SortBy?>(), It.IsAny<int?>(), It.IsAny<DateTime?>())).ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _service.Run(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
        _loggerMock.VerifyLogger(LogLevel.Error, "Error processing: GetValidationExceptions validation exceptions request");
    }

    [TestMethod]
    public async Task Run_WithExceptionStatus_ReturnsFilteredExceptions()
    {
        // Arrange
        var exceptionStatus = ExceptionStatus.Raised;
        SetupQueryParameters(new Dictionary<string, string> { { "exceptionStatus", "Raised" } });

        var filteredExceptions = _exceptionList.Take(2).ToList();
        var paginatedResult = new PaginationResult<ValidationException>
        {
            Items = filteredExceptions,
            HasNextPage = false,
            HasPreviousPage = false,
            CurrentPage = 1,
            TotalItems = 2,
            TotalPages = 1,
        };

        var expectedHeaders = new Dictionary<string, string> { { "X-Total-Count", "2" } };

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "exceptionId", 0)).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "page", 0)).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "pageSize", 0)).Returns(10);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsDateTime(_requestMock.Object, "reportDate")).Returns((DateTime?)null);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsBool(_requestMock.Object, "isReport", false)).Returns(false);
        _validationDataMock.Setup(s => s.GetFilteredExceptions(exceptionStatus, SortOrder.Descending, _exceptionCategory, It.IsAny<SortBy?>(), It.IsAny<int?>(), It.IsAny<DateTime?>())).ReturnsAsync(filteredExceptions);
        _paginationServiceMock.Setup(p => p.GetPaginatedResult(It.IsAny<IQueryable<ValidationException>>(), It.IsAny<int>(), It.IsAny<int>())).Returns(paginatedResult);
        _paginationServiceMock.Setup(p => p.AddNavigationHeaders(_requestMock.Object, paginatedResult)).Returns(expectedHeaders);

        // Act
        var result = await _service.Run(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

        var responseData = DeserializeResponseBody<PaginationResult<ValidationException>>(result);
        Assert.IsNotNull(responseData);
        Assert.AreEqual(2, responseData.Items.Count());
        Assert.AreEqual(2, responseData.TotalItems);
    }

    [TestMethod]
    public async Task Run_WithSortOrder_ReturnsOrderedExceptions()
    {
        // Arrange
        SetupQueryParameters(new Dictionary<string, string> { { "sortOrder", "Ascending" } });

        var sortedExceptions = _exceptionList.OrderBy(e => e.DateCreated).ToList();
        var paginatedResult = new PaginationResult<ValidationException>
        {
            Items = sortedExceptions,
            HasNextPage = false,
            HasPreviousPage = false,
            CurrentPage = 1,
            TotalItems = sortedExceptions.Count,
            TotalPages = 1,
        };

        var expectedHeaders = new Dictionary<string, string> { { "X-Total-Count", sortedExceptions.Count.ToString() } };

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "exceptionId", 0)).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "page", 0)).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "pageSize", 0)).Returns(10);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsDateTime(_requestMock.Object, "reportDate")).Returns((DateTime?)null);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsBool(_requestMock.Object, "isReport", false)).Returns(false);
        _validationDataMock.Setup(s => s.GetFilteredExceptions(ExceptionStatus.All, SortOrder.Ascending, _exceptionCategory, It.IsAny<SortBy?>(), It.IsAny<int?>(), It.IsAny<DateTime?>())).ReturnsAsync(sortedExceptions);
        _paginationServiceMock.Setup(p => p.GetPaginatedResult(It.IsAny<IQueryable<ValidationException>>(), It.IsAny<int>(), It.IsAny<int>())).Returns(paginatedResult);
        _paginationServiceMock.Setup(p => p.AddNavigationHeaders(_requestMock.Object, paginatedResult)).Returns(expectedHeaders);

        // Act
        var result = await _service.Run(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

        var responseData = DeserializeResponseBody<PaginationResult<ValidationException>>(result);
        Assert.IsNotNull(responseData);
        responseData.Items.Should().BeInAscendingOrder(x => x.DateCreated);
    }

    [TestMethod]
    public async Task Run_WithPageParameter_ReturnsPaginatedResults()
    {
        // Arrange
        var pageNumber = 2;
        SetupQueryParameters(new Dictionary<string, string> { { "page", pageNumber.ToString() } });

        var page2Items = _exceptionList.Skip(1).Take(2).ToList();
        var paginatedResult = new PaginationResult<ValidationException>
        {
            Items = page2Items,
            HasNextPage = true,
            HasPreviousPage = true,
            CurrentPage = pageNumber,
            TotalItems = _exceptionList.Count,
            TotalPages = 3,
        };

        var expectedHeaders = new Dictionary<string, string> {
            { "X-Total-Count", _exceptionList.Count.ToString() },
            { "X-Page", pageNumber.ToString() }
        };

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "exceptionId", 0)).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "page", 0)).Returns(pageNumber);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "pageSize", 0)).Returns(10);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsDateTime(_requestMock.Object, "reportDate")).Returns((DateTime?)null);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsBool(_requestMock.Object, "isReport", false)).Returns(false);
        _validationDataMock.Setup(s => s.GetFilteredExceptions(ExceptionStatus.All, SortOrder.Descending, _exceptionCategory, It.IsAny<SortBy?>(), It.IsAny<int?>(), It.IsAny<DateTime?>())).ReturnsAsync(_exceptionList);
        _paginationServiceMock.Setup(p => p.GetPaginatedResult(It.IsAny<IQueryable<ValidationException>>(), It.IsAny<int>(), It.IsAny<int>())).Returns(paginatedResult);
        _paginationServiceMock.Setup(p => p.AddNavigationHeaders(_requestMock.Object, paginatedResult)).Returns(expectedHeaders);

        // Act
        var result = await _service.Run(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

        var responseData = DeserializeResponseBody<PaginationResult<ValidationException>>(result);
        Assert.IsNotNull(responseData);
        Assert.AreEqual(pageNumber, responseData.CurrentPage);
        Assert.AreEqual(2, responseData.Items.Count());
        Assert.IsTrue(responseData.HasNextPage);
        Assert.IsTrue(responseData.HasPreviousPage);
        Assert.AreEqual(3, responseData.TotalPages);
    }

    [TestMethod]
    public async Task Run_WithPageZero_DefaultsToPage1()
    {
        // Arrange
        SetupQueryParameters(new Dictionary<string, string> { { "page", "0" } });

        var paginatedResult = new PaginationResult<ValidationException>
        {
            Items = _exceptionList,
            HasNextPage = false,
            HasPreviousPage = false,
            CurrentPage = 1,
            TotalItems = _exceptionList.Count,
            TotalPages = 1,

        };

        var expectedHeaders = new Dictionary<string, string> { { "X-Total-Count", _exceptionList.Count.ToString() } };

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "exceptionId", 0)).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "page", 0)).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "pageSize", 0)).Returns(10);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsDateTime(_requestMock.Object, "reportDate")).Returns((DateTime?)null);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsBool(_requestMock.Object, "isReport", false)).Returns(false);
        _validationDataMock.Setup(s => s.GetFilteredExceptions(ExceptionStatus.All, SortOrder.Descending, _exceptionCategory, It.IsAny<SortBy?>(), It.IsAny<int?>(), It.IsAny<DateTime?>())).ReturnsAsync(_exceptionList);
        _paginationServiceMock.Setup(p => p.GetPaginatedResult(It.IsAny<IQueryable<ValidationException>>(), It.IsAny<int>(), It.IsAny<int>())).Returns(paginatedResult);
        _paginationServiceMock.Setup(p => p.AddNavigationHeaders(_requestMock.Object, paginatedResult)).Returns(expectedHeaders);

        // Act
        var result = await _service.Run(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

        var responseData = DeserializeResponseBody<PaginationResult<ValidationException>>(result);
        Assert.IsNotNull(responseData);
        Assert.AreEqual(1, responseData.CurrentPage);
    }

    [TestMethod]
    public async Task Run_WithNegativePage_DefaultsToPage1()
    {
        // Arrange
        SetupQueryParameters(new Dictionary<string, string> { { "page", "-5" } });

        var paginatedResult = new PaginationResult<ValidationException>
        {
            Items = _exceptionList,
            HasNextPage = false,
            HasPreviousPage = false,
            CurrentPage = 1,
            TotalItems = _exceptionList.Count,
            TotalPages = 1,

        };

        var expectedHeaders = new Dictionary<string, string> { { "X-Total-Count", _exceptionList.Count.ToString() } };

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "exceptionId", 0)).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "page", 0)).Returns(-5);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "pageSize", 0)).Returns(10);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsDateTime(_requestMock.Object, "reportDate")).Returns((DateTime?)null);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsBool(_requestMock.Object, "isReport", false)).Returns(false);
        _validationDataMock.Setup(s => s.GetFilteredExceptions(ExceptionStatus.All, SortOrder.Descending, _exceptionCategory, It.IsAny<SortBy?>(), It.IsAny<int?>(), It.IsAny<DateTime?>())).ReturnsAsync(_exceptionList);
        _paginationServiceMock.Setup(p => p.GetPaginatedResult(It.IsAny<IQueryable<ValidationException>>(), It.IsAny<int>(), It.IsAny<int>())).Returns(paginatedResult);
        _paginationServiceMock.Setup(p => p.AddNavigationHeaders(_requestMock.Object, paginatedResult)).Returns(expectedHeaders);

        // Act
        var result = await _service.Run(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

        var responseData = DeserializeResponseBody<PaginationResult<ValidationException>>(result);
        Assert.IsNotNull(responseData);
        Assert.AreEqual(1, responseData.CurrentPage);
    }

    [TestMethod]
    public async Task Run_GetExceptionByIdThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var exceptionId = 1;
        SetupQueryParameters(new Dictionary<string, string> { { "exceptionId", exceptionId.ToString() } });
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "exceptionId", 0)).Returns(exceptionId);
        _validationDataMock.Setup(s => s.GetExceptionById(exceptionId)).ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.Run(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
        _loggerMock.VerifyLogger(LogLevel.Error, "Error processing: GetValidationExceptions validation exceptions request");
    }

    [TestMethod]
    public async Task Run_PaginationServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var exceptionCategory = ExceptionCategory.NBO;
        SetupQueryParameters(new Dictionary<string, string> { { "isReport", "true" } });

        var reportExceptions = _exceptionList.Where(e => e.Category == 12 || e.Category == 13).ToList();

        var paginatedResult = new PaginationResult<ValidationException>
        {
            Items = reportExceptions,
            HasNextPage = false,
            HasPreviousPage = false,
            CurrentPage = 1,
            TotalItems = reportExceptions.Count,
            TotalPages = 1,

        };

        var expectedHeaders = new Dictionary<string, string> { { "X-Total-Count", reportExceptions.Count.ToString() } };

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "exceptionId", 0)).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "page", 0)).Returns(0);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "pageSize", 0)).Returns(10);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsBool(_requestMock.Object, "isReport", false)).Returns(true);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsDateTime(_requestMock.Object, "reportDate")).Returns((DateTime?)null);
        _validationDataMock.Setup(s => s.GetReportExceptions(null, exceptionCategory)).ReturnsAsync(reportExceptions);
        _paginationServiceMock.Setup(p => p.GetPaginatedResult(It.IsAny<IQueryable<ValidationException>>(), It.IsAny<int>(), It.IsAny<int>())).Returns(paginatedResult);
        _paginationServiceMock.Setup(p => p.AddNavigationHeaders(_requestMock.Object, paginatedResult)).Returns(expectedHeaders);

        // Act
        var result = await _service.Run(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

        var responseData = DeserializeResponseBody<PaginationResult<ValidationException>>(result);
        Assert.IsNotNull(responseData);
        Assert.AreEqual(2, responseData.Items.Count());
        Assert.AreEqual(1, responseData.CurrentPage);
        Assert.AreEqual(2, responseData.TotalItems);
        Assert.IsFalse(responseData.HasNextPage);
        Assert.IsFalse(responseData.HasPreviousPage);
        Assert.IsTrue(result.Headers.Contains("X-Total-Count"));
        Assert.AreEqual("2", result.Headers.GetValues("X-Total-Count").First());
    }

    [TestMethod]
    [DataRow("NhsNumber", null, DisplayName = "Missing searchValue")]
    [DataRow("NhsNumber", "", DisplayName = "Empty searchValue")]
    [DataRow("ExceptionId", "   ", DisplayName = "Whitespace searchValue")]
    public async Task GetValidationExceptionsByType_InvalidSearchValue_ReturnsBadRequest(string searchType, string? searchValue)
    {
        // Arrange
        var queryParams = new Dictionary<string, string> { { "searchType", searchType } };
        if (searchValue != null) queryParams.Add("searchValue", searchValue);
        SetupQueryParameters(queryParams);

        // Act
        var result = await _service.GetValidationExceptionsByType(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    [DataRow("abc", DisplayName = "Non-numeric ExceptionId")]
    [DataRow("12.5", DisplayName = "Decimal ExceptionId")]
    [DataRow("-abc", DisplayName = "Invalid negative format")]
    public async Task GetValidationExceptionsByType_ExceptionIdInvalidFormat_ReturnsBadRequest(string invalidId)
    {
        // Arrange
        SetupQueryParameters(new Dictionary<string, string> { { "searchType", "ExceptionId" }, { "searchValue", invalidId } });
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "page", 1)).Returns(1);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "pageSize", 10)).Returns(10);

        // Act
        var result = await _service.GetValidationExceptionsByType(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    [DataRow("9434765910", DisplayName = "Invalid checksum")]
    [DataRow("123456789", DisplayName = "Too short (9 digits)")]
    [DataRow("12345678901", DisplayName = "Too long (11 digits)")]
    [DataRow("943476591A", DisplayName = "Contains letter")]
    [DataRow("94347659!9", DisplayName = "Contains special character")]
    public async Task GetValidationExceptionsByType_NhsNumberInvalidFormat_ReturnsBadRequest(string invalidNhsNumber)
    {
        // Arrange
        SetupQueryParameters(new Dictionary<string, string> { { "searchType", "NhsNumber" }, { "searchValue", invalidNhsNumber } });
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "page", 1)).Returns(1);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "pageSize", 10)).Returns(10);

        // Act
        var result = await _service.GetValidationExceptionsByType(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task GetValidationExceptionsByType_ExceptionIdValidId_ReturnsException()
    {
        // Arrange
        var exceptionId = 1;
        var expectedException = _exceptionList.First(f => f.ExceptionId == exceptionId);
        SetupQueryParameters(new Dictionary<string, string> { { "searchType", "ExceptionId" }, { "searchValue", exceptionId.ToString() } });

        var paginatedResult = new PaginationResult<ValidationException>
        {
            Items = [expectedException],
            HasNextPage = false,
            HasPreviousPage = false,
            CurrentPage = 1,
            TotalItems = 1,
            TotalPages = 1,
        };
        var expectedHeaders = new Dictionary<string, string> { { "X-Total-Count", "1" } };

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "page", 1)).Returns(1);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "pageSize", 10)).Returns(10);
        _validationDataMock.Setup(s => s.GetExceptionById(exceptionId)).ReturnsAsync(expectedException);
        _paginationServiceMock.Setup(p => p.GetPaginatedResult(It.IsAny<IQueryable<ValidationException>>(), It.IsAny<int>(), It.IsAny<int>())).Returns(paginatedResult);
        _paginationServiceMock.Setup(p => p.AddNavigationHeaders(_requestMock.Object, paginatedResult)).Returns(expectedHeaders);

        // Act
        var result = await _service.GetValidationExceptionsByType(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

        var responseData = DeserializeResponseBody<ValidationExceptionsResponse>(result);
        Assert.IsNotNull(responseData);
        Assert.AreEqual(SearchType.ExceptionId, responseData.SearchType);
        Assert.AreEqual(exceptionId.ToString(), responseData.SearchValue);
        Assert.AreEqual(1, responseData.PaginatedExceptions.Items.Count());
        Assert.AreEqual(0, responseData.Reports.Count);
    }

    [TestMethod]
    public async Task GetValidationExceptionsByType_ExceptionIdNotFound_ReturnsNoContent()
    {
        // Arrange
        var exceptionId = 999;
        SetupQueryParameters(new Dictionary<string, string> { { "searchType", "ExceptionId" }, { "searchValue", exceptionId.ToString() } });

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "page", 1)).Returns(1);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "pageSize", 10)).Returns(10);
        _validationDataMock.Setup(s => s.GetExceptionById(exceptionId)).ReturnsAsync((ValidationException?)null);

        // Act
        var result = await _service.GetValidationExceptionsByType(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
    }

    [TestMethod]
    [DataRow("9434765919", null, DisplayName = "Standard format")]
    [DataRow("943 476 5919", "9434765919", DisplayName = "With spaces")]
    public async Task GetValidationExceptionsByType_NhsNumberValid_ReturnsResults(string inputNhsNumber, string? expectedCleanedNumber)
    {
        // Arrange
        var cleanedNhsNumber = expectedCleanedNumber ?? inputNhsNumber;
        var exceptionsResponse = new ValidationExceptionsResponse
        {
            SearchType = SearchType.NhsNumber,
            SearchValue = cleanedNhsNumber,
            Exceptions = [_exceptionList[0]],
            Reports = [new ValidationExceptionReport { ReportDate = DateTime.Today, Category = 12, ExceptionCount = 1 }]
        };
        SetupQueryParameters(new Dictionary<string, string> { { "searchType", "NhsNumber" }, { "searchValue", inputNhsNumber } });

        var paginatedResult = new PaginationResult<ValidationException>
        {
            Items = exceptionsResponse.Exceptions,
            HasNextPage = false,
            HasPreviousPage = false,
            CurrentPage = 1,
            TotalItems = 1,
            TotalPages = 1,
        };
        var expectedHeaders = new Dictionary<string, string> { { "X-Total-Count", "1" } };

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "page", 1)).Returns(1);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "pageSize", 10)).Returns(10);
        _validationDataMock.Setup(s => s.GetExceptionsByNhsNumber(cleanedNhsNumber)).ReturnsAsync(exceptionsResponse);
        _paginationServiceMock.Setup(p => p.GetPaginatedResult(It.IsAny<IQueryable<ValidationException>>(), It.IsAny<int>(), It.IsAny<int>())).Returns(paginatedResult);
        _paginationServiceMock.Setup(p => p.AddNavigationHeaders(_requestMock.Object, paginatedResult)).Returns(expectedHeaders);

        // Act
        var result = await _service.GetValidationExceptionsByType(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _validationDataMock.Verify(s => s.GetExceptionsByNhsNumber(cleanedNhsNumber), Times.Once);
    }

    [TestMethod]
    public async Task GetValidationExceptionsByType_NhsNumber_NoResults_ReturnsNoContent()
    {
        // Arrange
        var nhsNumber = "9434765919";
        var exceptionsResponse = new ValidationExceptionsResponse
        {
            SearchType = SearchType.NhsNumber,
            SearchValue = nhsNumber,
            Exceptions = [],
            Reports = []
        };
        SetupQueryParameters(new Dictionary<string, string> { { "searchType", "NhsNumber" }, { "searchValue", nhsNumber } });

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "page", 1)).Returns(1);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "pageSize", 10)).Returns(10);
        _validationDataMock.Setup(s => s.GetExceptionsByNhsNumber(nhsNumber)).ReturnsAsync(exceptionsResponse);

        // Act
        var result = await _service.GetValidationExceptionsByType(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
    }

    [TestMethod]
    public async Task GetValidationExceptionsByType_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var nhsNumber = "9434765919";
        var exceptions = _exceptionList.Take(5).ToList();
        var exceptionsResponse = new ValidationExceptionsResponse
        {
            SearchType = SearchType.NhsNumber,
            SearchValue = nhsNumber,
            Exceptions = exceptions,
            Reports = []
        };
        SetupQueryParameters(new Dictionary<string, string> { { "searchType", "NhsNumber" }, { "searchValue", nhsNumber }, { "page", "2" }, { "pageSize", "2" } });

        var page2Items = exceptions.Skip(2).Take(2).ToList();
        var paginatedResult = new PaginationResult<ValidationException>
        {
            Items = page2Items,
            HasNextPage = true,
            HasPreviousPage = true,
            CurrentPage = 2,
            TotalItems = 5,
            TotalPages = 3,
        };
        var expectedHeaders = new Dictionary<string, string> { { "X-Total-Count", "5" }, { "X-Page", "2" } };

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "page", 1)).Returns(2);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "pageSize", 10)).Returns(2);
        _validationDataMock.Setup(s => s.GetExceptionsByNhsNumber(nhsNumber)).ReturnsAsync(exceptionsResponse);
        _paginationServiceMock.Setup(p => p.GetPaginatedResult(It.IsAny<IQueryable<ValidationException>>(), 2, 2)).Returns(paginatedResult);
        _paginationServiceMock.Setup(p => p.AddNavigationHeaders(_requestMock.Object, paginatedResult)).Returns(expectedHeaders);

        // Act
        var result = await _service.GetValidationExceptionsByType(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

        var responseData = DeserializeResponseBody<ValidationExceptionsResponse>(result);
        Assert.IsNotNull(responseData);
        Assert.AreEqual(2, responseData.PaginatedExceptions.CurrentPage);
        Assert.AreEqual(2, responseData.PaginatedExceptions.Items.Count());
        Assert.IsTrue(responseData.PaginatedExceptions.HasNextPage);
        Assert.IsTrue(responseData.PaginatedExceptions.HasPreviousPage);
    }

    [TestMethod]
    public async Task GetValidationExceptionsByType_DefaultSearchType_IsNhsNumber()
    {
        // Arrange - no searchType specified, should default to NhsNumber
        var nhsNumber = "9434765919";
        var exceptionsResponse = new ValidationExceptionsResponse
        {
            SearchType = SearchType.NhsNumber,
            SearchValue = nhsNumber,
            Exceptions = [_exceptionList[0]],
            Reports = []
        };
        SetupQueryParameters(new Dictionary<string, string> { { "searchValue", nhsNumber } });

        var paginatedResult = new PaginationResult<ValidationException>
        {
            Items = exceptionsResponse.Exceptions,
            HasNextPage = false,
            HasPreviousPage = false,
            CurrentPage = 1,
            TotalItems = 1,
            TotalPages = 1,
        };
        var expectedHeaders = new Dictionary<string, string> { { "X-Total-Count", "1" } };

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "page", 1)).Returns(1);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "pageSize", 10)).Returns(10);
        _validationDataMock.Setup(s => s.GetExceptionsByNhsNumber(nhsNumber)).ReturnsAsync(exceptionsResponse);
        _paginationServiceMock.Setup(p => p.GetPaginatedResult(It.IsAny<IQueryable<ValidationException>>(), It.IsAny<int>(), It.IsAny<int>())).Returns(paginatedResult);
        _paginationServiceMock.Setup(p => p.AddNavigationHeaders(_requestMock.Object, paginatedResult)).Returns(expectedHeaders);

        // Act
        var result = await _service.GetValidationExceptionsByType(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _validationDataMock.Verify(s => s.GetExceptionsByNhsNumber(nhsNumber), Times.Once);
    }

    [TestMethod]
    public async Task GetValidationExceptionsByType_DatabaseError_ReturnsInternalServerError()
    {
        // Arrange
        var nhsNumber = "9434765919";
        SetupQueryParameters(new Dictionary<string, string> { { "searchType", "NhsNumber" }, { "searchValue", nhsNumber } });

        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "page", 1)).Returns(1);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(_requestMock.Object, "pageSize", 10)).Returns(10);
        _validationDataMock.Setup(s => s.GetExceptionsByNhsNumber(nhsNumber)).ThrowsAsync(new Exception("Database connection error"));

        // Act
        var result = await _service.GetValidationExceptionsByType(_requestMock.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }
}
