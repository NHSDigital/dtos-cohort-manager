using Common;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<IBlobStorageHelper, BlobStorageHelper>();
        // Register health checks
        services.AddBlobStorageHealthCheck("FileValidation");
    })
    .AddExceptionHandler()
    .Build();

await host.RunAsync();
