using Microsoft.VisualStudio.TestTools.UnitTesting;
using Azure.Storage.Blobs;
using System.IO;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Linq;
using Model;
using System.Collections.Generic;
using Tests.Integration.Helpers;

namespace Tests.Integration.EndtoEndTests
{
    [TestClass]
    [TestCategory("Integration")]
    public class E2E_FileUploadAndCreateParticipantTest : BaseIntegrationTest
    {
        private BlobServiceClient _blobServiceClient;
        private string _localFilePath;
        private string _blobContainerName;
        private string _connectionString;
        private AppSettings _config;
        private List<string> _nhsNumbers;

        protected override void LoadConfiguration()
        {
            _config = TestConfig.Get();
            _connectionString = _config.ConnectionStrings.DtOsDatabaseConnectionString;
            _blobServiceClient = new BlobServiceClient(_config.AzureWebJobsStorage);
            _localFilePath = _config.FilePaths.Local;
            _blobContainerName = _config.BlobContainerName;
        }

        protected override void AssertAllConfigurations()
        {
            Assert.IsNotNull(_connectionString, "Database connection string is not set in configuration");
            Assert.IsNotNull(_blobServiceClient, "Blob service connection string is not set in configuration");
            Assert.IsNotNull(_localFilePath, "Local file path is not set in configuration");
            Assert.IsNotNull(_blobContainerName, "Blob container name is not set in configuration");
        }

        protected override async Task AdditionalSetupAsync()
        {
            _nhsNumbers = CsvHelperService.ExtractNhsNumbersFromCsv(_localFilePath);
            await CleanDatabaseAsync();
        }

        private async Task CleanDatabaseAsync()
        {
            var query = "DELETE FROM PARTICIPANT_MANAGEMENT; DELETE FROM PARTICIPANT_DEMOGRAPHIC";
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

            var participantVerified = await VerifyRecordCountAsync("PARTICIPANT_MANAGEMENT", originalParticipantCount, expectedIncrement: _nhsNumbers.Count);
            var demographicVerified = await VerifyRecordCountAsync("PARTICIPANT_DEMOGRAPHIC", originalDemographicCount, expectedIncrement: _nhsNumbers.Count);

            Assert.IsTrue(participantVerified, $"The expected number of participant records ({originalParticipantCount + _nhsNumbers.Count}) was not found in the database.");
            Assert.IsTrue(demographicVerified, $"The expected number of demographic records ({originalDemographicCount + _nhsNumbers.Count}) was not found in the database.");

            Logger.LogInformation("Starting additional data integrity check for NHS Numbers.");

            // Additional check: Verify that specific NHS numbers from the CSV are present in the tables
            await DatabaseValidationHelper.VerifyNhsNumbersAsync(_connectionString, "PARTICIPANT_MANAGEMENT", _nhsNumbers, Logger);
            await DatabaseValidationHelper.VerifyNhsNumbersAsync(_connectionString, "PARTICIPANT_DEMOGRAPHIC", _nhsNumbers, Logger);

            Logger.LogInformation("Completed data integrity check.");
        }

        private async Task UploadFileToBlobStorageAsync()
        {
            Assert.IsTrue(File.Exists(_localFilePath), $"File not found at {_localFilePath}");
            Logger.LogInformation("Uploading file {FilePath} to blob storage", _localFilePath);

            var blobContainerClient = _blobServiceClient.GetBlobContainerClient(_blobContainerName);
            await blobContainerClient.CreateIfNotExistsAsync();

            var blobClient = blobContainerClient.GetBlobClient(Path.GetFileName(_localFilePath));
            await blobClient.UploadAsync(File.OpenRead(_localFilePath), true);

            Logger.LogInformation("File uploaded successfully");
        }

        private async Task<bool> VerifyRecordCountAsync(string tableName, int originalCount, int expectedIncrement, int maxRetries = 10, int delay = 1000)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                var newCount = await DatabaseHelper.GetRecordCountAsync(_connectionString, tableName);
                if (newCount == originalCount + expectedIncrement)
                {
                    Logger.LogInformation("Database record count verified for {TableName}: {NewCount}", tableName, newCount);
                    return true;
                }

                Logger.LogInformation("Database record count not yet updated for {TableName}, retrying... ({Retry}/{MaxRetries})", tableName, i + 1, maxRetries);
                await Task.Delay(delay);
            }
            return false;
        }
    }
}
