namespace HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Common;
using Model;

public class BlobStorageHealthCheck : IHealthCheck
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobStorageHealthCheck> _logger;

    public BlobStorageHealthCheck(ILogger<BlobStorageHealthCheck> logger, BlobServiceClient blobServiceClient)
    {
        _logger = logger;
        _blobServiceClient = blobServiceClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Running health check for Azure Blob Storage...");

        try
        {
            // Check if the blob service is accessible
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
