using Common;
using Common.Interfaces;
using Data.Database;
using DataServices.Client;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Model;
using NHS.Screening.GetValidationExceptions;

var host = new HostBuilder()
    .AddConfiguration<GetValidationExceptionsConfig>(out GetValidationExceptionsConfig config)
    .ConfigureFunctionsWorkerDefaults()
    .AddDataServicesHandler()
    .AddDataService<ExceptionManagement>(config.ExceptionManagementDataServiceURL)
    .AddDataService<ParticipantDemographic>(config.DemographicDataServiceURL)

    .Build()
    .ConfigureServices(services =>
    {
        services.AddTransient<IValidationExceptionData, ValidationExceptionData>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<IHttpParserHelper, HttpParserHelper>();
        services.AddScoped(typeof(IPaginationService<>), typeof(PaginationService<>));
        // Register health checks
        services.AddDatabaseHealthCheck("GetValidationExceptions");
    })
    .AddTelemetry()
    .AddDatabaseConnection()
    .AddHttpClient()
    .AddExceptionHandler()
    .Build();

await host.RunAsync();
