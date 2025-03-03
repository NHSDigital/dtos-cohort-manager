namespace HealthChecks.Extensions;

using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;

public static class BlobStorageHealthCheckExtension
{
    public static IServiceCollection AddBlobStorageHealthCheck(this IServiceCollection services, string name)
    {
        // Register blob storage health checks
        services.AddHealthChecks()
            .AddCheck<BlobStorageHealthCheck>(
                "HealthCheck for " + name,
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
