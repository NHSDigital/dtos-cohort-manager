using Common;
using Common.Interfaces;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DataServices.Client;
using Model;
using NHS.CohortManager.DemographicServices;

var host = new HostBuilder()
    .AddConfiguration<RetrievePDSDemographicConfig>(out RetrievePDSDemographicConfig config)
    .ConfigureFunctionsWorkerDefaults()
    .AddDataServicesHandler()
        .AddDataService<ParticipantDemographic>(config.DemographicDataServiceURL)
        .Build()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<IHttpParserHelper, HttpParserHelper>();
        services.AddSingleton<IFhirPatientDemographicMapper, FhirPatientDemographicMapper>();
        services.AddSingleton<IAddBatchToQueue, AddBatchToQueue>();
        // Register health checks
        services.AddBasicHealthCheck("RetrievePdsDemographic");
    })
    .AddJwtTokenSigning(config.UseFakePDSServices)
    .AddTelemetry()
    .AddServiceBusClient(config.ServiceBusConnectionString)
    .AddHttpClient(config.UseFakePDSServices)
    .Build();

await host.RunAsync();
