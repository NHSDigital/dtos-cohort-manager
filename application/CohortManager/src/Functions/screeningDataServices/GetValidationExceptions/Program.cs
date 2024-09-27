using System.Data;
using System.Data.Common;
using Common;
using Data.Database;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<IValidationExceptionData, ValidationExceptionData>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
    })
    .AddDatabaseConnection()
    .Build();

host.Run();
