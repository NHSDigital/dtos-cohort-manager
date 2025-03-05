using Common;
using Data.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NHS.CohortManager.CohortDistributionService;

var hostBuilder = new HostBuilder();


hostBuilder.AddConfiguration<CreateCohortDistributionConfig>();
var host = hostBuilder.ConfigureFunctionsWorkerDefaults()
.ConfigureServices(services =>
{
    services.AddSingleton<ICallFunction, CallFunction>();
    services.AddSingleton<ICreateResponse, CreateResponse>();
    services.AddSingleton<ICohortDistributionHelper, CohortDistributionHelper>();
    services.TryAddTransient<IDatabaseHelper, DatabaseHelper>();
})
.AddAzureQueues()
.AddDatabaseConnection()
.AddExceptionHandler()
.Build();

await host.RunAsync();
