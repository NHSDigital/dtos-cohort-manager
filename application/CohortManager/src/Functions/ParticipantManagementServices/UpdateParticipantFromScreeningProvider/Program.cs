using Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DataServices.Client;
using Model;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .AddDataServicesHandler()
        .AddDataService<ParticipantManagement>(Environment.GetEnvironmentVariable("ParticipantManagementUrl"))
        .AddCachedDataService<GeneCodeLkp>(Environment.GetEnvironmentVariable("GeneCodeLkpUrl"))
        .AddCachedDataService<HigherRiskReferralReasonLkp>(Environment.GetEnvironmentVariable("HigherRiskReferralReasonLkpUrl"))
        .Build()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
    })
    .AddEventGridClient()
    .AddExceptionHandler()
    .Build();

await host.RunAsync();
