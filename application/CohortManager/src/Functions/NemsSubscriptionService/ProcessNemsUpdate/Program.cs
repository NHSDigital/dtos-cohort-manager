using Common;
using Common.Interfaces;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NHS.Screening.ProcessNemsUpdate;

var host = new HostBuilder()
    .AddConfiguration<ProcessNemsUpdateConfig>()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<IFhirPatientDemographicMapper, FhirPatientDemographicMapper>();
        services.AddBlobStorageHealthCheck("ProcessNemsUpdate");
    })
    .AddExceptionHandler()
    .AddHttpClient()
    .Build();

await host.RunAsync();
