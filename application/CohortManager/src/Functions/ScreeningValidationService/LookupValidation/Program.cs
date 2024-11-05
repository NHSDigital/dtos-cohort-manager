using Common;
using Data.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Data.SqlClient;
using System.Data;
using DataServices.Client;
using DataServices.Database;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .AddDataServicesHandler()
        .AddDataService<BsSelectGpPractice>("http://localhost:7998/api/" )
        .Build()
    .ConfigureServices(services =>
    {

        services.AddSingleton<ICallFunction, CallFunction>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<IReadRulesFromBlobStorage, ReadRulesFromBlobStorage>();
        services.AddTransient<IDbLookupValidationBreastScreening, DbLookupValidationBreastScreening>();
    })
    .AddDatabaseConnection()
    .AddExceptionHandler()
    .Build();

host.Run();
