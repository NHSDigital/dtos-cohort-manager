namespace Common;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;

public class BlobStorageHelper : IBlobStorageHelper
{
    private readonly ILogger<BlobStorageHelper> _logger;
    public BlobStorageHelper(ILogger<BlobStorageHelper> logger)
    {
        _logger = logger;
    }
    public async Task<bool> CopyFileAsync(string connectionString, string fileName, string containerName)
    {
        var sourceBlobServiceClient = new BlobServiceClient(connectionString);
        var sourceContainerClient = sourceBlobServiceClient.GetBlobContainerClient(containerName);
        var sourceBlobClient = sourceContainerClient.GetBlobClient(fileName);

        BlobLeaseClient sourceBlobLease = new(sourceBlobClient);

        var destinationBlobServiceClient = new BlobServiceClient(connectionString);
        var destinationContainerClient = destinationBlobServiceClient.GetBlobContainerClient(Environment.GetEnvironmentVariable("fileExceptions"));
        var destinationBlobClient = destinationContainerClient.GetBlobClient(fileName);

        await destinationContainerClient.CreateIfNotExistsAsync(PublicAccessType.None);


        try
        {
            await sourceBlobLease.AcquireAsync(BlobLeaseClient.InfiniteLeaseDuration);

            var copyOperation = await destinationBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);
            await copyOperation.WaitForCompletionAsync();
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError($"there has been a problem while copying the file: {ex.Message}");
            return false;
        }
        finally
        {
            await sourceBlobLease.ReleaseAsync();
        }

        return true;
    }
}
