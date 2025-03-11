using Common;
using Common.Interfaces;
using Data.Database;
using DataServices.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Model;
using NHS.CohortManager.CohortDistribution.ValidateCohortDistributionRecord;

var hostBuilder = new HostBuilder();

hostBuilder.AddConfiguration(out ValidateCohortDistributionConfig config);

var host = hostBuilder.AddDataServicesHandler()
        .AddDataService<CohortDistribution>(config.CohortDistributionDataServiceURL)
        .Build()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.TryAddTransient<IDatabaseHelper, DatabaseHelper>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<ICreateParticipant, CreateParticipant>();
    })
    .AddDatabaseConnection()
    .AddExceptionHandler()
    .Build();

await host.RunAsync();
