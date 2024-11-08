using Common;
using Data.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Data.SqlClient;
using System.Data;
using Model;
using DataServices.Client;
using DataServices.Database;

var hostBuilder = new HostBuilder();

hostBuilder.AddConfiguration<LookupValidationConfig>(out LookupValidationConfig config);

var host = hostBuilder.ConfigureFunctionsWorkerDefaults()
    .AddDataServicesHandler()
        .AddCachedDataService<BsSelectGpPractice>(config.BsSelectGpPracticeUrl )
        .AddCachedDataService<BsSelectOutCode>(config.BsSelectOutCodeUrl)
        .AddCachedDataService<LanguageCode>(config.LanguageCodeUrl)
        .AddCachedDataService<CurrentPosting>(config.CurrentPostingUrl)
        .Build()
    .ConfigureServices(services =>
    {

        services.AddSingleton<ICallFunction, CallFunction>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<IReadRulesFromBlobStorage, ReadRulesFromBlobStorage>();
        services.AddSingleton<IDataLookupFacade,DataLookupFacade>();
    })
    .AddDatabaseConnection()
    .AddExceptionHandler()
    .Build();

host.Run();
