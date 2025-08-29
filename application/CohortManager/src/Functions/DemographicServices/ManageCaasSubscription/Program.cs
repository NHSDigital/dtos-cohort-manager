using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using HealthChecks.Extensions;
using Common;
using NHS.CohortManager.DemographicServices;
using DataServices.Database;
using DataServices.Core;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .AddConfiguration<ManageCaasSubscriptionConfig>(out ManageCaasSubscriptionConfig config)
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddBasicHealthCheck("ManageCaasSubscription");
        services.AddSingleton<IMeshSendCaasSubscribe, MeshSendCaasSubscribeStub>();
        services.AddScoped<IMeshPoller, MeshPoller>();
        // Wrap NemsSubscription accessor to enforce SubscriptionSource = MESH when missing
        services.AddScoped<DataServices.Core.IDataServiceAccessor<Model.NemsSubscription>>(sp =>
        {
            var inner = sp.GetRequiredService<DataServices.Core.IDataServiceAccessor<Model.NemsSubscription>>();
            return new NHS.CohortManager.DemographicServices.CaasNemsSubscriptionAccessor(inner);
        });
    })
    .AddDataServicesHandler<DataServicesContext>()
    .AddHttpClient()
    .AddTelemetry()
    .AddExceptionHandler()
    .Build();

await host.RunAsync();
