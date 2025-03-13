using Common;
using Data.Database;
using DataServices.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Model;
using NHS.Screening.RemoveValidationException;

var host = new HostBuilder()
    .AddConfiguration<RemoveValidationExceptionConfig>(out RemoveValidationExceptionConfig config)
    .ConfigureFunctionsWorkerDefaults()
    .AddDataServicesHandler()
    .AddDataService<ExceptionManagement>(config.ExceptionManagementDataServiceURL)
    .AddDataService<ParticipantDemographic>(config.DemographicDataServiceURL)
    .AddDataService<GPPractice>(config.GPPracticeDataServiceURL)
    .Build()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICallFunction, CallFunction>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddTransient<IValidationExceptionData, ValidationExceptionData>();
    })
    .AddDatabaseConnection()
    .AddExceptionHandler()
    .Build();

await host.RunAsync();
