using Microsoft.VisualStudio.TestTools.UnitTesting;
using Azure.Storage.Blobs;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Tests.Integration.Helpers;

namespace Tests.Integration.EndtoEndTests
{
    [TestClass]
    [TestCategory("Integration")]
    public class E2E_FileUploadAndCohortDistributionTest : BaseIntegrationTest
    {
        private BlobStorageHelper _blobStorageHelper;
        private AppSettings _config;
        private ILogger<E2E_FileUploadAndCohortDistributionTest> _logger;
        private List<string> _nhsNumbers;
        private string _connectionString;
        private string _localFilePath;
        private string _blobContainerName;

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
            services.AddSingleton<AppSettings>();
            services.AddSingleton<BlobStorageHelper>();
        }

        [TestInitialize]
        public async Task TestInitialize()
        {
            _config = ServiceProvider.GetService<AppSettings>();
            _blobStorageHelper = ServiceProvider.GetService<BlobStorageHelper>();
            _logger = ServiceProvider.GetService<ILogger<E2E_FileUploadAndCohortDistributionTest>>();
            _connectionString = AppSettings.ConnectionStrings.DtOsDatabaseConnectionString;
            _localFilePath = AppSettings.FilePaths.Local;
            _blobContainerName = AppSettings.BlobContainerName;
            _nhsNumbers = CsvHelperService.ExtractNhsNumbersFromCsv(_localFilePath);

            await CleanDatabaseAsync();
        }

        private async Task CleanDatabaseAsync()
        {
            var query = "DELETE FROM PARTICIPANT_MANAGEMENT; DELETE FROM PARTICIPANT_DEMOGRAPHIC; DELETE FROM BS_COHORT_DISTRIBUTION";
            await DatabaseHelper.ExecuteNonQueryAsync(_connectionString, query);
            _logger.LogInformation("Database cleanup completed.");
        }

        [TestMethod]
        public async Task EndToEnd_FileUploadAndDistributionTest()
        {
            _logger.LogInformation("Starting end-to-end Cohort Distribution test");

            var originalDistributionCount = await DatabaseHelper.GetRecordCountAsync(_connectionString, "BS_COHORT_DISTRIBUTION");

            await UploadFileToBlobStorageAsync();

            var distributionVerified = await VerifyRecordCountAsync("BS_COHORT_DISTRIBUTION", originalDistributionCount, expectedIncrement: _nhsNumbers.Count);

            Assert.IsTrue(distributionVerified, $"The expected number of Cohort Distribution records ({originalDistributionCount + _nhsNumbers.Count}) was not found in the database.");

            _logger.LogInformation("Starting additional data integrity check for NHS Numbers in the BS_COHORT_DISTRIBUTION table.");

            await DatabaseValidationHelper.VerifyNhsNumbersAsync(_connectionString, "BS_COHORT_DISTRIBUTION", _nhsNumbers, _logger);

            _logger.LogInformation("Completed data integrity check.");
        }

        private async Task UploadFileToBlobStorageAsync()
        {
            Assert.IsTrue(File.Exists(_localFilePath), $"File not found at {_localFilePath}");
            _logger.LogInformation("Uploading file {FilePath} to blob storage", _localFilePath);

            await _blobStorageHelper.UploadFileToBlobStorageAsync(_localFilePath, _blobContainerName);

            _logger.LogInformation("File uploaded successfully");
        }

        private async Task<bool> VerifyRecordCountAsync(string tableName, int originalCount, int expectedIncrement, int maxRetries = 10, int delay = 1000)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                var newCount = await DatabaseHelper.GetRecordCountAsync(_connectionString, tableName);
                if (newCount == originalCount + expectedIncrement)
                {
                    _logger.LogInformation("Database record count verified for {TableName}: {NewCount}", tableName, newCount);
                    return true;
                }

                _logger.LogInformation("Database record count not yet updated for {TableName}, retrying... ({Retry}/{MaxRetries})", tableName, i + 1, maxRetries);
                await Task.Delay(delay);
            }
            return false;
        }
    }
}
