using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NHS.MESH.Client;
using Common;
using System.Security.Cryptography.X509Certificates;
using Azure.Security.KeyVault.Certificates;
using Azure.Identity;
using Microsoft.Extensions.Logging;


var logFactory = new LoggerFactory();

var logger = logFactory.CreateLogger<Type>();

try
{


    X509Certificate2 cert;

    if(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("KeyVaultConnectionString")))
    {
        logger.LogInformation("Pulling Mesh Certificate from KeyVault");
        var client = new CertificateClient(vaultUri: new Uri(Environment.GetEnvironmentVariable("KeyVaultConnectionString")), credential: new DefaultAzureCredential());
        var certificate = client.DownloadCertificate(Environment.GetEnvironmentVariable("MeshKeyName"));
        cert = certificate.Value;
    }
    else
    {
        logger.LogInformation("Pulling Mesh Certificate from local File");
        cert = new X509Certificate2(Environment.GetEnvironmentVariable("MeshKeyName"),Environment.GetEnvironmentVariable("MeshKeyPassphrase"));
    }



    var host = new HostBuilder()
        .ConfigureFunctionsWebApplication()
        .ConfigureServices(services => {
            services.AddApplicationInsightsTelemetryWorkerService();
            services.ConfigureFunctionsApplicationInsights();
            services
                .AddMeshClient(_ => _.MeshApiBaseUrl = Environment.GetEnvironmentVariable("MeshApiBaseUrl"))
                .AddMailbox(Environment.GetEnvironmentVariable("BSSMailBox")!,new NHS.MESH.Client.Configuration.MailboxConfiguration
                {
                    Password = Environment.GetEnvironmentVariable("MeshPassword"),
                    SharedKey = Environment.GetEnvironmentVariable("MeshSharedKey"),
                    Cert = cert
                })
                .Build();
            services.AddSingleton<IBlobStorageHelper, BlobStorageHelper>();
            services.AddTransient<IMeshToBlobTransferHandler,MeshToBlobTransferHandler>();
        })
        .AddExceptionHandler()
        .Build();

    host.Run();
}
catch(Exception ex)
{
    logger.LogCritical(ex,"Failed to start up Function");
}


