using Common;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Model;
using NHS.Screening.NEMSUnSubscription;

var host = new HostBuilder()
.AddConfiguration<NEMSUnSubscriptionConfig>(out NEMSUnSubscriptionConfig config)
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddHttpClient();
    })
    .Build();

await host.RunAsync();
