using NHS.CohortManager.SmokeTests.Config;
using NHS.CohortManager.SmokeTests.Contexts;
using NHS.CohortManager.SmokeTests.Helpers;
using NHS.CohortManager.SmokeTests.TestServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reqnroll.Microsoft.Extensions.DependencyInjection;

namespace NHS.CohortManager.SmokeTests;

internal static class Startup
{
    [ScenarioDependencies]
    public static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        return services;

    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Load configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("Config/appsettings.json", optional: false, reloadOnChange: true)
            //settings in the local file would override those in the base file
            .AddJsonFile("Config/appsettings-local.json", optional: true, reloadOnChange: true)
            .Build();

        // Bind AppSettings section to POCO
        services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

        // Add logging
        services.AddLogging(configure => configure.AddConsole());

        // Register Azure Blob Storage helper
        services.AddSingleton(sp =>
        {
            var connectionString = configuration["AppSettings:CloudFileStorageConnectionString"];
            return new Azure.Storage.Blobs.BlobServiceClient(connectionString);
        });
        services.AddSingleton<BlobStorageHelper>();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<AppSettings>>().Value);
        services.AddTransient<EndToEndFileUploadService>();

        services.AddScoped(_ => new SmokeTestsContext());
    }
}
