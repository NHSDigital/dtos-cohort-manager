namespace NHS.CohortManager.ScreeningValidationService;

using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Model;

public class FileValidation
{
    private readonly ILogger<FileValidation> _logger;
    private readonly ICallFunction _callFunction;

    public FileValidation(ILogger<FileValidation> logger, ICallFunction callFunction)
    {
        _logger = logger;
        _callFunction = callFunction;
    }

    [Function("FileValidation")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        ValidationException requestBody;

        try
        {
            string requestBodyJson;
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBodyJson = await reader.ReadToEndAsync();
            }


            requestBody = JsonSerializer.Deserialize<ValidationException>(requestBodyJson);

            var createResponse = await _callFunction.SendPost(Environment.GetEnvironmentVariable("CreateValidationExceptionURL"), requestBodyJson);
            if (createResponse.StatusCode != HttpStatusCode.OK)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }
            _logger.LogInformation("File validation exception: {ExceptionMessage} from {FileName}", requestBody.RuleName, requestBody.NhsNumber);
            return req.CreateResponse(HttpStatusCode.OK);
        }
        catch
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }
    }
}
