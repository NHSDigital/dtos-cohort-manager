using Common;
using DataServices.Client;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Model;
using NHS.Screening.DemographicDataManagementFunction;

var host = new HostBuilder()
    .AddConfiguration<DemographicDataManagementFunctionConfig>(out DemographicDataManagementFunctionConfig config)
    .AddDataServicesHandler()
        .AddDataService<ParticipantDemographic>(config.ParticipantDemographicDataServiceURL)
        .Build()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        // Register health checks
        services.AddBasicHealthCheck("DemographicDataFunction");
    })
    .AddTelemetry()
    .AddHttpClient()
    .Build();

await host.RunAsync();
