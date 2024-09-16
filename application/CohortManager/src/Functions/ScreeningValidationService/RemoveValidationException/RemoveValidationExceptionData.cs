namespace NHS.CohortManager.ScreeningValidationService;

using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Common;
using Data.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;


public class RemoveValidationExceptionData
{
    private readonly ICreateResponse _createResponse;
    private readonly IExceptionHandler _handleException;
    private readonly IValidationExceptionData _validationExceptionData;

    private readonly ILogger<RemoveValidationExceptionData> _logger;

    public RemoveValidationExceptionData(ICreateResponse createResponse, IExceptionHandler handleException, IValidationExceptionData validationExceptionData, ILogger<RemoveValidationExceptionData> logger)
    {
        _createResponse = createResponse;
        _handleException = handleException;
        _validationExceptionData = validationExceptionData;
        _logger = logger;
    }
    [Function("RemoveValidationExceptionData")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        OldExceptionRecord removeOldException;
        using (var reader = new StreamReader(req.Body, Encoding.UTF8))
        {
            var requestBodyJson = reader.ReadToEnd();
            removeOldException = JsonSerializer.Deserialize<OldExceptionRecord>(requestBodyJson);
        }

        try
        {
            var exceptionRemoved = _validationExceptionData.RemoveOldException(removeOldException.NhsNumber, removeOldException.ScreeningName);
            if (exceptionRemoved)
            {
                _logger.LogInformation("The Last Exception has been removed successfully");
                return _createResponse.CreateHttpResponse(HttpStatusCode.Created, req);
            }
            _logger.LogInformation("The Last Exception has not been removed but no error was thrown");
            //we want to return ok here because an error has not actually occurred
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
        }
        catch (Exception ex)
        {
            await _handleException.CreateSystemExceptionLogFromNhsNumber(ex, removeOldException.NhsNumber, "");

            _logger.LogError("There was exception while removing an old ValidationExceptionRecord for NHS number: {NhsNumber}. StackTrace: {ex}", removeOldException.NhsNumber, ex);
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }
}
