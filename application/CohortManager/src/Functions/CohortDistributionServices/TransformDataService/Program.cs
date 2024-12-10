

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
        .Build()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddScoped<IBsTransformationLookups, BsTransformationLookups>();
        services.AddSingleton<ITransformDataLookupFacade, TransformDataLookupFacade>();
        services.AddSingleton<ITransformReasonForRemoval, TransformReasonForRemoval>();
    })
    .AddDatabaseConnection()
    .AddExceptionHandler()
    .Build();

host.Run();
