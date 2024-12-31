using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Common;
using Data.Database;
using Common.Interfaces;
using Microsoft.Extensions.Logging;
using NHS.Screening.ReceiveCaasFile;
using receiveCaasFile;
using Microsoft.Extensions.Azure;
using Azure.Identity;

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger("program.cs");


try
{
    var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddTransient<ICallFunction, CallFunction>();
        services.AddTransient<IScreeningServiceData, ScreeningServiceData>();
        services.AddSingleton<IReceiveCaasFileHelper, ReceiveCaasFileHelper>();
        services.AddScoped<IProcessCaasFile, ProcessCaasFile>(); //Do not change the lifetime of this.
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddScoped<ICheckDemographic, CheckDemographic>();
        services.AddScoped<ICreateBasicParticipantData, CreateBasicParticipantData>();
        services.AddScoped<IAddBatchToQueue, AddBatchToQueue>();
        services.AddScoped<RecordsProcessedTracker>(); //Do not change the lifetime of this.
        services.AddAzureClients(builder =>
        {
            // Use the environment credential by default
            builder.UseCredential(new DefaultAzureCredential());
            builder.AddQueueServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "")
              .ConfigureOptions(c => c.MessageEncoding = Azure.Storage.Queues.QueueMessageEncoding.Base64);
        });
    })
    .AddExceptionHandler()
    .AddDatabaseConnection()
    .Build();

    host.Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "failed to start up function receive caas file function function");
}
