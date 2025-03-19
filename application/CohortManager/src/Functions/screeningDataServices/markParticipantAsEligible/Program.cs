using Common;
using Data.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DataServices.Client;
using HealthChecks.Extensions;
using Model;


var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .AddDataServicesHandler()
    .AddDataService<ParticipantManagement>(Environment.GetEnvironmentVariable("ParticipantManagementUrl"))
    .Build()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddTransient<IDatabaseHelper, DatabaseHelper>();
        services.AddSingleton<ICallFunction, CallFunction>();
        // Register health checks
        services.AddDatabaseHealthCheck("markParticipantAsEligible");
    })
    .AddDatabaseConnection()
    .AddExceptionHandler()
    .Build();

await host.RunAsync();
