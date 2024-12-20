
using Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Data.Database;
using Microsoft.Extensions.DependencyInjection.Extensions;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddTransient<IDatabaseHelper, DatabaseHelper>();
        services.AddTransient<ICreateDemographicData, CreateDemographicData>();
    })
    .AddExceptionHandler()
    .AddDatabaseConnection()
    .Build();

host.Run();
