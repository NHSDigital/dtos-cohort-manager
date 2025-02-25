namespace NHS.Screening.ReceiveCaasFile;

using System.Text;
using System.Text.Json;
using Common;
using Microsoft.Extensions.Logging;
using Model;

public class CopyFailedBatchToBlob : ICopyFailedBatchToBlob
{
    private readonly ILogger<CopyFailedBatchToBlob> _logger;
    private readonly IBlobStorageHelper _blobStorageHelper;

    private readonly IExceptionHandler _handleException;

    const string chars = "abcdefghijklmnopqrstuvwxyz";
    public CopyFailedBatchToBlob(ILogger<CopyFailedBatchToBlob> logger, IBlobStorageHelper blobStorageHelper, IExceptionHandler handleException)
    {
        _logger = logger;
        _blobStorageHelper = blobStorageHelper;
        _handleException = handleException;
    }

    public async Task<bool> writeBatchToBlob(string jsonFromBatch, InvalidOperationException invalidOperationException)
    {
        var randN = new Random();
        var randomString = new string(Enumerable.Repeat(chars, 5)
                    .Select(s => s[randN.Next(s.Length)]).ToArray());

        using (var stream = GenerateStreamFromString(jsonFromBatch))
        {
            {
                var blobFile = new BlobFile(stream, $"failedBatch-{randomString}.json");
                var copied = await _blobStorageHelper.UploadFileToBlobStorage(Environment.GetEnvironmentVariable("caasfolder_STORAGE"), "failed-batch", blobFile);

                if (copied)
                {
                    _logger.LogInformation("adding failed batch to blob was unsuccessful");
                    await _handleException.CreateSystemExceptionLog(invalidOperationException, new Participant(), "file name unknown but batch was copied to FailedBatch blob store");
                    return true;
                }
                _logger.LogInformation("adding failed batch to blob was successful");
                return false;
            }
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

