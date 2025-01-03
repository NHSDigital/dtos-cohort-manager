using Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Data.Database;
using NHS.CohortManager.DemographicServices;
using Model;
using DataServices.Client;

var host = new HostBuilder()
    .AddDataServicesHandler()
        .AddDataService<ParticipantDemographic>(Environment.GetEnvironmentVariable("DemographicDataServiceURL"))
        .Build()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddTransient<IDatabaseHelper, DatabaseHelper>();
        services.AddTransient<ICreateDemographicData, CreateDemographicData>();
    })
    .AddExceptionHandler()
    .AddDatabaseConnection()
    .Build();

host.Run();
