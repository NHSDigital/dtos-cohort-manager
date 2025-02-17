

using Common;
using Data.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DataServices.Client;
using Model;
using NHS.CohortManager.CohortDistribution;

var hostBuilder = new HostBuilder();

hostBuilder.AddConfiguration<TransformDataServiceConfig>(out TransformDataServiceConfig config);

var host = hostBuilder.ConfigureFunctionsWorkerDefaults()
    .AddDataServicesHandler()
        .AddCachedDataService<BsSelectOutCode>(config.BsSelectOutCodeUrl)
        .AddCachedDataService<BsSelectGpPractice>(config.BsSelectGpPracticeUrl)
        .AddDataService<CohortDistribution>(config.CohortDistributionDataServiceUrl)
        .Build()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<ITransformDataLookupFacade, TransformDataLookupFacade>();
        services.AddSingleton<ITransformReasonForRemoval, TransformReasonForRemoval>();
    })
    .AddDatabaseConnection()
    .AddExceptionHandler()
    .Build();

await host.RunAsync();
