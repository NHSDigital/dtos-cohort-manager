namespace NHS.CohortManager.ScreeningValidationService;

using System.Net;
using Common;
using Data.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

public class ExceptionDataService
{
    private readonly ILogger<ExceptionDataService> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IValidationData _validationData;

    public ExceptionDataService(ILogger<ExceptionDataService> logger, ICreateResponse createResponse, IValidationData validationData)
    {
        _logger = logger;
        _createResponse = createResponse;
        _validationData = validationData;
    }

    [Function("GetExceptions")]
    public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        foreach (var exception in _validationData.GetAll())
        {
            _logger.LogInformation($"Exception {exception.RuleName} at {exception.DateCreated}");
        }

        return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
    }
}
