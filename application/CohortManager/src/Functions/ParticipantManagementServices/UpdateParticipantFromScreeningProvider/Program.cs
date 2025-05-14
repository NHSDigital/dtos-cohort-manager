using Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DataServices.Client;
using Model;
using NHS.Screening.UpdateParticipantFromScreeningProvider;

var host = new HostBuilder()
    .AddConfiguration<UpdateParticipantFromScreeningProviderConfig>(out UpdateParticipantFromScreeningProviderConfig config)
    .ConfigureFunctionsWorkerDefaults()
    .AddDataServicesHandler()
        .AddDataService<ParticipantManagement>(config.ParticipantManagementUrl)
        .AddCachedDataService<GeneCodeLkp>(config.GeneCodeLkpUrl)
        .AddCachedDataService<HigherRiskReferralReasonLkp>(config.HigherRiskReferralReasonLkpUrl)
        .Build()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
    })
    .AddEventGridClient()
    .AddExceptionHandler()
    .Build();

await host.RunAsync();
