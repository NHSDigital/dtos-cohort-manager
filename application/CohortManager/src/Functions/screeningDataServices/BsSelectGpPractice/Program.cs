using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using DataServices.Core;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // services.AddApplicationInsightsTelemetryWorkerService();
        // services.ConfigureFunctionsApplicationInsights();

        // services.AddDbContext<DataServicesContext>(
        //     options => options.UseSqlServer(Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString"))
        // );
    })
    .AddDataServicesHandler()
    .Build();

host.Run();
