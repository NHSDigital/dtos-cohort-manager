using Common;
using Common.Interfaces;
using Data.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddTransient<IValidationExceptionData, ValidationExceptionData>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<IHttpParserHelper, HttpParserHelper>();
        services.AddScoped(typeof(IPaginationService<>), typeof(PaginationService<>));
    })
    .AddDatabaseConnection()
    .AddExceptionHandler()
    .Build();

host.Run();
