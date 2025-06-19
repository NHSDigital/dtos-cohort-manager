using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Data.Database;
using Common;
using DataServices.Client;
using HealthChecks.Extensions;
using Model;
using NHS.Screening.CreateException;

var host = new HostBuilder()
    .AddConfiguration<CreateExceptionConfig>(out CreateExceptionConfig config)
    .AddDataServicesHandler()
    .AddDataService<ExceptionManagement>(config.ExceptionManagementDataServiceURL)
    .AddDataService<ParticipantDemographic>(config.DemographicDataServiceURL)
    .Build()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddTransient<IValidationExceptionData, ValidationExceptionData>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
        // Register health checks
        services.AddBasicHealthCheck("CreateException");
    })
    .AddDatabaseConnection()
    .AddHttpClient()
    .Build();

await host.RunAsync();
