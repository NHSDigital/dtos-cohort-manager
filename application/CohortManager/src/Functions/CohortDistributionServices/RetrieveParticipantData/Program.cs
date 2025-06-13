using Common;
using Data.Database;
using DataServices.Client;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Model;
using NHS.Screening.RetrieveParticipantData;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .AddConfiguration<RetrieveParticipantDataConfig>(out RetrieveParticipantDataConfig config)
    .AddDataServicesHandler()
        .AddDataService<ParticipantManagement>(config.ParticipantManagementUrl)
        .Build()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<IDatabaseHelper, DatabaseHelper>();
        services.AddSingleton<ICallFunction, CallFunction>();
        services.AddSingleton<ICreateParticipant, CreateParticipant>();
        // Register health checks
        services.AddDatabaseHealthCheck("RetrieveParticipantData");
    })
    .AddDatabaseConnection()
    .AddExceptionHandler()
    .AddHttpClient()
    .Build();

await host.RunAsync();
