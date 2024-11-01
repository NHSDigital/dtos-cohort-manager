namespace screeningDataServices;

using System.Net;
using System.Text.Json;
using Common;
using Common.Interfaces;
using Data.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;

public class GetValidationExceptions
{
    private readonly ILogger<GetValidationExceptions> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IValidationExceptionData _validationData;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IHttpParserHelper _httpParserHelper;


    public GetValidationExceptions(ILogger<GetValidationExceptions> logger, ICreateResponse createResponse, IValidationExceptionData validationData, IExceptionHandler exceptionHandler, IHttpParserHelper httpParserHelper)
    {
        _logger = logger;
        _createResponse = createResponse;
        _validationData = validationData;
        _exceptionHandler = exceptionHandler;
        _httpParserHelper = httpParserHelper;

    }

    [Function(nameof(GetValidationExceptions))]
    public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {

        var ExceptionId = _httpParserHelper.GetQueryParameterAsInt(req, "ExceptionId");
        var validationException = new List<ValidationException>();

        try
        {
            if (ExceptionId == 0)
            {
                validationException = _validationData.GetAllExceptions();
            }
            else
            {
                validationException.Add(_validationData.GetExceptionById(ExceptionId));
                if (_validationData.GetExceptionById(ExceptionId) == null)
                {
                    _logger.LogError("Validation Exception not found with ID: {ExceptionId}", ExceptionId);
                    return _createResponse.CreateHttpResponse(HttpStatusCode.NoContent, req);
                }
            }

            var validationExceptionJson = JsonSerializer.Serialize(validationException);
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, validationExceptionJson);

        }
        catch (Exception ex)
        {
            _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, "", "", "", "N/A");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }
}
