using Microsoft.Extensions.Hosting;
using DataServices.Core;
using DataServices.Database;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .AddDataServicesHandler<DataServicesContext>()
    .Build();

await host.RunAsync();
