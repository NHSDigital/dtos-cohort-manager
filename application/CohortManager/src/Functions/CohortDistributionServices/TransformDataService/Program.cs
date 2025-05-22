using Common;
using Data.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DataServices.Client;
using HealthChecks.Extensions;
using Model;
using NHS.CohortManager.CohortDistribution;

var hostBuilder = new HostBuilder();

hostBuilder.AddConfiguration<TransformDataServiceConfig>(out TransformDataServiceConfig config);

var host = hostBuilder.ConfigureFunctionsWorkerDefaults()
    .AddDataServicesHandler()
        .AddDataServiceStaticCachedClient<BsSelectOutCode>(config.BsSelectOutCodeUrl)
        .AddDataServiceStaticCachedClient<BsSelectGpPractice>(config.BsSelectGpPracticeUrl)
        .AddDataService<CohortDistribution>(config.CohortDistributionDataServiceUrl)
        .Build()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<ITransformDataLookupFacade, TransformDataLookupFacade>();
        services.AddSingleton<ITransformReasonForRemoval, TransformReasonForRemoval>();
        // Register health checks
        services.AddDatabaseHealthCheck("TransformDataService");
    })
    .AddDatabaseConnection()
    .AddExceptionHandler()
    .AddHttpClient()
    .Build();

await host.RunAsync();
