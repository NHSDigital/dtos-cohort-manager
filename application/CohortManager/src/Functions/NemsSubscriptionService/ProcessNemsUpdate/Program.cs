using Common;
using Common.Interfaces;
using Model;
using DataServices.Client;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NHS.Screening.ProcessNemsUpdate;

var host = new HostBuilder()
    .AddConfiguration<ProcessNemsUpdateConfig>(out ProcessNemsUpdateConfig config)
        .AddDataServicesHandler()
        .AddDataService<ParticipantDemographic>(config.ParticipantDemographicDataServiceURL)
        .Build()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<IFhirPatientDemographicMapper, FhirPatientDemographicMapper>();
        services.AddScoped<ICreateBasicParticipantData, CreateBasicParticipantData>();
        services.AddScoped<IAddBatchToQueue, AddBatchToQueue>();
        services.AddBlobStorageHealthCheck("ProcessNemsUpdate");
    })
    .AddTelemetry()
    .AddExceptionHandler()
    .AddHttpClient()
    .AddAzureQueues()
    .Build();

await host.RunAsync();
