using System.Security.Cryptography;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Common;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


ConfigurationBuilder configBuilder = new ConfigurationBuilder();
configBuilder.AddAzureKeyVault(new Uri(Environment.GetEnvironmentVariable("KeyVaultUrl")), new DefaultAzureCredential(), new AzureKeyVaultConfigurationOptions());

IConfiguration configuration = configBuilder.Build();

var host = new HostBuilder()

    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICallFunction, CallFunction>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<ICheckDemographic, CheckDemographic>();
        services.AddSingleton<ICreateBasicParticipantData, CreateBasicParticipantData>();
        services.AddOptions<ProcessCaasFileConfig>()
            .Bind(configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    })

    .AddExceptionHandler()
    .Build();

host.Run();
