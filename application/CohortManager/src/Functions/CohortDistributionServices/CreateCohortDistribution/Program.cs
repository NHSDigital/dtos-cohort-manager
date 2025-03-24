using Common;
using Data.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using DataServices.Client;
using Model;
using NHS.Screening.CreateCohortDistribution;
using HealthChecks.Extensions;

var host = new HostBuilder()
    .AddConfiguration<CreateCohortDistributionConfig>(out CreateCohortDistributionConfig config)
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICallFunction, CallFunction>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<ICohortDistributionHelper, CohortDistributionHelper>();
        services.TryAddTransient<IDatabaseHelper, DatabaseHelper>();
        // Register health checks
        services.AddDatabaseHealthCheck("CreateCohortDistribution");
        services.AddBlobStorageHealthCheck("CreateCohortDistribution");
    })
    .AddAzureQueues()
    .AddDatabaseConnection()
    .AddExceptionHandler()
    .Build();

await host.RunAsync();
