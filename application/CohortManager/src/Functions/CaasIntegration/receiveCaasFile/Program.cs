using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Common;
using Data.Database;
using Common.Interfaces;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddScoped<ICallFunction, CallFunction>();
        services.AddTransient<IScreeningServiceData, ScreeningServiceData>();
        services.AddSingleton<IReceiveCaasFileHelper, ReceiveCaasFileHelper>();
    })
    .AddDatabaseConnection()
    .Build();

host.Run();
