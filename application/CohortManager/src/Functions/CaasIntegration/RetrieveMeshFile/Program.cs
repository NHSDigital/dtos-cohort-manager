using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NHS.MESH.Client;
using Common;
using System.Security.Cryptography.X509Certificates;
using Azure.Security.KeyVault.Certificates;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using NHS.Screening.RetrieveMeshFile;
using System.Text.Json;


var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger("program.cs");

try
{
    var host = new HostBuilder();

    X509Certificate2 cert;

    host.AddConfiguration<RetrieveMeshFileConfig>(out RetrieveMeshFileConfig config);

    if(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("KeyVaultConnectionString")))
    {
        logger.LogInformation("Pulling Mesh Certificate from KeyVault");
        var client = new CertificateClient(vaultUri: new Uri(Environment.GetEnvironmentVariable("KeyVaultConnectionString")), credential: new DefaultAzureCredential());
        var certificate = await client.DownloadCertificateAsync(config.MeshKeyName);
        cert = certificate.Value;
    }
    else
    {
        logger.LogInformation("Pulling Mesh Certificate from local File");
        cert = new X509Certificate2(config.MeshKeyName,config.MeshKeyPassphrase);
    }

    //var jsonstring = JsonSerializer.Serialize(config);
    logger.LogInformation(config.BSSMailBox);

    host.ConfigureFunctionsWebApplication();
    host.ConfigureServices(services => {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services
            .AddMeshClient(_ => _.MeshApiBaseUrl = config.MeshApiBaseUrl)
            .AddMailbox(config.BSSMailBox,new NHS.MESH.Client.Configuration.MailboxConfiguration
            {
                Password = config.MeshPassword,
                SharedKey = config.MeshSharedKey,
                Cert = cert
            })
            .Build();
        services.AddSingleton<IBlobStorageHelper, BlobStorageHelper>();
        services.AddTransient<IMeshToBlobTransferHandler,MeshToBlobTransferHandler>();
    })
    .AddExceptionHandler();

    var app = host.Build();

    await app.RunAsync();
}
catch(Exception ex)
{
    logger.LogCritical(ex,"Failed to start up Function");
}


