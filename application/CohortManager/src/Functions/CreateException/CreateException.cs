namespace NHS.CohortManager.ExceptionService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text;
using System.Text.Json;
using Model;
using System.Runtime.CompilerServices;
using Common;
using Data.Database;

public class CreateException
{
    private readonly ILogger<CreateException> _logger;
    private readonly IValidationExceptionData _validationData;

    private readonly ICreateResponse _createResponse;

    private readonly ICallFunction _callFunction;
    public CreateException(ILogger<CreateException> logger, ICallFunction callFunction, IValidationExceptionData validationExceptionData, ICreateResponse createResponse)
    {
        _logger = logger;
        _callFunction = callFunction;
        _validationData = validationExceptionData;
        _createResponse = createResponse;
    }

    [Function("CreateException")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {

        ValidationException exception;
        var requestBody = "";

        using (var reader = new StreamReader(req.Body, Encoding.UTF8))
        {
            requestBody = await reader.ReadToEndAsync();
            exception = JsonSerializer.Deserialize<ValidationException>(requestBody);
        }
        try
        {
            if (!string.IsNullOrEmpty(requestBody))
            {
                _logger.LogError("CreateException received an empty payload");

                if (_validationData.Create(exception))
                {
                    return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
                }
            }
            return req.CreateResponse(HttpStatusCode.BadRequest);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "there has been an error while creating an exception record: {Message}", ex.Message);
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

    }
}

