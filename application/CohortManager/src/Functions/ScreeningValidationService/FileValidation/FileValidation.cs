namespace NHS.CohortManager.ScreeningValidationService;

using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Model;
using NHS.Screening.FileValidation;
using Microsoft.Extensions.Options;

public class FileValidation
{
    private readonly ILogger<FileValidation> _logger;
    private readonly IBlobStorageHelper _blobStorageHelper;
    private readonly IExceptionHandler _handleException;
    private readonly FileValidationConfig _config;

    public FileValidation(
        ILogger<FileValidation> logger,
        IBlobStorageHelper blobStorageHelper,
        IExceptionHandler handleException,
        IOptions<FileValidationConfig> fileValidationConfig)
    {
        _logger = logger;
        _blobStorageHelper = blobStorageHelper;
        _handleException = handleException;
        _config = fileValidationConfig.Value;
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

            var errorDescription = "The file failed file validation. Check the file Exceptions blob store.";
            var isAdded = await _handleException.CreateRecordValidationExceptionLog(requestBody.NhsNumber, requestBody.FileName, errorDescription, "", requestBody.ErrorRecord);

            if (!isAdded)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            if (requestBody.FileName != null)
            {
                var copied = await _blobStorageHelper.CopyFileAsync(
                    connectionString: _config.caasfolder_STORAGE,
                    fileName: requestBody.FileName,
                    containerName: _config.inboundBlobName);
                if (copied)
                {
                    _logger.LogInformation("File validation exception has completed successfully");
                    return req.CreateResponse(HttpStatusCode.OK);
                }

                _logger.LogError("there has been an error while copying the bad file or saving the exception");
                return req.CreateResponse(HttpStatusCode.InternalServerError);
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }
    }
}
