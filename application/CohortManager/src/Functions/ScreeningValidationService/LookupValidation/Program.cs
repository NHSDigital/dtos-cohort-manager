using Common;
using Common.Interfaces;
using Data.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Model;
using DataServices.Client;
using HealthChecks.Extensions;
using NHS.CohortManager.ScreeningValidationService;

var hostBuilder = new HostBuilder();

hostBuilder.AddConfiguration<LookupValidationConfig>(out LookupValidationConfig config);

var host = hostBuilder.ConfigureFunctionsWorkerDefaults()
    .AddDataServicesHandler()
        .AddDataServiceStaticCachedClient<BsSelectGpPractice>(config.BsSelectGpPracticeUrl)
        .AddDataServiceStaticCachedClient<BsSelectOutCode>(config.BsSelectOutCodeUrl)
        .AddDataServiceStaticCachedClient<CurrentPosting>(config.CurrentPostingUrl)
        .AddDataServiceStaticCachedClient<ExcludedSMULookup>(config.ExcludedSMULookupUrl)
        .Build()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<IReadRules, ReadRules>();
        services.AddSingleton<IDataLookupFacadeBreastScreening, DataLookupFacadeBreastScreening>();
        // Register health checks
        services.AddBasicHealthCheck("LookupValidation");
    })
    .AddDatabaseConnection()
    .AddExceptionHandler()
    .AddHttpClient()
    .Build();

await host.RunAsync();
