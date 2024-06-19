namespace Common;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

public class BlobStorageHelper : IBlobStorageHelper
{
    public async Task<bool> CopyFileAsync(string ConnectionString, string FileName, string ContainerName)
    {
        var sourceBlobServiceClient = new BlobServiceClient(ConnectionString);
        var sourceContainerClient = sourceBlobServiceClient.GetBlobContainerClient(ContainerName);
        var sourceBlobClient = sourceContainerClient.GetBlobClient(FileName);

        var destinationBlobServiceClient = new BlobServiceClient(ConnectionString);
        var destinationContainerClient = destinationBlobServiceClient.GetBlobContainerClient(Environment.GetEnvironmentVariable("inboundPoison"));
        var destinationBlobClient = destinationContainerClient.GetBlobClient(FileName);


        await destinationContainerClient.CreateIfNotExistsAsync(PublicAccessType.None);
        await destinationBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);

        var properties = await destinationBlobClient.GetPropertiesAsync();
        while (properties.Value.CopyStatus == CopyStatus.Pending)
        {
            await Task.Delay(1000);
            properties = await destinationBlobClient.GetPropertiesAsync();
        }

        if (properties.Value.CopyStatus != CopyStatus.Success)
        {
            throw new InvalidOperationException($"Failed to copy blob: {properties.Value.CopyStatusDescription}");
        }
        return true;
    }
}
