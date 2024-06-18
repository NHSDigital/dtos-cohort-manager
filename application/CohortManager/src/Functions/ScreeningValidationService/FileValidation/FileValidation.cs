namespace NHS.CohortManager.ScreeningValidationService;

using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text;
using System.Text.Json;

public class FileValidation
{
    private readonly ILogger<FileValidation> _logger;

    public FileValidation(ILogger<FileValidation> logger)
    {
        _logger = logger;
    }

    [Function("FileValidation")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        FileValidationRequestBody requestBody;

        try
        {
            string requestBodyJson;
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBodyJson = await reader.ReadToEndAsync();
            }

            requestBody = JsonSerializer.Deserialize<FileValidationRequestBody>(requestBodyJson);
        }
        catch
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        _logger.LogInformation("File validation exception: {ExceptionMessage} from {FileName}", requestBody.ExceptionMessage, requestBody.FileName);
        return req.CreateResponse(HttpStatusCode.OK);
    }
}
