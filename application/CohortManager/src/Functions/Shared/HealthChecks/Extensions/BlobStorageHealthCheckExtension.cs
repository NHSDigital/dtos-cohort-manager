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
                "Storage HealthCheck For " + name,
                tags: new[] { "Blob", "Azure Storage" });
        // Register BlobServiceClient service for health check
        services.AddSingleton<BlobServiceClient>(provider =>
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            return new BlobServiceClient(connectionString);
        });

        return services;
    }
}
