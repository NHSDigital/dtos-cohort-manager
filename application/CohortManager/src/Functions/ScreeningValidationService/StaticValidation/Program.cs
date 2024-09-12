using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Common;
using Data.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICallFunction, CallFunction>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<IReadRulesFromBlobStorage, ReadRulesFromBlobStorage>();
    })
    .AddExceptionHandler()
    .Build();

host.Run();
