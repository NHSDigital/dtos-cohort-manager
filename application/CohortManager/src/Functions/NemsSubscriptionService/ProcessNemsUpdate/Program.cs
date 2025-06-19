using Common;
using HealthChecks.Extensions;
using Microsoft.Extensions.Hosting;
using NHS.Screening.ProcessNemsUpdate;

var host = new HostBuilder()
    .AddConfiguration<ProcessNemsUpdateConfig>()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddBlobStorageHealthCheck("ProcessNemsUpdate");
    })
    .AddExceptionHandler()
    .Build();

await host.RunAsync();
