using Common;
using Data.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DataServices.Client;
using Model;
using NHS.Screening.MarkParticipantAsEligible;


var host = new HostBuilder()
    .AddConfiguration<MarkParticipantAsEligibleConfig>(out MarkParticipantAsEligibleConfig config)
    .ConfigureFunctionsWorkerDefaults()
    .AddDataServicesHandler()
    .AddDataService<ParticipantManagement>(config.ParticipantManagementUrl)
    .Build()
    .ConfigureServices(services =>
    {
        services.AddTransient<IGetParticipantData, GetParticipantData>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddTransient<IDatabaseHelper, DatabaseHelper>();
        services.AddSingleton<ICallFunction, CallFunction>();
    })
    .AddDatabaseConnection()
    .AddExceptionHandler()
    .Build();

await host.RunAsync();
