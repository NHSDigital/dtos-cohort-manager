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
        services.AddTransient<IParticipantManagerData, ParticipantManagerData>();
        services.AddTransient<IGetParticipantData, GetParticipantData>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.TryAddTransient<IParticipantManagerData, ParticipantManagerData>();
        services.TryAddTransient<IDatabaseHelper, DatabaseHelper>();
        services.AddSingleton<ICallFunction, CallFunction>();
    })
    .AddDatabaseConnection()
    .AddExceptionHandler()
    .Build();

await host.RunAsync();
