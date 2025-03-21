using Common;
using Data.Database;
using DataServices.Client;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Model;
using NHS.Screening.RetrievePDSDemographic;

var host = new HostBuilder()
    .AddConfiguration<RetrievePDSDemographicConfig>(out RetrievePDSDemographicConfig config)
    .AddDataServicesHandler()
        .AddDataService<ParticipantDemographic>(config.ParticipantDemographicDataServiceURL)
        .Build()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddScoped<ICallFunction, CallFunction>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
        // Register health checks
        services.AddBasicHealthCheck("RetrievePdsDemographic");
    })
    .Build();

await host.RunAsync();
