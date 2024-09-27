using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Common;
using Data.Database;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using System.Data;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddScoped<ICallFunction, CallFunction>();
        services.AddSingleton<IScreeningServiceData, ScreeningServiceData>();
    }).AddDatabaseConnection()
    .Build();

host.Run();
