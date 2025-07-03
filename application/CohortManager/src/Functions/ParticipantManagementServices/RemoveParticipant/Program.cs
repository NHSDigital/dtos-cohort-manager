using Common;
using DataServices.Client;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Model;
using NHS.Screening.RemoveParticipant;

var host = new HostBuilder()
.AddConfiguration<RemoveParticipantConfig>(out RemoveParticipantConfig config)
    .ConfigureFunctionsWorkerDefaults()
    .AddDataServicesHandler()
        .AddDataService<ParticipantManagement>(config.ParticipantManagementUrl)
        .Build()
    .AddTelemetry()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<ICheckDemographic, CheckDemographic>();
        services.AddSingleton<ICreateParticipant, CreateParticipant>();
        services.AddSingleton<ICohortDistributionHandler, CohortDistributionHandler>();
        // Register health checks
        services.AddBasicHealthCheck("RemoveParticipant");
    })
    .AddAzureQueues()
    .AddExceptionHandler()
    .AddHttpClient()
    .Build();

await host.RunAsync();
