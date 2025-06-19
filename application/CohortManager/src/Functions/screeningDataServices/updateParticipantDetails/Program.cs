using Common;
using Common.Interfaces;
using Data.Database;
using DataServices.Client;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Model;
using NHS.Screening.UpdateParticipantDetails;

var host = new HostBuilder()
    .AddConfiguration<UpdateParticipantDetailsConfig>(out UpdateParticipantDetailsConfig config)
    .AddDataServicesHandler()
        .AddDataService<ParticipantManagement>(config.ParticipantManagementUrl)
        .Build()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<IDatabaseHelper, DatabaseHelper>();
        services.AddTransient<ICreateCohortDistributionData, CreateCohortDistributionData>();
        // Register health checks
        services.AddDatabaseHealthCheck("updateParticipantDetails");
    })
    .AddTelemetry()
    .AddDatabaseConnection()
    .AddExceptionHandler()
    .AddHttpClient()
    .Build();

await host.RunAsync();
