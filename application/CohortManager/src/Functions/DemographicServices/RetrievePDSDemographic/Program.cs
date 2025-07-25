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
        // Register health checks
        services.AddBasicHealthCheck("RetrievePdsDemographic");
    })
    .AddJwtTokenSigning()
    .AddTelemetry()
    .AddHttpClient()
    .Build();

await host.RunAsync();
