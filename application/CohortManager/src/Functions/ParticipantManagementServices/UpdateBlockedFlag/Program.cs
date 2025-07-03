using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using Common;
using DataServices.Client;
using NHS.CohortManager.ParticipantManagementService;
using Model;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;

var host = new HostBuilder()
    .AddConfiguration<UpdateBlockedFlagConfig>(out UpdateBlockedFlagConfig config)
    .ConfigureFunctionsWebApplication()
    .AddDataServicesHandler()
        .AddDataService<ParticipantManagement>(config.ParticipantManagementUrl)
        .AddDataService<ParticipantDemographic>(config.ParticipantDemographicDataServiceURL)
        .Build()
    .ConfigureServices(services =>
    {
        // Register health checks
        services.AddBasicHealthCheck("Update Blocked Flag");
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<IExceptionHandler, ExceptionHandler>();
    })
    .AddTelemetry()
    .AddExceptionHandler()
    .AddHttpClient()
    .Build();

await host.RunAsync();
