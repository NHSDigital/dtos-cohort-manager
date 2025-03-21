using Common;
using Data.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using DataServices.Client;
using HealthChecks.Extensions;
using Model;
using NHS.Screening.MarkParticipantAsIneligible;

var host = new HostBuilder()
    .AddConfiguration<MarkParticipantAsIneligibleConfig>(out MarkParticipantAsIneligibleConfig config)
    .ConfigureFunctionsWorkerDefaults()
    .AddDataServicesHandler()
    .AddDataService<ParticipantManagement>(config.ParticipantManagementUrl)
    .Build()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.TryAddTransient<IDatabaseHelper, DatabaseHelper>();
        services.AddSingleton<ICallFunction, CallFunction>();
        // Register health checks
        services.AddDatabaseHealthCheck("markParticipantAsIneligible");
    })
    .AddDatabaseConnection()
    .AddExceptionHandler()
    .Build();

await host.RunAsync();
