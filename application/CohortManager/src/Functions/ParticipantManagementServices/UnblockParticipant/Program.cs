using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using Common;
using DataServices.Client;
using NHS.Screening.UnblockParticipant;
using Model;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;

var host = new HostBuilder()
    .AddConfiguration<UnblockParticipantConfig>(out UnblockParticipantConfig config)
    .ConfigureFunctionsWebApplication()
    .AddDataServicesHandler()
        .AddDataService<ParticipantManagement>(config.ParticipantManagementUrl)
        .AddDataService<ParticipantDemographic>(config.ParticipantDemographicDataServiceURL)
        .Build()
    .ConfigureServices(services => {
        // Register health checks
        services.AddBasicHealthCheck("Unblock Participant");
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<IExceptionHandler, ExceptionHandler>();
    })
    .Build();

await host.RunAsync();