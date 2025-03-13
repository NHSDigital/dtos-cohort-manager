using Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NHS.Screening.FileValidation;

var host = new HostBuilder()
    .AddConfiguration<FileValidationConfig>(out FileValidationConfig config)
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<IBlobStorageHelper, BlobStorageHelper>();
    })
    .AddExceptionHandler()
    .Build();

await host.RunAsync();
