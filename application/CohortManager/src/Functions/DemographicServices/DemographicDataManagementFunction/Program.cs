using Common;
using DataServices.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Model;
using NHS.Screening.DemographicDataManagementFunction;

var host = new HostBuilder()
    .AddConfiguration<DemographicDataManagementFunctionConfig>(out DemographicDataManagementFunctionConfig config)
    .AddDataServicesHandler()
        .AddDataService<ParticipantDemographic>(config.ParticipantDemographicDataServiceURL)
        .Build()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddScoped<ICallFunction, CallFunction>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
    })
    .Build();

await host.RunAsync();
