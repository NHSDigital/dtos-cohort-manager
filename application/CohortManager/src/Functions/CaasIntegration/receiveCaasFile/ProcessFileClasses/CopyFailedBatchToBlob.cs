namespace NHS.Screening.ReceiveCaasFile;

using System;
using System.Collections.Generic;
using System.Text.Json;
using Azure.Storage.Blobs;
using Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using Parquet.Serialization;

public class CopyFailedBatchToBlob : ICopyFailedBatchToBlob
{
    private readonly ILogger<CopyFailedBatchToBlob> _logger;
    private readonly IBlobStorageHelper _blobStorageHelper;
    private readonly IExceptionHandler _handleException;

    private readonly ReceiveCaasFileConfig _config;

    private readonly IFailedBatchDict _failedBatchDict;

    public CopyFailedBatchToBlob(ILogger<CopyFailedBatchToBlob> logger, IBlobStorageHelper blobStorageHelper, IExceptionHandler handleException, IOptions<ReceiveCaasFileConfig> config, IFailedBatchDict failedBatchDict)
    {
        _config = config.Value;
        _logger = logger;
        _blobStorageHelper = blobStorageHelper;
        _handleException = handleException;
        _failedBatchDict = failedBatchDict;
    }

    public async Task<bool> writeBatchToBlob(string jsonFromBatch, InvalidOperationException invalidOperationException, List<ParticipantsParquetMap> parquetValuesForRetry, string fileName = "")
    {
        using (var stream = GenerateStreamFromString(jsonFromBatch))
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                if (_failedBatchDict.ShouldRetryFile(fileName))
                {
                    var fileRetryCount = _failedBatchDict.GetRetryCount(fileName);
                    upsertRetryValue(fileName, fileRetryCount);
                    var pathOfFileToRetry = await convertBatchToParquet(parquetValuesForRetry, fileName);
                    if (!string.IsNullOrEmpty(pathOfFileToRetry))
                    {
                        await RetryFailedBatch(pathOfFileToRetry, fileName);
                    }
                }
                else
                {
                    var filePath = FileDirectoryPath(fileName);
                    if (File.Exists(fileName))
                    {
                        File.Delete(fileName);
                    }
                }
            }

            fileName = $"failedBatch-{Guid.NewGuid()}.json";
            await AddItemToBlob(stream, fileName);

            await _handleException.CreateSystemExceptionLog(invalidOperationException, new Participant(), "file name unknown but batch was copied to FailedBatch blob store");
            _logger.LogInformation("adding failed batch to blob was unsuccessful");
            return true;
        }
    }

    private void upsertRetryValue(string filename, int fileRetryCount)
    {
        fileRetryCount = fileRetryCount + 1;
        if (!_failedBatchDict.HasFileFailedBefore(filename))
        {

            _failedBatchDict.AddFailedBatchDataToDict(filename, fileRetryCount);
        }
        else
        {
            _failedBatchDict.UpdateFileFailureCount(filename, fileRetryCount);
        }
    }

    private async Task<string> convertBatchToParquet(List<ParticipantsParquetMap> parquetValuesForRetry, string fileName)
    {
        try
        {
            var parquetData = parquetValuesForRetry
                   .Select(ParticipantsParquetMap.ToParticipantParquet)
                   .ToList();

            var filePath = FileDirectoryPath(fileName);
            await ParquetSerializer.SerializeAsync(parquetData, filePath);

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "There was a problem when converting a failed batch to parquet for retry {error}", ex.Message);
            return "";
        }

    }

    private async Task<bool> RetryFailedBatch(string localFilePath, string fileName)
    {

        var copied = false;
        using (FileStream fileStream = File.OpenRead(localFilePath))
        {
            var blobFile = new BlobFile(fileStream, fileName);
            copied = await _blobStorageHelper.UploadFileToBlobStorage(_config.caasfolder_STORAGE, "inbound", blobFile, true);
        }

        if (copied)
        {
            _logger.LogInformation("Adding failed batch to blob was successful");
            return true;
        }
        _logger.LogError("Adding failed batch to blob was unsuccessful");
        return false;
    }


    private async Task<bool> AddItemToBlob(Stream stream, string fileName)
    {
        var blobFile = new BlobFile(stream, fileName);
        var copied = await _blobStorageHelper.UploadFileToBlobStorage(_config.caasfolder_STORAGE, "failed-batch", blobFile);

        if (copied)
        {
            _logger.LogInformation("Adding failed batch to blob was successful");
            return true;
        }
        return false;
    }

    private string FileDirectoryPath(string fileName)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var filePath = Path.Combine(currentDirectory, fileName);

        filePath = filePath.Replace("bin/output/", "");

        return filePath;
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

