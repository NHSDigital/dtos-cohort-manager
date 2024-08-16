using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NHS.MESH.Client;
using Common;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services => {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddMeshClient(_ => _.MeshApiBaseUrl = Environment.GetEnvironmentVariable("MeshApiBaseUrl"));
        services.AddSingleton<IBlobStorageHelper, BlobStorageHelper>();
        services.AddTransient<IMeshToBlobTransferHandler,MeshToBlobTransferHandler>();
    })
    .AddExceptionHandler()
    .Build();

host.Run();
