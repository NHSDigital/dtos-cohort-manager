using dtos_cohort_manager_specflow.Config;
using dtos_cohort_manager_specflow.Contexts;
using dtos_cohort_manager_specflow.Helpers;
using dtos_cohort_manager_specflow.TestServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SolidToken.SpecFlow.DependencyInjection;

namespace dtos_cohort_manager_specflow;

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
            .Build();

        // Bind AppSettings section to POCO
        services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

        // Add logging
        services.AddLogging(configure => configure.AddConsole());

        // Register Azure Blob Storage helper
        services.AddSingleton(sp =>
        {
            var connectionString = configuration["AppSettings:AzureWebJobsStorage"];
            return new Azure.Storage.Blobs.BlobServiceClient(connectionString);
        });
        services.AddSingleton<BlobStorageHelper>();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<AppSettings>>().Value);
        services.AddTransient<EndToEndFileUploadService>();

        services.AddScoped(_ => new SmokeTestsContext());
    }
}
