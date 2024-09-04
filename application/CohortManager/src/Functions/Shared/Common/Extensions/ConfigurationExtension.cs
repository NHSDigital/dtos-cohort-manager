namespace Common;

using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class ConfigurationExtension
{
    public static IHostBuilder AddConfiguration<T>(this IHostBuilder hostBuilder, string? keyVaultUrl) where T: class
    {
        ConfigurationBuilder configBuilder = new ConfigurationBuilder();
        keyVaultUrl ??= Environment.GetEnvironmentVariable("KeyVaultUrl");
        if(keyVaultUrl != null){
            try
            {
                configBuilder.AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential(), new AzureKeyVaultConfigurationOptions());
            }
            catch
            {

            }
        }
        configBuilder.AddEnvironmentVariables();
        IConfiguration configuration = configBuilder.Build();

        return hostBuilder.ConfigureServices(_ =>
        {
            _.AddOptions<T>()
                .Bind(configuration)
                .ValidateDataAnnotations()
                .ValidateOnStart();
        });
    }
}
