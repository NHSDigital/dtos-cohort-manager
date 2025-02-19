using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Model;

public class BlobStorageHealthCheck : IHealthCheck
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobStorageHealthCheck> _logger;

    public BlobStorageHealthCheck(BlobServiceClient blobServiceClient, ILogger<BlobStorageHealthCheck> logger)
    {
       _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if the blob service is accessible
            //var blobServiceClient = new BlobServiceClient(Environment.GetEnvironmentVariable("caasfolder_STORAGE"));
           // var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
           // var blobClient = containerClient.GetBlobClient(fileName);
            await _blobServiceClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy("Blob storage is accessible.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blob storage health check failed.");
            return HealthCheckResult.Unhealthy("Blob storage is inaccessible.", ex);
        }
    }
}
