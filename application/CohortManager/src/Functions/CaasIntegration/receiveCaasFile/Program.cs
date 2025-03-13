using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Common;
using Data.Database;
using Common.Interfaces;
using Microsoft.Extensions.Logging;
using NHS.Screening.ReceiveCaasFile;
using Model;
using DataServices.Client;
using HealthChecks.Extensions;


var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger("program.cs");

try
{
    var host = new HostBuilder()
        .AddConfiguration<ReceiveCaasFileConfig>(out ReceiveCaasFileConfig config)
        .AddDataServicesHandler()
        .AddDataService<ParticipantDemographic>(config.DemographicDataServiceURL)
        .AddCachedDataService<ScreeningLkp>(config.ScreeningLkpDataServiceURL)
        .Build()
    .ConfigureFunctionsWebApplication()

    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddTransient<ICallFunction, CallFunction>();
        services.AddSingleton<IReceiveCaasFileHelper, ReceiveCaasFileHelper>();
        services.AddScoped<IProcessCaasFile, ProcessCaasFile>(); //Do not change the lifetime of this.
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddScoped<ICheckDemographic, CheckDemographic>();
        services.AddScoped<ICreateBasicParticipantData, CreateBasicParticipantData>();
        services.AddScoped<IAddBatchToQueue, AddBatchToQueue>();
        services.AddScoped<IRecordsProcessedTracker, RecordsProcessedTracker>(); //Do not change the lifetime of this.
        services.AddHttpClient<ICheckDemographic, CheckDemographic>(client =>
        {
            client.BaseAddress = new Uri(config.DemographicURI);
        });
        services.AddScoped<IValidateDates, ValidateDates>();
        // Register health checks
        services.AddBlobStorageHealthCheck("receiveCaasFile");
    })
    .AddAzureQueues()
    .AddExceptionHandler()
    .AddDatabaseConnection()
    .Build();

    await host.RunAsync();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "failed to start up function receive caas file function function");
}
