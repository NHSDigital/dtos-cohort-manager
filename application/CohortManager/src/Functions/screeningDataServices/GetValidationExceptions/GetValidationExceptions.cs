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
/// Azure Function for retrieving cohort distribution data based on ScreeningServiceId.
/// </summary>
/// <param name="req">The HTTP request data containing query parameters and request details.</param>
/// <param name="exceptionId">query parameter used to search for an exception by Id..</param>
/// if not exceptionId is passed in the full list of exceptions will be returned
/// <returns>
/// HTTP response with:
/// - 204 No Content if no data is found.
/// - 200 OK - List<GetValidationExceptions> or single GetValidationExcept in JSON format .
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

        try
        {
            if (exceptionId != 0)
            {
                var exceptionById = await _validationData.GetExceptionById(exceptionId);
                return exceptionById == null
                    ? _createResponse.CreateHttpResponse(HttpStatusCode.NoContent, req)
                    : _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, JsonSerializer.Serialize(exceptionById));
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
            if (updateRequest == null || updateRequest.ExceptionId == 0 || string.IsNullOrWhiteSpace(updateRequest.ServiceNowId))
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Invalid request. ExceptionId and ServiceNowId are required.");
            }

            var validationError = ValidateServiceNowId(updateRequest.ServiceNowId);
            if (validationError != null)
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, validationError);
            }

            var updateResult = await _validationData.UpdateExceptionServiceNowId(updateRequest.ExceptionId, updateRequest.ServiceNowId);
            if (!updateResult)
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, $"Failed to update ServiceNow ID or Exception with ID {updateRequest.ExceptionId} not found.");
            }

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, "ServiceNow ID updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing: {Function} update ServiceNow ID request", nameof(UpdateExceptionServiceNowId));
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }

    private static string? ValidateServiceNowId(string serviceNowId)
    {
        if (string.IsNullOrWhiteSpace(serviceNowId))
            return "ServiceNowID is required.";

        if (serviceNowId.Contains(' '))
            return "ServiceNowID cannot contain spaces.";

        if (serviceNowId.Length < 9)
            return "ServiceNowID must be at least 9 characters long.";

        if (!serviceNowId.All(char.IsLetterOrDigit))
            return "ServiceNowID must contain only alphanumeric characters.";

        return null;
    }
}
