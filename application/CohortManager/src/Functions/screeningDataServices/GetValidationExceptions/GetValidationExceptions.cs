namespace screeningDataServices;

using System.Net;
using Common;
using Data.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

public class GetValidationExceptions
{
    private readonly ILogger<GetValidationExceptions> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IValidationExceptionData _validationData;

    public GetValidationExceptions(ILogger<GetValidationExceptions> logger, ICreateResponse createResponse, IValidationExceptionData validationData)
    {
        _logger = logger;
        _createResponse = createResponse;
        _validationData = validationData;
    }

    [Function("GetValidationExceptions")]
    public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        foreach (var exception in _validationData.GetAll())
        {
            _logger.LogInformation($"Exception {exception.RuleId} at {exception.DateCreated}");
        }

        return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
    }
}
