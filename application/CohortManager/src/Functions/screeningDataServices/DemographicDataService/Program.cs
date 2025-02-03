
using Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Data.Database;
using DataServices.Client;
using Model;

var host = new HostBuilder()
    .AddDataServicesHandler()
        .AddDataService<ParticipantDemographic>(Environment.GetEnvironmentVariable("DemographicDataServiceURL"))
        .Build()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddTransient<IDatabaseHelper, DatabaseHelper>();
        services.AddTransient<ICreateDemographicData, CreateDemographicData>();
    })
    .AddExceptionHandler()
    .AddDatabaseConnection()
    .Build();

await host.RunAsync();
