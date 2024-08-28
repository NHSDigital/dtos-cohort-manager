using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Azure.Storage.Blobs;
using System;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;

namespace Tests.Integration.Helpers
{
    public abstract class BaseIntegrationTest
    {
        protected IServiceProvider ServiceProvider { get; private set; }
        protected ILogger<BaseIntegrationTest> Logger { get; private set; }
        protected AppSettings AppSettings { get; private set; }
        protected BlobStorageHelper BlobStorageHelper { get; private set; }

        [TestInitialize]
        public void Setup()
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();
            Logger = ServiceProvider.GetService<ILogger<BaseIntegrationTest>>();
            AppSettings = ServiceProvider.GetService<IOptions<AppSettings>>()?.Value;
            BlobStorageHelper = ServiceProvider.GetService<BlobStorageHelper>();

            AssertAllConfigurations();
        }

        protected virtual void ConfigureServices(IServiceCollection services)
        {
            // Load configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("Config/appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Bind the configuration to AppSettings
            services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

            // Register common services
            services.AddLogging(configure => configure.AddConsole());

            // Register Blob Storage Helper dependencies
            services.AddSingleton(sp => new BlobServiceClient(configuration["AppSettings:AzureWebJobsStorage"]));
            services.AddSingleton<BlobStorageHelper>();
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<AppSettings>>().Value);
        }

        protected virtual void AssertAllConfigurations()
        {
            bool isValid = true; // Overall flag to indicate if all critical configurations are valid

            // Log the status of each configuration and service
            if (AppSettings == null)
            {
                Logger.LogError("AppSettings configuration is not set.");
                isValid = false;
            }
            else
            {
                if (AppSettings.ConnectionStrings?.DtOsDatabaseConnectionString == null)
                {
                    Logger.LogError("Database connection string is not set in AppSettings.");
                    isValid = false;
                }

                if (AppSettings.FilePaths?.Local == null)
                {
                    Logger.LogError("Local file path is not set in AppSettings.");
                    isValid = false;
                }

                if (AppSettings.BlobContainerName == null)
                {
                    Logger.LogError("Blob container name is not set in AppSettings.");
                    isValid = false;
                }
            }

            if (BlobStorageHelper == null)
            {
                Logger.LogError("BlobStorageHelper is not initialised. Ensure it is registered in the DI container.");
                isValid = false;
            }

            // Assert that all critical configurations are valid
            Assert.IsTrue(isValid, "Some configurations or services are not set. See logs for details.");
        }
    }
}