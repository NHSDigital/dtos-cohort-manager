using Common;
using Data.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DataServices.Client;
using HealthChecks.Extensions;
using Model;
using NHS.CohortManager.CohortDistributionService;

var hostBuilder = new HostBuilder();

hostBuilder.AddConfiguration<TransformDataServiceConfig>(out TransformDataServiceConfig config);

var host = hostBuilder.ConfigureFunctionsWorkerDefaults()
    .AddDataServicesHandler()
        .AddDataServiceStaticCachedClient<BsSelectOutCode>(config.BsSelectOutCodeUrl)
        .AddDataServiceStaticCachedClient<BsSelectGpPractice>(config.BsSelectGpPracticeUrl)
        .AddDataServiceStaticCachedClient<LanguageCode>(config.LanguageCodeUrl)
        .AddDataServiceStaticCachedClient<ExcludedSMULookup>(config.ExcludedSMULookupUrl)
        .AddDataServiceStaticCachedClient<CurrentPosting>(config.CurrentPostingUrl)
        .Build()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<ITransformDataLookupFacade, TransformDataLookupFacade>();
        services.AddSingleton<ITransformReasonForRemoval, TransformReasonForRemoval>();
        services.AddSingleton<IReasonForRemovalLookup,ReasonForRemovalLookup>();
        services.AddMemoryCache();

        // Register health checks
        services.AddDatabaseHealthCheck("TransformDataService");
    })
    .AddTelemetry()
    .AddDatabaseConnection()
    .AddExceptionHandler()
    .AddHttpClient()
    .Build();

await host.RunAsync();
