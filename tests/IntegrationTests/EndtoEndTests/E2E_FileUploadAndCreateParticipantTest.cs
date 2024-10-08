using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Tests.Integration.Helpers;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Tests.Integration.EndtoEndTests
{
    [TestClass]
    [TestCategory("Integration")]
    public class E2E_FileUploadAndCreateParticipantTest : BaseIntegrationTest
    {
        private BlobStorageHelper _blobStorageHelper;
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
            _blobStorageHelper = ServiceProvider.GetService<BlobStorageHelper>();
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
            Logger.LogInformation("Database cleanup completed.");
        }

        [TestMethod]
        public async Task EndToEnd_FileUploadAndCreateParticipantTest()
        {
            Logger.LogInformation("Starting end-to-end happy path test");

            var originalParticipantCount = await DatabaseHelper.GetRecordCountAsync(_connectionString, "PARTICIPANT_MANAGEMENT");
            var originalDemographicCount = await DatabaseHelper.GetRecordCountAsync(_connectionString, "PARTICIPANT_DEMOGRAPHIC");

            await UploadFileToBlobStorageAsync();

            var participantVerified = await DatabaseValidationHelper.VerifyRecordCountAsync(_connectionString, "PARTICIPANT_MANAGEMENT", originalParticipantCount + _nhsNumbers.Count, Logger);
            var demographicVerified = await DatabaseValidationHelper.VerifyRecordCountAsync(_connectionString, "PARTICIPANT_DEMOGRAPHIC", originalDemographicCount + _nhsNumbers.Count, Logger);

            Assert.IsTrue(participantVerified, $"The expected number of participant records ({originalParticipantCount + _nhsNumbers.Count}) was not found in the database.");
            Assert.IsTrue(demographicVerified, $"The expected number of demographic records ({originalDemographicCount + _nhsNumbers.Count}) was not found in the database.");

            Logger.LogInformation("Starting additional data integrity check for NHS Numbers.");

            await DatabaseValidationHelper.VerifyNhsNumbersAsync(_connectionString, "PARTICIPANT_MANAGEMENT", _nhsNumbers, Logger);
            await DatabaseValidationHelper.VerifyNhsNumbersAsync(_connectionString, "PARTICIPANT_DEMOGRAPHIC", _nhsNumbers, Logger);

            Logger.LogInformation("Completed data integrity check.");
        }

        private async Task UploadFileToBlobStorageAsync()
        {
            Assert.IsTrue(System.IO.File.Exists(_localFilePath), $"File not found at {_localFilePath}");
            Logger.LogInformation("Uploading file {FilePath} to blob storage", _localFilePath);

            await _blobStorageHelper.UploadFileToBlobStorageAsync(_localFilePath, _blobContainerName);

            Logger.LogInformation("File uploaded successfully");
        }
    }
}
