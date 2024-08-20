using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NHS.MESH.Client;
using Common;
using System.Security.Cryptography.X509Certificates;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services => {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services
            .AddMeshClient(_ => _.MeshApiBaseUrl = Environment.GetEnvironmentVariable("MeshApiBaseUrl"))
            .AddMailbox(Environment.GetEnvironmentVariable("BSSMailbox")!,new NHS.MESH.Client.Configuration.MailboxConfiguration
            {
                Password = Environment.GetEnvironmentVariable("MeshPassword"),
                SharedKey = Environment.GetEnvironmentVariable("MeshSharedKey"),
                Cert = new X509Certificate2("mycert.pfx",Environment.GetEnvironmentVariable("MeshKeyPassphrase")) //THIS WILL NEED CHANGING TO PULL FROM A KEYSTORE OR BLOB
            })
            .Build();
        services.AddSingleton<IBlobStorageHelper, BlobStorageHelper>();
        services.AddTransient<IMeshToBlobTransferHandler,MeshToBlobTransferHandler>();
    })
    .AddExceptionHandler()
    .Build();

host.Run();
