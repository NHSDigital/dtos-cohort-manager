using Common;
using Data.Database;
using DataServices.Client;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Model;
using NHS.Screening.ValidateCohortDistributionRecord;

var host = new HostBuilder()
    .AddConfiguration<ValidateCohortDistributionRecordConfig>(out ValidateCohortDistributionRecordConfig config)
    .AddDataServicesHandler()
    .AddDataService<CohortDistribution>(config.CohortDistributionDataServiceURL)
    .Build()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.TryAddTransient<IDatabaseHelper, DatabaseHelper>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<ICreateParticipant, CreateParticipant>();
        services.AddSingleton<ICallFunction, CallFunction>();
        // Register health checks
        services.AddDatabaseHealthCheck("ValidateCohortDistributionRecord");
    })
    .AddDatabaseConnection()
    .AddExceptionHandler()
    .AddHttpClient()
    .Build();

await host.RunAsync();
