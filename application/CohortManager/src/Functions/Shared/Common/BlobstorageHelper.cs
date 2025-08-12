namespace Common;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;
using Model;

public class BlobStorageHelper : IBlobStorageHelper
{
    private readonly ILogger<BlobStorageHelper> _logger;
    public BlobStorageHelper(ILogger<BlobStorageHelper> logger)
    {
        _logger = logger;
    }
    public async Task CopyFileToPoisonAsync(string connectionString, string fileName, string containerName)
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
            await destinationBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "There has been a problem while copying the file: {Message}", ex.Message);
            throw;
        }
        finally
        {
            await sourceBlobLease.ReleaseAsync();
        }
    }

    public async Task CopyFileToPoisonAsync(string connectionString, string fileName, string containerName, string poisonContainerName)
    {
        var sourceBlobServiceClient = new BlobServiceClient(connectionString);
        var sourceContainerClient = sourceBlobServiceClient.GetBlobContainerClient(containerName);
        var sourceBlobClient = sourceContainerClient.GetBlobClient(fileName);

        BlobLeaseClient sourceBlobLease = new(sourceBlobClient);

        var destinationBlobServiceClient = new BlobServiceClient(connectionString);
        var destinationContainerClient = destinationBlobServiceClient.GetBlobContainerClient(poisonContainerName);
        var destinationBlobClient = destinationContainerClient.GetBlobClient(fileName);

        await destinationContainerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        try
        {
            await sourceBlobLease.AcquireAsync(BlobLeaseClient.InfiniteLeaseDuration);
            await destinationBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "There has been a problem while copying the file: {Message}", ex.Message);
            throw;
        }
        finally
        {
            await sourceBlobLease.ReleaseAsync();
        }
    }

    public async Task<bool> UploadFileToBlobStorage(string connectionString, string containerName, BlobFile blobFile, bool overwrite = false)
    {
        var sourceBlobServiceClient = new BlobServiceClient(connectionString);
        var sourceContainerClient = sourceBlobServiceClient.GetBlobContainerClient(containerName);
        var sourceBlobClient = sourceContainerClient.GetBlobClient(blobFile.FileName);


        await sourceContainerClient.CreateIfNotExistsAsync(PublicAccessType.None);
        try
        {
            await sourceBlobClient.UploadAsync(blobFile.Data, overwrite: overwrite);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "There has been a problem while uploading the file: {Message}", ex.Message);
            return false;
        }

        return true;
    }

    public async Task<BlobFile> GetFileFromBlobStorage(string connectionString, string containerName, string fileName)
    {

        _logger.LogInformation("Downloading File: {FileName} From blobStorage Container: {ContainerName}", fileName, containerName);

        var blobServiceClient = new BlobServiceClient(connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(fileName);

        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        if (await blobClient.ExistsAsync())
        {
            var stream = new MemoryStream();
            await blobClient.DownloadToAsync(stream);
            return new BlobFile(stream, fileName);
        }
        _logger.LogWarning("File {FileName} does not exist in blobStorageContainer: {ContainerName}", fileName, containerName);
        return null;

    }

}
