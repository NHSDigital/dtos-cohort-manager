using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Common;
using Model;
using DataServices.Client;
using HealthChecks.Extensions;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .AddDataServicesHandler()
        .AddDataService<ParticipantManagement>(Environment.GetEnvironmentVariable("ParticipantManagementUrl"))
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
