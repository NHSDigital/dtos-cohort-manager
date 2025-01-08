using Common;
using Common.Interfaces;
using Data.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<IDatabaseHelper, DatabaseHelper>();
        services.AddSingleton<ICallFunction, CallFunction>();
        .AddDataService<GeneCodeLkp>(Environment.GetEnvironmentVariable("GeneCodeLkpUrl"))
        .AddDataService<HigherRiskReferralReasonLkp>(Environment.GetEnvironmentVariable("HigherRiskReferralReasonLkpUrl"))
    })
    .AddDatabaseConnection()
    .AddExceptionHandler()
    .Build();

host.Run();
