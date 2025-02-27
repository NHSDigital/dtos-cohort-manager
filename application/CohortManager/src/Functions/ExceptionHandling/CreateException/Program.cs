using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Data.Database;
using Common;
using DataServices.Client;
using Model;

var host = new HostBuilder()
    .AddDataServicesHandler()
    .AddDataService<ExceptionManagement>(Environment.GetEnvironmentVariable("ExceptionManagementDataServiceURL"))
    .AddDataService<ParticipantDemographic>(Environment.GetEnvironmentVariable("DemographicDataServiceURL"))
    .AddDataService<GPPractice>(Environment.GetEnvironmentVariable("GPPracticeDataServiceURL"))
    .Build()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddTransient<IValidationExceptionData, ValidationExceptionData>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
    })
    .AddDatabaseConnection()
    .Build();

await host.RunAsync();
