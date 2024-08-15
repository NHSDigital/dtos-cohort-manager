using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Tests.Integration.Helpers
{
    public abstract class BaseIntegrationTest
    {
        protected ILogger<BaseIntegrationTest> Logger { get; private set; }

        [TestInitialize]
        public async Task InitializeAsync()
        {
            InitializeLogger();
            LoadConfiguration();
            AssertAllConfigurations();
            await AdditionalSetupAsync();
        }

        private void InitializeLogger()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            Logger = loggerFactory.CreateLogger<BaseIntegrationTest>();
        }

        protected abstract void LoadConfiguration();

        protected abstract void AssertAllConfigurations();

        protected virtual Task AdditionalSetupAsync()
        {
            // Override this in derived classes to add additional setup if needed
            return Task.CompletedTask;
        }
    }
}
