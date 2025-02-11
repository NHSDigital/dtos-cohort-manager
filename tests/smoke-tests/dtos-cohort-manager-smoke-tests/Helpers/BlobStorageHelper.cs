using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace dtos_cohort_manager_specflow.Helpers;

public class BlobStorageHelper
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobStorageHelper> _logger;


    public BlobStorageHelper(BlobServiceClient blobServiceClient, ILogger<BlobStorageHelper> logger)
    {
    _blobServiceClient = blobServiceClient;
    _logger = logger;
    }

    public async Task UploadFileToBlobStorageAsync(string filePath, string blobContainerName)
    {
    if (!File.Exists(filePath))
    {
    _logger.LogError($"File not found at {filePath}");
    return;
    }

    _logger.LogInformation("Uploading file {FilePath} to blob storage", filePath);

    var blobContainerClient = _blobServiceClient.GetBlobContainerClient(blobContainerName);
    await blobContainerClient.CreateIfNotExistsAsync();

    var blobClient = blobContainerClient.GetBlobClient(Path.GetFileName(filePath));
    await blobClient.UploadAsync(File.OpenRead(filePath), true);

    _logger.LogInformation("File uploaded successfully");
    }
}
