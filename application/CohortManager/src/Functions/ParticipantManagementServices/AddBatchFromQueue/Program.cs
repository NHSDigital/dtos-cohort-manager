using Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using DataServices.Client;

using Model;
using Data.Database;
using AddBatchFromQueue;
using Microsoft.Azure.Amqp.Serialization;


var host = new HostBuilder()
    .AddConfiguration<AddBatchFromQueueConfig>(out AddBatchFromQueueConfig config)
    .ConfigureFunctionsWebApplication()
    .AddDataServicesHandler()
    .AddDataService<ParticipantManagement>(config.ParticipantManagementUrl)
    .Build()
    .ConfigureServices(services =>
    {
        services.TryAddTransient<IDatabaseHelper, DatabaseHelper>();
        services.AddSingleton<ICallFunction, CallFunction>();
        services.AddSingleton<ICohortDistributionHandler, CohortDistributionHandler>();
        services.AddSingleton<IAzureQueueStorageHelper, AzureQueueStorageHelper>();
        services.AddSingleton<IQueueClientFactory, QueueClientFactory>();
        services.AddSingleton<ICheckDemographic, CheckDemographic>();
        services.AddSingleton<ICreateParticipant, CreateParticipant>();
        services.AddSingleton<IExceptionHandler, ExceptionHandler>();
        services.AddSingleton<IDurableAddProcessor, DurableAddProcessor>();
    })
    .AddDatabaseConnection()
    .AddExceptionHandler()
    .Build();

await host.RunAsync();
