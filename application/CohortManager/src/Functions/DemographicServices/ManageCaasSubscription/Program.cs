using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using HealthChecks.Extensions;
using Common;
using NHS.CohortManager.DemographicServices;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .AddConfiguration<ManageCaasSubscriptionConfig>(out ManageCaasSubscriptionConfig config)
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddBasicHealthCheck("ManageCaasSubscription");
        services.AddSingleton<IMeshSendCaasSubscribe, MeshSendCaasSubscribeStub>();
    })
    .AddHttpClient()
    .AddTelemetry()
    .AddExceptionHandler()
    .Build();

await host.RunAsync();
