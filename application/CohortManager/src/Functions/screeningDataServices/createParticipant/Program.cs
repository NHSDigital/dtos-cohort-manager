using Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Data.Database;
using DataServices.Client;
using HealthChecks.Extensions;
using Model;
using NHS.Screening.CreateParticipant;

var host = new HostBuilder()
    .AddConfiguration<CreateParticipantConfig>(out CreateParticipantConfig config)
    .ConfigureFunctionsWorkerDefaults()
    .AddDataServicesHandler()
        .AddDataService<ParticipantManagement>(config.ParticipantManagementUrl)
        .Build()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddTransient<IDatabaseHelper, DatabaseHelper>();
        services.AddSingleton<ICallFunction, CallFunction>();
        // Register health checks
        services.AddDatabaseHealthCheck("createParticipantDataService");
    })
    .AddDatabaseConnection()
    .AddExceptionHandler()
    .AddHttpClient()
    .Build();

await host.RunAsync();
