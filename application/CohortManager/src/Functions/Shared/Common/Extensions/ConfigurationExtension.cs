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
    private static List<string> _emptyList = new List<string>();

    public static IHostBuilder AddConfiguration<T>(this IHostBuilder hostBuilder, string? keyVaultUrl = null) where T: class
    {
        var configuration = CreateConfiguration(keyVaultUrl);
        return BuildIOptionsDependency<T>(hostBuilder,configuration);
    }
    /// <summary>
    /// Create
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="hostBuilder"></param>
    /// <param name="config"></param>
    /// <param name="keyVaultUrl"></param>
    /// <returns></returns>
    public static IHostBuilder AddConfiguration<T>(this IHostBuilder hostBuilder, out T config, string? keyVaultUrl = null) where T: class
    {
        var configuration = CreateConfiguration(keyVaultUrl);

        config = configuration.Get<T>();
        return BuildIOptionsDependency<T>(hostBuilder,configuration);
    }
    /// <summary>
    /// Creates and bind configuration that is not registered to IOptions
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="keyVaultUrl"></param>
    /// <param name="configFilePaths"></param>
    /// <returns></returns>
    public static T GetConfiguration<T>(string? keyVaultUrl = null, List<string>? configFilePaths = null) where T: class
    {
        var configuration = CreateConfiguration(keyVaultUrl, configFilePaths);
        return configuration.Get<T>();

    }




    private static IConfiguration CreateConfiguration(string? keyVaultUrl = null, List<string>? configFilePaths = null)
    {

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger(nameof(ConfigurationExtension));

        logger.LogInformation("Building Configuration");
        ConfigurationBuilder configBuilder = new ConfigurationBuilder();

        keyVaultUrl ??= Environment.GetEnvironmentVariable("KeyVaultConnectionString");

        if(keyVaultUrl != null)
        {
            try
            {
                configBuilder.AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential(), new AzureKeyVaultConfigurationOptions());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unable to add Azure KeyVault");
            }
        }
        if(configFilePaths != null)
        {
            foreach(var configFile in configFilePaths)
            {
                configBuilder.AddJsonFile(configFile);
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
