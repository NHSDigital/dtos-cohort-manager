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
    .AddConfiguration<AuthConfig>()
    .ConfigureFunctionsWorkerDefaults(
            workerOptions =>
            {
                workerOptions.UseMiddleware<CIS2AuthMiddleware>();
            }
    )
    .AddDataServicesHandler()
    .AddDataService<ExceptionManagement>(config.ExceptionManagementDataServiceURL)
    .AddDataService<ParticipantDemographic>(config.DemographicDataServiceURL)
    .Build()
    //.AddAuthentication()
    .ConfigureServices(services =>
    {
        services.AddSingleton<IAuthenticationService, JWTAuthentication>();
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
