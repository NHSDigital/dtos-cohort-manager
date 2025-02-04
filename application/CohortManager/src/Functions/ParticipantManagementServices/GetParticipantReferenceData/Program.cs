using Common;
using Data.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Model;
using DataServices.Client;


var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .AddDataServicesHandler()
        .AddCachedDataService<HigherRiskReferralReasonLkp>(Environment.GetEnvironmentVariable("HigherRiskReferralReasonLkpDataServiceUrl"))
        .AddCachedDataService<GeneCodeLkp>(Environment.GetEnvironmentVariable("GeneCodeLkpDataServiceUrl"))
        .Build()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<IDatabaseHelper, DatabaseHelper>();
    })
    .Build();

await host.RunAsync();
