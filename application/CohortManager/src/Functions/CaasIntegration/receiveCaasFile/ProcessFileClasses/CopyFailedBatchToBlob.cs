namespace NHS.Screening.ReceiveCaasFile;

using System.Text;
using System.Text.Json;
using Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;

public class CopyFailedBatchToBlob : ICopyFailedBatchToBlob
{
    private readonly ILogger<CopyFailedBatchToBlob> _logger;
    private readonly IBlobStorageHelper _blobStorageHelper;
    private readonly IExceptionHandler _handleException;

    private readonly ReceiveCaasFileConfig _config;

    public CopyFailedBatchToBlob(ILogger<CopyFailedBatchToBlob> logger, IBlobStorageHelper blobStorageHelper, IExceptionHandler handleException, IOptions<ReceiveCaasFileConfig> config)
    {
        _config = config.Value;
        _logger = logger;
        _blobStorageHelper = blobStorageHelper;
        _handleException = handleException;
    }

    public async Task<bool> writeBatchToBlob(string jsonFromBatch, InvalidOperationException invalidOperationException)
    {
        using (var stream = GenerateStreamFromString(jsonFromBatch))
        {
            var blobFile = new BlobFile(stream, $"failedBatch-{Guid.NewGuid()}.json");
            var copied = await _blobStorageHelper.UploadFileToBlobStorage(_config.caasfolder_STORAGE, "failed-batch", blobFile);

            if (copied)
            {
                _logger.LogInformation("adding failed batch to blob was successful");
                await _handleException.CreateSystemExceptionLog(invalidOperationException, new Participant(), "file name unknown but batch was copied to FailedBatch blob store");
                return true;
            }
            _logger.LogInformation("adding failed batch to blob was unsuccessful");
            return false;
        }
    }

    private static Stream GenerateStreamFromString(string s)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(s);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }
}

