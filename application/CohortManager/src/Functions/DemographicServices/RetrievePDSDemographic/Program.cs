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
    .AddConfiguration<JwtTokenServiceConfig>(out JwtTokenServiceConfig JwtTokenServiceConfig)
    .ConfigureFunctionsWorkerDefaults()
    .AddDataServicesHandler()
        .AddDataService<ParticipantDemographic>(config.DemographicDataServiceURL)
        .Build()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<IHttpParserHelper, HttpParserHelper>();
        services.AddSingleton<IFhirPatientDemographicMapper, FhirPatientDemographicMapper>();
        services.AddSingleton<IAuthClientCredentials, AuthClientCredentials>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddMemoryCache();
        // Register health checks
        services.AddBasicHealthCheck("RetrievePdsDemographic");
    })
    .GetPrivateKey(JwtTokenServiceConfig)
    .AddTelemetry()
    .AddHttpClient()
    .Build();

await host.RunAsync();
