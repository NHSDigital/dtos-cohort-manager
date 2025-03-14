using Common;
using Data.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Model;
using DataServices.Client;
using HealthChecks.Extensions;


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
        // Register health checks
        services.AddBasicHealthCheck("GetParticipantReferenceData");
    })
    .Build();

await host.RunAsync();
