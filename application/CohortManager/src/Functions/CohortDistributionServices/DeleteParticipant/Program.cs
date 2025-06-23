using Common;
using Data.Database;
using DataServices.Client;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Model;
using NHS.Screening.DeleteParticipant;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .AddConfiguration<DeleteParticipantConfig>(out DeleteParticipantConfig config)
    .AddDataServicesHandler()
        .AddDataService<CohortDistribution>(config.CohortDistributionDataServiceUrl)
        .Build()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<IDatabaseHelper, DatabaseHelper>();
        services.AddSingleton<ICreateParticipant, CreateParticipant>();
        // Register health checks
        services.AddBasicHealthCheck("DeleteParticipant");
    })
    .AddTelemetry()
    .AddExceptionHandler()
    .AddHttpClient()
    .Build();

await host.RunAsync();
