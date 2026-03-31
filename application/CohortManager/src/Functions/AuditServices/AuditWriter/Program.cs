using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using DataServices.Core;
using DataServices.Database;
using HealthChecks.Extensions;
using Common;
using Azure.Storage.Blobs;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .AddDataServicesHandler<DataServicesContext>()
    .ConfigureServices(services =>
    {
        services.AddSingleton(_ =>
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            return new BlobServiceClient(connectionString);
        });

        services.AddDatabaseHealthCheck("AuditWriter");
    })
    .AddTelemetry()
    .Build();

await host.RunAsync();
