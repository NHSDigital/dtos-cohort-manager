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
        services.AddTransient<IDatabaseHelper, DatabaseHelper>();
        services.AddTransient<IGetParticipantData, GetParticipantData>();
        services.AddTransient<IParticipantManagerData, ParticipantManagerData>();
        services.AddSingleton<ICallFunction, CallFunction>();
    })
    .AddDatabaseConnection()
    .AddExceptionHandler()
    .Build();

host.Run();
