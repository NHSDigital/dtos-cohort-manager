namespace Common;

using System.Text.Json;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public static class ConfigurationExtension
{
    public static IHostBuilder AddConfiguration<T>(this IHostBuilder hostBuilder, string? keyVaultUrl = null) where T: class
    {
        var configuration = CreateConfiguration<T>(keyVaultUrl);
        return BuildIOptionsDependency<T>(hostBuilder,configuration);
    }
    public static IHostBuilder AddConfiguration<T>(this IHostBuilder hostBuilder, out T config, string? keyVaultUrl = null) where T: class
    {

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger(nameof(ConfigurationExtension));

        var configuration = CreateConfiguration<T>(keyVaultUrl);

        config = configuration.Get<T>();
        return BuildIOptionsDependency<T>(hostBuilder,configuration);
    }

    private static IConfiguration CreateConfiguration<T>(string? keyVaultUrl = null) where T: class
    {

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger(nameof(ConfigurationExtension));
        logger.LogInformation("Building Configuration");
        ConfigurationBuilder configBuilder = new ConfigurationBuilder();
        keyVaultUrl ??= Environment.GetEnvironmentVariable("KeyVaultConnectionString");
        if(keyVaultUrl != null){
            try
            {
                configBuilder.AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential(), new AzureKeyVaultConfigurationOptions());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unable to add Azure KeyVault");
            }
        }
        configBuilder.AddEnvironmentVariables();
        return configBuilder.Build();
    }

    private static IHostBuilder BuildIOptionsDependency<T>(IHostBuilder hostBuilder, IConfiguration configuration) where T: class
    {
        return hostBuilder.ConfigureServices(_ =>
        {
            _.AddOptions<T>()
                .Bind(configuration)
                .ValidateDataAnnotations()
                .ValidateOnStart();
        });
    }
}
