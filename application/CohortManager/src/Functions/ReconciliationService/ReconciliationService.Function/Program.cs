using Common;
using DataServices.Core;
using DataServices.Database;
using DataServices.Client;
using HealthChecks.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Model;
using NHS.CohortManager.ReconciliationService;
;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .AddConfiguration<ReconciliationServiceConfig>(out var config)
    .AddDataServicesHandler<DataServicesContext>()
    .AddDataServicesHandler()
        .AddDataService<CohortDistribution>(config.CohortDistributionDataServiceUrl)
        .AddDataService<ExceptionManagement>(config.ExceptionManagementDataServiceURL)
    .Build()
    .AddStateStorage()
    .ConfigureServices(services =>
    {
        services.AddDatabaseHealthCheck("ReconciliationService");
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddScoped<IReconciliationProcessor, ParticipantReconciliation>();
    })
    .AddTelemetry()
    .Build();

await host.RunAsync();
