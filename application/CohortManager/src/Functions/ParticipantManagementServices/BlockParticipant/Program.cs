using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using Common;
using DataServices.Client;
using NHS.Screening.BlockParticipant;
using Model;

var host = new HostBuilder()
    .AddConfiguration<BlockParticipantConfig>(out BlockParticipantConfig config)
    .ConfigureFunctionsWebApplication()
    .AddDataServicesHandler()
        .AddDataService<ParticipantManagement>(config.ParticipantManagementUrl)
        .Build()
    .Build();

await host.RunAsync();