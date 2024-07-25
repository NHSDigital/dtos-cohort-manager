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
    private readonly IBlobStorageHelper _blobStorageHelper;

    public FileValidation(ILogger<FileValidation> logger, ICallFunction callFunction, IBlobStorageHelper blobStorageHelper)
    {
        _logger = logger;
        _callFunction = callFunction;
        _blobStorageHelper = blobStorageHelper;
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

            var requestObject = new ValidationException()
            {
                RuleId = requestBody.RuleId == null ? 0 : requestBody.RuleId,
                Cohort = "",
                NhsNumber = string.IsNullOrEmpty(requestBody.NhsNumber) ? "" : requestBody.NhsNumber,
                DateCreated = requestBody.DateCreated ?? DateTime.Now,
                FileName = string.IsNullOrEmpty(requestBody.FileName) ? "" : requestBody.FileName,
                DateResolved = requestBody.DateResolved ?? DateTime.MaxValue,
                RuleContent = requestBody.RuleContent ?? "",
                RuleDescription = requestBody.RuleDescription ?? "",
                Category = requestBody.Category ?? 0,
                ScreeningService = requestBody.ScreeningService ?? 1,
                Fatal = requestBody.Fatal ?? 0,
            };

            var createResponse = await _callFunction.SendPost(Environment.GetEnvironmentVariable("CreateExceptionURL"), JsonSerializer.Serialize<ValidationException>(requestObject));
            if (createResponse.StatusCode != HttpStatusCode.OK)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            if (requestObject.FileName != null)
            {
                var copied = await _blobStorageHelper.CopyFileAsync(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), requestObject.FileName, Environment.GetEnvironmentVariable("inboundBlobName"));
                if (copied)
                {
                    _logger.LogInformation("File validation exception: {RuleId} from {NhsNumber}", requestObject.RuleId, requestObject.NhsNumber);
                    return req.CreateResponse(HttpStatusCode.OK);
                }
                _logger.LogError("there has been an error while copying the bad file or saving the exception");
                return req.CreateResponse(HttpStatusCode.InternalServerError);
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }
    }
}
