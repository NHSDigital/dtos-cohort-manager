using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Common;
using Model;
using DataServices.Client;
using NHS.Screening.CheckParticipantExists;

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
    })
    .Build();

await host.RunAsync();
