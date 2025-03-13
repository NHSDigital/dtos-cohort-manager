using Common;
using Data.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using DataServices.Client;
using Model;
using NHS.Screening.CreateCohortDistribution;

var host = new HostBuilder()
    .AddConfiguration<CreateCohortDistributionConfig>(out CreateCohortDistributionConfig config)
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICallFunction, CallFunction>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<ICohortDistributionHelper, CohortDistributionHelper>();
        services.TryAddTransient<IDatabaseHelper, DatabaseHelper>();
    })
    .AddAzureQueues()
    .AddDatabaseConnection()
    .AddExceptionHandler()
    .Build();

await host.RunAsync();
