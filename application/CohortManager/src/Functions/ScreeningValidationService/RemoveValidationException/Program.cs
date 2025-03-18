using Common;
using Data.Database;
using DataServices.Client;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Model;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .AddDataServicesHandler()
    .AddDataService<ExceptionManagement>(Environment.GetEnvironmentVariable("ExceptionManagementDataServiceURL"))
    .AddDataService<ParticipantDemographic>(Environment.GetEnvironmentVariable("DemographicDataServiceURL"))
    .AddDataService<GPPractice>(Environment.GetEnvironmentVariable("GPPracticeDataServiceURL"))
    .Build()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICallFunction, CallFunction>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddTransient<IValidationExceptionData, ValidationExceptionData>();
        // Register health checks
        services.AddBasicHealthCheck("RemoveValidationException");
    })
    .AddDatabaseConnection()
    .AddExceptionHandler()
    .Build();

await host.RunAsync();
