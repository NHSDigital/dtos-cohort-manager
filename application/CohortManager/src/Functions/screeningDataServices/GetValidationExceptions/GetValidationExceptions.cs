namespace NHS.CohortManager.ScreeningDataServices;

using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Common;
using Common.Interfaces;
using Data.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;
using Model.DTO;
using Model.Enums;
using Model.Pagination;

/// <summary>
/// Azure Function for retrieving and managing validation exceptions.
/// Provides endpoints for exception queries, reports, and ServiceNowId updates.
/// </summary>
public class GetValidationExceptions
{
    private readonly ILogger<GetValidationExceptions> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IValidationExceptionData _validationData;
    private readonly IHttpParserHelper _httpParserHelper;
    private readonly IPaginationService<ValidationException> _paginationService;

    public GetValidationExceptions(ILogger<GetValidationExceptions> logger, ICreateResponse createResponse, IValidationExceptionData validationData, IHttpParserHelper httpParserHelper, IPaginationService<ValidationException> paginationService)
    {
        _logger = logger;
        _createResponse = createResponse;
        _validationData = validationData;
        _httpParserHelper = httpParserHelper;
        _paginationService = paginationService;
    }

    /// <summary>
    /// Retrieves validation exceptions based on query parameters.
    /// Supports single exception lookup, filtered lists, and report-based queries.
    /// </summary>
    /// <param name="req">The HTTP request data containing query parameters.</param>
    /// <returns>
    /// HTTP response containing validation exceptions in JSON format.
    /// Returns 200 OK with data, 204 No Content if empty, 400 Bad Request for validation errors, or 500 Internal Server Error.
    /// </returns>
    [Function(nameof(GetValidationExceptions))]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        var exceptionId = _httpParserHelper.GetQueryParameterAsInt(req, "exceptionId");
        var page = _httpParserHelper.GetQueryParameterAsInt(req, "page");
        var pageSize = _httpParserHelper.GetQueryParameterAsInt(req, "pageSize");
        var exceptionStatus = HttpParserHelper.GetEnumQueryParameter(req, "exceptionStatus", ExceptionStatus.All);
        var sortOrder = HttpParserHelper.GetEnumQueryParameter(req, "sortOrder", SortOrder.Descending);
        var exceptionCategory = HttpParserHelper.GetEnumQueryParameter(req, "exceptionCategory", ExceptionCategory.NBO);
        var reportDate = _httpParserHelper.GetQueryParameterAsDateTime(req, "reportDate");
        var isReport = _httpParserHelper.GetQueryParameterAsBool(req, "isReport");

        try
        {
            if (exceptionId > 0)
            {
                var exceptionById = await _validationData.GetExceptionById(exceptionId);
                return exceptionById == null
                ? _createResponse.CreateHttpResponse(HttpStatusCode.NoContent, req)
                : _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, JsonSerializer.Serialize(exceptionById));
            }

            if (isReport)
            {
                if (reportDate.HasValue && reportDate.Value > DateTime.Now.Date)
                {
                    return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Report date cannot be in the future.");
                }

                var reportExceptions = await _validationData.GetReportExceptions(reportDate, exceptionCategory);
                return CreatePaginatedResponse(req, reportExceptions!.AsQueryable(), page, reportExceptions!.Count);
            }

            var filteredExceptions = await _validationData.GetFilteredExceptions(exceptionStatus, sortOrder, exceptionCategory);
            return CreatePaginatedResponse(req, filteredExceptions!.AsQueryable(), page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing: {Function} validation exceptions request", nameof(GetValidationExceptions));
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }

    private HttpResponseData CreatePaginatedResponse(HttpRequestData request, IQueryable<ValidationException> source, int page, int pageSize)
    {
        var paginatedResult = _paginationService.GetPaginatedResult(source, page, pageSize);
        var headers = _paginationService.AddNavigationHeaders(request, paginatedResult);

        return _createResponse.CreateHttpResponseWithHeaders(HttpStatusCode.OK, request, JsonSerializer.Serialize(paginatedResult), headers);
    }

    /// <summary>
    /// Updates the ServiceNowId for a specific validation exception.
    /// </summary>
    [Function(nameof(UpdateExceptionServiceNowId))]
    public async Task<HttpResponseData> UpdateExceptionServiceNowId([HttpTrigger(AuthorizationLevel.Anonymous, "put")] HttpRequestData req)
    {
        try
        {
            using var bodyReader = new StreamReader(req.Body);
            var requestBody = await bodyReader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Request body cannot be empty.");
            }

            var updateRequest = JsonSerializer.Deserialize<UpdateExceptionServiceNowIdRequest>(requestBody);
            if (updateRequest == null || updateRequest.ExceptionId == 0)
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Invalid request. ExceptionId and ServiceNowId is required.");
            }

            var response = await _validationData.UpdateExceptionServiceNowId(updateRequest.ExceptionId, updateRequest.ServiceNowId);

            if (!response.Success)
            {
                return _createResponse.CreateHttpResponse(response.StatusCode, req, response.Message ?? "Failed to update ServiceNowId");
            }

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, response.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing: {Function} update ServiceNowId request", nameof(UpdateExceptionServiceNowId));
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }

    /// <summary>
    /// Retrieves validation exceptions and reports for a specific NHS number.
    /// </summary>
    [Function(nameof(GetValidationExceptionsByNhsNumber))]
    public async Task<HttpResponseData> GetValidationExceptionsByNhsNumber([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        var nhsNumber = req.Query["nhsNumber"];
        var page = _httpParserHelper.GetQueryParameterAsInt(req, "page", 1);
        var pageSize = _httpParserHelper.GetQueryParameterAsInt(req, "pageSize", 10);

        try
        {
            var (exceptions, reports, nhsNumberResult) = await _validationData.GetExceptionsWithReportsByNhsNumber(nhsNumber!);

            if (!exceptions.Any())
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.NoContent, req);
            }

            var paginatedExceptions = _paginationService.GetPaginatedResult(exceptions, page, pageSize);
            var headers = _paginationService.AddNavigationHeaders(req, paginatedExceptions);

            var result = new ValidationExceptionsByNhsNumberResponse
            {
                NhsNumber = nhsNumberResult,
                Exceptions = paginatedExceptions,
                Reports = reports
            };

            return _createResponse.CreateHttpResponseWithHeaders(HttpStatusCode.OK, req, JsonSerializer.Serialize(result), headers);
        }
        catch (ArgumentException ex)
        {
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving validation exceptions");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }
}
