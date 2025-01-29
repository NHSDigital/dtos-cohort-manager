using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using dtos_cohort_manager_specflow.Config;
using dtos_cohort_manager_specflow.Helpers;
using Microsoft.Extensions.Options;
using FluentAssertions;
using TechTalk.SpecFlow;

namespace dtos_cohort_manager_specflow.Hook
{
    [Binding]
    public class IntegrationTestHooks(ScenarioContext scenarioContext)
    {
        protected ILogger<IntegrationTestHooks>? Logger { get; private set; }
        protected AppSettings? AppSettings { get; private set; }
        protected BlobStorageHelper? BlobStorageHelper { get; private set; }

        private readonly ScenarioContext _scenarioContext = scenarioContext;

        [BeforeScenario]
        public void BeforeScenario(IServiceProvider services)
        {

            // Retrieve configured services
            Logger = services.GetService<ILogger<IntegrationTestHooks>>();
            AppSettings = services.GetService<IOptions<AppSettings>>()?.Value;
            BlobStorageHelper = services.GetService<BlobStorageHelper>();

            // Validate the configuration
            AssertAllConfigurations();
        }

        [AfterScenario]
        public void AfterScenario()
        {
            //// Clean up resources if necessary
            //if (ServiceProvider is IDisposable disposable)
            //{
            //    disposable.Dispose();
            //}
        }

        private void AssertAllConfigurations()
        {
            AppSettings.Should().NotBeNull("AppSettings configuration is not set.");
            _ = (AppSettings.ConnectionStrings?.DtOsDatabaseConnectionString.Should().NotBeNull("Database connection string is not set in AppSettings."));
            AppSettings.FilePaths?.Add.Should().NotBeNull("Local file path is not set in AppSettings.");
            AppSettings.BlobContainerName.Should().NotBeNull("Blob container name is not set in AppSettings.");
            BlobStorageHelper.Should().NotBeNull("BlobStorageHelper is not initialized. Ensure it is registered in the DI container.");

            // Log success
            Logger?.LogInformation("All critical configurations and services are set correctly.");
        }
    }
}