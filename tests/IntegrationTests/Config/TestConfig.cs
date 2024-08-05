using Microsoft.Extensions.Configuration;
using System.IO;

public static class TestConfig
{
    private static readonly IConfigurationRoot Configuration;

    static TestConfig()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var configPath = Path.Combine(currentDirectory, "Config", "appsettings.json");

        var builder = new ConfigurationBuilder()
            .SetBasePath(currentDirectory)
            .AddJsonFile(configPath, optional: false, reloadOnChange: true)
            .AddEnvironmentVariables();
        Configuration = builder.Build();

    }

    public static string Get(string key)
    {
        return Configuration[key];
    }

    public static IConfigurationSection GetSection(string key)
    {
        return Configuration.GetSection(key);
    }
}
