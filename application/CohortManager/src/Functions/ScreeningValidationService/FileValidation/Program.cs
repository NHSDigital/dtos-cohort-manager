using Common;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NHS.Screening.FileValidation;

var host = new HostBuilder()
    .AddConfiguration<FileValidationConfig>(out FileValidationConfig config)
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
