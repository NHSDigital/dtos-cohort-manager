namespace NHS.CohortManager.ExceptionService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text;
using System.Text.Json;
using Model;
using Common;
using Data.Database;

public class CreateException
{
    private readonly ILogger<CreateException> _logger;
    private readonly IValidationExceptionData _validationData;

    private readonly ICreateResponse _createResponse;
    public CreateException(ILogger<CreateException> logger, IValidationExceptionData validationExceptionData, ICreateResponse createResponse)
    {
        _logger = logger;
        _validationData = validationExceptionData;
        _createResponse = createResponse;
    }

    [Function("CreateException")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        try
        {
            ValidationException exception;
            var requestBody = "";

            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBody = await reader.ReadToEndAsync();
                exception = JsonSerializer.Deserialize<ValidationException>(requestBody);
            }

            if (string.IsNullOrEmpty(requestBody))
            {
                _logger.LogError("The requestBody is empty, unable to create exception record");
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
            }

            if (_validationData.Create(exception))
            {
                _logger.LogInformation("The exception record has been created successfully");
                return _createResponse.CreateHttpResponse(HttpStatusCode.Created, req);
            }

            _logger.LogError("The exception record was not inserted into the database: {Exception}", exception);
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "There has been an error while creating an exception record: {Message}", ex.Message);
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }

    }
}

