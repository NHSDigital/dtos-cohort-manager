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
using Model.Enums;

/// <summary>
/// Azure Function for retrieving validation exceptions.
/// </summary>
/// <param name="req">The HTTP request data containing query parameters and request details.</param>
/// <param name="exceptionId">Query parameter used to search for an exception by Id.</param>
/// <param name="reportDate">Query parameter to retrieve exceptions within 24 hours of the specified date (Confusion and Superseded categories only).</param>
/// If no exceptionId is passed, the full list of exceptions will be returned.
/// <returns>
/// HTTP response with:
/// - 204 No Content if no data is found.
/// - 200 OK - List&lt;ValidationException&gt; or single ValidationException in JSON format.
/// - 400 Bad Request if reportDate is in the future.
/// - 500 Internal Server Error if an exception occurs.
/// </returns>
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

    [Function(nameof(GetValidationExceptions))]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        var exceptionId = _httpParserHelper.GetQueryParameterAsInt(req, "exceptionId");
        var lastId = _httpParserHelper.GetQueryParameterAsInt(req, "lastId");
        var exceptionStatus = HttpParserHelper.GetEnumQueryParameter(req, "exceptionStatus", ExceptionStatus.All);
        var sortOrder = HttpParserHelper.GetEnumQueryParameter(req, "sortOrder", SortOrder.Descending);
        var exceptionCategory = HttpParserHelper.GetEnumQueryParameter(req, "exceptionCategory", ExceptionCategory.NBO);
        var reportDate = _httpParserHelper.GetQueryParameterAsDateTime(req, "reportDate");
        var isReport = _httpParserHelper.GetQueryParameterAsBool(req, "isReport");

        try
        {
            if (isReport)
            {
                var isReportCategories = exceptionCategory == ExceptionCategory.Confusion || exceptionCategory == ExceptionCategory.Superseded;

                if (isReportCategories && !reportDate.HasValue) return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Report date is required when filtering by Confusion or Superseded category.");

                if (reportDate.HasValue && reportDate.Value > DateTime.UtcNow.Date) return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Report date cannot be in the future.");
                if (reportDate.HasValue && !isReportCategories) return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Invalid category for report. Only Confusion and Superseded categories are supported.");

                var reportExceptions = await _validationData.GetReportExceptions(reportDate, exceptionCategory);

                if (reportExceptions?.Count == 0) return _createResponse.CreateHttpResponse(HttpStatusCode.NoContent, req);

                var result = _paginationService.GetPaginatedResult(reportExceptions.AsQueryable(), lastId, e => e.ExceptionId);
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, JsonSerializer.Serialize(result));
            }

            var exceptions = await _validationData.GetAllFilteredExceptions(exceptionStatus, sortOrder, exceptionCategory);

            if (exceptions == null || exceptions.Count == 0)
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.NoContent, req);
            }

            var paginatedResults = _paginationService.GetPaginatedResult(exceptions.AsQueryable(), lastId, e => e.ExceptionId);

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, JsonSerializer.Serialize(paginatedResults));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing: {Function} validation exceptions request", nameof(GetValidationExceptions));
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }

    /// <summary>
    /// Updates the ServiceNow ID for a specific validation exception.
    /// </summary>
    /// <param name="req">The HTTP request data containing the exception ID and ServiceNow ID.</param>
    /// <returns>
    /// HTTP response with:
    /// - 200 OK if the update is successful
    /// - 400 Bad Request if required parameters are missing
    /// - 500 Internal Server Error if an exception occurs
    /// </returns>
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
}
