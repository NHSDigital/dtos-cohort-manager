using Common;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DataServices.Core;
using DataServices.Database;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .AddDataServicesHandler<DataServicesContext>()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<ICheckDemographic, CheckDemographic>();
        services.AddSingleton<ICreateParticipant, CreateParticipant>();
        services.AddSingleton<ICohortDistributionHandler, CohortDistributionHandler>();
        // Register health checks
        services.AddBasicHealthCheck("RemoveParticipant");
    })
    .AddAzureQueues()
    .AddExceptionHandler()
    .AddHttpClient()
    .Build();

await host.RunAsync();
