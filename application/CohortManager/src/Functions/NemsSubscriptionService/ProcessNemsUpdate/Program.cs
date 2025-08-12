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
    .ConfigureFunctionsWorkerDefaults()
      .AddDataServicesHandler()
        .AddDataService<ParticipantDemographic>(config.DemographicDataServiceURL)
        .Build()
    .ConfigureServices(services =>
    {
        services.AddSingleton<IFhirPatientDemographicMapper, FhirPatientDemographicMapper>();
        services.AddScoped<ICreateBasicParticipantData, CreateBasicParticipantData>();
        services.AddScoped<IAddBatchToQueue, AddBatchToQueue>();
        services.AddScoped<IBlobStorageHelper, BlobStorageHelper>();
        services.AddBlobStorageHealthCheck("ProcessNemsUpdate");
    })
    .AddTelemetry()
    .AddExceptionHandler()
    .AddHttpClient()
    .AddServiceBusClient(config.ServiceBusConnectionString_client_internal)
    .Build();

await host.RunAsync();
