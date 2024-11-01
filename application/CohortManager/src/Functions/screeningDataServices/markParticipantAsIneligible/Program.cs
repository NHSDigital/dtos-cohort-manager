using Common;
using Data.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {

        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.TryAddTransient<IParticipantManagerData, ParticipantManagerData>();
        services.TryAddTransient<IDatabaseHelper, DatabaseHelper>();
        services.AddSingleton<ICallFunction, CallFunction>();
    })
    .AddDatabaseConnection()
    .AddExceptionHandler()
    .Build();

host.Run();
