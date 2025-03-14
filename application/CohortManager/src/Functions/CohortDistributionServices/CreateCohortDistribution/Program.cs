using Common;
using Data.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using DataServices.Client;
using Model;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .AddDataServicesHandler()
        .AddDataService<ParticipantManagement>(Environment.GetEnvironmentVariable("ParticipantManagementUrl"))
        .Build()
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
