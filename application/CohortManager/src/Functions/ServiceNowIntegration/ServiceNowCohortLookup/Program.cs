using Common;
using Model;
using DataServices.Client;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NHS.CohortManager.ServiceNowIntegrationService;

var host = new HostBuilder()
    .AddConfiguration<ServiceNowCohortLookupConfig>(out ServiceNowCohortLookupConfig config)
    .AddDataServicesHandler()
        .AddDataService<ServicenowCase>(config.ServiceNowCasesDataServiceURL)
        .AddDataService<CohortDistribution>(config.CohortDistributionDataServiceURL)
        .Build()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<IBlobStorageHelper, BlobStorageHelper>();
        // Register health checks
        services.AddBlobStorageHealthCheck("ServiceNowCohortLookup");
    })
    .AddTelemetry()
    .AddExceptionHandler()
    .AddHttpClient()
    .Build();

await host.RunAsync();
