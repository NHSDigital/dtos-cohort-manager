using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Data.Database;
using Common;
using DataServices.Client;
using HealthChecks.Extensions;
using Model;
using NHS.Screening.UpdateException;

var host = new HostBuilder()
    .AddConfiguration<UpdateExceptionConfig>(out UpdateExceptionConfig config)
        .AddDataServicesHandler()
        .AddDataService<ExceptionManagement>(config.ExceptionManagementDataServiceURL)
        .Build()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        // Register health checks
        services.AddBasicHealthCheck("UpdateException");
    })
    .AddTelemetry()
    .AddDatabaseConnection()
    .Build();

await host.RunAsync();
