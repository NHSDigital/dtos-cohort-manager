namespace HealthChecks.Extensions;

using Azure.Storage.Blobs;
using DataServices.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public static class BlobStorageHealthCheckExtension
{
    public static IServiceCollection AddBlobStorageHealthCheck(this IServiceCollection services)
    {
        // Register blob storage health checks
        services.AddHealthChecks()
            .AddCheck<BlobStorageHealthCheck>(
                "Blob HealthCheck",
                tags: new[] { "Blob", "Azure Storage" });
        // Register BlobServiceClient service for health check
        services.AddSingleton<BlobServiceClient>(provider =>
        {
            var connectionString = Environment.GetEnvironmentVariable("caasfolder_STORAGE");
            return new BlobServiceClient(connectionString);
        });

        return services;
    }
}
