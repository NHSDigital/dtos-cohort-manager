namespace NHS.CohortManager.ScreeningDataServices;

using System.Linq.Dynamic.Core;
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
        var pageSize = 20;
        var todayOnly = _httpParserHelper.GetQueryParameterAsBool(req, "todayOnly");

        try
        {
            if (exceptionId != 0)
            {
                return await GetExceptionById(req, exceptionId);
            }

            var todaysExceptions = await _validationData.GetAllExceptions(todayOnly);

            if (!todaysExceptions.Any())
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.NoContent, req);
            }

            var paginatedResults = _paginationService.GetPaginatedResult(todaysExceptions.AsQueryable(), lastId, pageSize, e => e.ExceptionId.Value);

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, JsonSerializer.Serialize(paginatedResults));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing: {Function} validation exceptions request", nameof(GetValidationExceptions));
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }

    private async Task<HttpResponseData> GetExceptionById(HttpRequestData req, int exceptionId)
    {
        var exceptionById = await _validationData.GetExceptionById(exceptionId);
        if (exceptionById == null)
        {
            _logger.LogError("Validation Exception not found with ID: {ExceptionId}", exceptionId);
            return _createResponse.CreateHttpResponse(HttpStatusCode.NoContent, req);
        }
        return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, JsonSerializer.Serialize(exceptionById)
        );
    }
}
