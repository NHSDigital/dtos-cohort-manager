using Common;
using Data.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICallFunction, CallFunction>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<ICohortDistributionHelper, CohortDistributionHelper>();
        services.TryAddTransient<IDatabaseHelper, DatabaseHelper>();
        services.TryAddTransient<IParticipantManagerData, ParticipantManagerData>();
        services.AddSingleton<IAzureQueueStorageHelper, AzureQueueStorageHelper>();
    })
    .AddDatabaseConnection()
    .AddExceptionHandler()
    .Build();

host.Run();
