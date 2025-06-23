using Common;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NHS.Screening.RemoveParticipant;

var host = new HostBuilder()
.AddConfiguration<RemoveParticipantConfig>(out RemoveParticipantConfig config)
    .ConfigureFunctionsWorkerDefaults()
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
    .AddAzureQueues(false, "")
    .AddExceptionHandler()
    .AddHttpClient()
    .Build();

await host.RunAsync();
