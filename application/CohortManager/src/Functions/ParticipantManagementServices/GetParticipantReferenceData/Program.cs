using Common;
using Data.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Model;
using DataServices.Client;
using NHS.Screening.GetParticipantReferenceData;


var host = new HostBuilder()
    .AddConfiguration<GetParticipantReferenceDataConfig>(out GetParticipantReferenceDataConfig config)
    .ConfigureFunctionsWorkerDefaults()
    .AddDataServicesHandler()
        .AddCachedDataService<HigherRiskReferralReasonLkp>(config.HigherRiskReferralReasonLkpDataServiceUrl)
        .AddCachedDataService<GeneCodeLkp>(config.GeneCodeLkpDataServiceUrl)
        .Build()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<IDatabaseHelper, DatabaseHelper>();
    })
    .Build();

await host.RunAsync();
