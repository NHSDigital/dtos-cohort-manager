using DataServices.Core;
using DataServices.Database;
using HealthChecks.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .AddDataServicesHandler<DataServicesContext>()
    .ConfigureServices(services =>
    {
        services.AddDatabaseHealthCheck("ReferenceDataService");
    })
    .Build();


await host.RunAsync();
