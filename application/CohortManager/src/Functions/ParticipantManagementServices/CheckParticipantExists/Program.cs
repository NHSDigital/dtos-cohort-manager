using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Common;
using Model;
using DataServices.Client;
using NHS.Screening.CheckParticipantExists;
using HealthChecks.Extensions;

var host = new HostBuilder()
    .AddConfiguration<CheckParticipantExistsConfig>(out CheckParticipantExistsConfig config)
    .ConfigureFunctionsWebApplication()
    .AddDataServicesHandler()
        .AddDataService<ParticipantManagement>(config.ParticipantManagementUrl)
        .Build()
    .ConfigureServices(services => {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddSingleton<ICreateResponse, CreateResponse>();
        // Register health checks
        services.AddBasicHealthCheck("CheckParticipantExists");
    })
    .Build();

await host.RunAsync();
