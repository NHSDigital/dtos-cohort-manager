namespace NHS.CohortManager.EndToEndTests.Helpers;

using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using FluentAssertions;
using Microsoft.Extensions.Logging;

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

    public async Task<bool> DoesBlobExistAsync(string fileName, string containerName)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);


            var response = await blobClient.ExistsAsync();
            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking if blob '{fileName}' exists in container '{containerName}'");
            return false;
        }
    }

    public async Task AssertLocalFileMatchesBlobAsync(string localFilePath, string containerName)
    {
        try
        {
            if (!File.Exists(localFilePath))
            {
                throw new FileNotFoundException($"Local file not found at {localFilePath}");
            }

            var fileName = Path.GetFileName(localFilePath);


            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);


            var response = await blobClient.DownloadAsync();
            string blobContent;
            using (var streamReader = new StreamReader(response.Value.Content))
            {
                blobContent = await streamReader.ReadToEndAsync();
            }


            string localContent = await File.ReadAllTextAsync(localFilePath);


            blobContent = NormalizeLineEndings(blobContent);
            localContent = NormalizeLineEndings(localContent);

            blobContent.Should().Be(localContent,
                $"The content of blob '{fileName}' should match the content of local file '{localFilePath}'");

            _logger.LogInformation($"Successfully verified content match for blob '{fileName}'");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error comparing local file '{localFilePath}' with blob in container '{containerName}'");
            throw;
        }
    }

    private static string NormalizeLineEndings(string text)
    {
        return text.Replace("\r\n", "\n").Replace("\r", "\n");
    }
}
