using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Common;
using Data.Database;
using Common.Interfaces;
using Microsoft.Extensions.Logging;

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
        services.AddSingleton<IProcessCaasFile, ProcessCaasFile>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<ICheckDemographic, CheckDemographic>();
        services.AddSingleton<ICreateBasicParticipantData, CreateBasicParticipantData>();
        services.AddSingleton<IAzureQueueStorageHelper, AzureQueueStorageHelper>();
    })
    .AddExceptionHandler()
    .AddDatabaseConnection()
    .Build();

    host.Run();


}
catch (Exception ex)
{
    logger.LogCritical(ex, "failed to start up function ");

}
