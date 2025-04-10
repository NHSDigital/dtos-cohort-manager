using Common;
using Common.Interfaces;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NHS.Screening.RetrievePDSDemographic;

var host = new HostBuilder()
    .AddConfiguration<RetrievePDSDemographicConfig>()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddHttpClient();
        services.AddScoped<IHttpClientFunction, HttpClientFunction>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<IHttpParserHelper, HttpParserHelper>();
        services.AddSingleton<IFhirPatientDemographicMapper, FhirPatientDemographicMapper>();
        // Register health checks
        services.AddBasicHealthCheck("RetrievePdsDemographic");
    })
    .Build();

await host.RunAsync();
