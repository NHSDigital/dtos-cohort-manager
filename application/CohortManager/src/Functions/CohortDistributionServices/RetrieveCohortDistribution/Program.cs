using Common;
using Common.Interfaces;
using Data.Database;
using DataServices.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Model;

var host = new HostBuilder()
    .AddDataServicesHandler()
        .AddDataService<CohortDistribution>(Environment.GetEnvironmentVariable("CohortDistributionDataServiceURL"))
        .Build()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddTransient<ICreateCohortDistributionData, CreateCohortDistributionData>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<IDatabaseHelper, DatabaseHelper>();
        services.AddSingleton<IHttpParserHelper, HttpParserHelper>();
    })
    .AddDatabaseConnection()
    .AddExceptionHandler()
    .Build();

await host.RunAsync();
