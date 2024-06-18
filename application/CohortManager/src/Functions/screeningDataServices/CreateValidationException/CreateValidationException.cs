namespace screeningDataServices;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Model;
using Data.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

public class CreateValidationException
{
    private readonly ILogger<CreateValidationException> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IValidationExceptionData _validationData;

    public CreateValidationException(ILogger<CreateValidationException> logger, ICreateResponse createResponse, IValidationExceptionData validationData)
    {
        _logger = logger;
        _createResponse = createResponse;
        _validationData = validationData;
    }

    [Function("CreateValidationException")]
    public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        ValidationException exception;

        try
        {
            string requestBodyJson;
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBodyJson = reader.ReadToEnd();
            }

            exception = JsonSerializer.Deserialize<ValidationException>(requestBodyJson);
        }
        catch
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        if (_validationData.Create(exception))
        {
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
        }

        return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
    }
}
