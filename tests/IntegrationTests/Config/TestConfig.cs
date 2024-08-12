using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.IO;

public class AppSettings
{
    public ConnectionStrings ConnectionStrings { get; set; }
    public FilePaths FilePaths { get; set; }
    public string BlobContainerName { get; set; }
    public string AzureWebJobsStorage {get; set; }
}

public class ConnectionStrings
{
    public string DtOsDatabaseConnectionString { get; set; }
}

public class FilePaths
{
    public string Local { get; set; }
}

public static class TestConfig
{
    private static ServiceProvider _serviceProvider;

    static TestConfig()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var configPath = Path.Combine(currentDirectory, "Config", "appsettings.json");

        var builder = new ConfigurationBuilder()
            .SetBasePath(currentDirectory)
            .AddJsonFile(configPath, optional: false, reloadOnChange: true)
            .AddEnvironmentVariables();

        var configuration = builder.Build();

        var services = new ServiceCollection();
        services.Configure<AppSettings>(configuration);

        _serviceProvider = services.BuildServiceProvider();
    }

    public static AppSettings Get()
    {
        return _serviceProvider.GetService<IOptions<AppSettings>>().Value;
    }
}
