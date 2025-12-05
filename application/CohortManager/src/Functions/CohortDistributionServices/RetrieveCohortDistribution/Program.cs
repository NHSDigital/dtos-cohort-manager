

using Common;
using Common.Interfaces;
using Data.Database;
using DataServices.Client;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NHS.CohortManager.CohortDistributionDataServices;
using Model;


var host = new HostBuilder()
        .AddConfiguration<RetrieveCohortDistributionConfig>(out RetrieveCohortDistributionConfig config)
        .AddDataServicesHandler()
        .AddDataService<CohortDistribution>(config.CohortDistributionDataServiceURL)
        .AddDataService<BsSelectRequestAudit>(config.BsSelectRequestAuditDataService)
        .Build()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddTransient<IExtractCohortDistributionRecordsStrategy, ExtractCohortDistributionRecords>();
        services.AddTransient<ICreateCohortDistributionData, CreateCohortDistributionData>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<IDatabaseHelper, DatabaseHelper>();
        services.AddSingleton<IHttpParserHelper, HttpParserHelper>();
        // Register health checks
        services.AddDatabaseHealthCheck("RetrieveCohortDistribution");
    })
    .AddTelemetry()
    .AddHttpClient()
    .AddDatabaseConnection()
    .AddExceptionHandler()
    .Build();

await host.RunAsync();
