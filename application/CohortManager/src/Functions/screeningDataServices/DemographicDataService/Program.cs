
using Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Data.Database;
using DataServices.Client;
using Model;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .Build();

await host.RunAsync();
