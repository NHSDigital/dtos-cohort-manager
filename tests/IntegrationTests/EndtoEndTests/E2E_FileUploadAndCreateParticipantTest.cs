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
        private List<string> _nhsNumbers;

        protected override void LoadConfiguration()
        {
            _connectionString = TestConfig.Get("ConnectionString:DtOsDatabaseConnectionString");
            _blobServiceClient = new BlobServiceClient(TestConfig.Get("AzureWebJobsStorage"));
            _localFilePath = TestConfig.Get("FilePaths:Local");
            _blobContainerName = TestConfig.Get("BlobContainerName");
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
            ExtractExpectedDataFromCsv();
            await CleanDatabaseAsync();
        }

        private void ExtractExpectedDataFromCsv()
        {
            var lines = File.ReadAllLines(_localFilePath);
            // Skips first line which contains the header of the CSV so we don't count it.
            _nhsNumbers = lines.Skip(1).Select(line => ParseCsvLine(line)).ToList();
        }

        private string ParseCsvLine(string line)
        {
            var columns = line.Split(',');
            return columns[3]; // returns NHS Number column from test data csv
        }

        private async Task CleanDatabaseAsync()
        {
            var query = "DELETE FROM dbo.PARTICIPANT_MANAGEMENT; DELETE FROM dbo.PARTICIPANT_DEMOGRAPHIC";
            await DatabaseHelper.ExecuteNonQueryAsync(_connectionString, query);
            Logger.LogInformation("Database cleanup completed.");
        }

        [TestMethod]
        public async Task EndToEnd_FileUploadAndCreateParticipantFlow()
        {
            Logger.LogInformation("Starting end-to-end happy path test");

            var originalParticipantCount = await DatabaseHelper.GetRecordCountAsync(_connectionString, "dbo.PARTICIPANT_MANAGEMENT");
            var originalDemographicCount = await DatabaseHelper.GetRecordCountAsync(_connectionString, "dbo.PARTICIPANT_DEMOGRAPHIC");

            await UploadFileToBlobStorageAsync();

            var participantVerified = await VerifyRecordCountAsync("dbo.PARTICIPANT_MANAGEMENT", originalParticipantCount, expectedIncrement: _nhsNumbers.Count);
            var demographicVerified = await VerifyRecordCountAsync("dbo.PARTICIPANT_DEMOGRAPHIC", originalDemographicCount, expectedIncrement: _nhsNumbers.Count);

            Assert.IsTrue(participantVerified, $"The expected number of participant records ({originalParticipantCount + _nhsNumbers.Count}) was not found in the database.");
            Assert.IsTrue(demographicVerified, $"The expected number of demographic records ({originalDemographicCount + _nhsNumbers.Count}) was not found in the database.");

            // Additional check: Verify that specific NHS numbers from the CSV are present in the tables
            await VerifyDataIntegrityAsync();
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

        private async Task VerifyDataIntegrityAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Verify the first, last, and 3 additional NHS numbers to check data integrity
                var nhsNumbersToCheck = new List<string> { _nhsNumbers.First(), _nhsNumbers.Last() }; // Check first and last NHS Number
                nhsNumbersToCheck.AddRange(_nhsNumbers.Skip(1).Take(3)); // Check 3 additional NHS numbers from the list

                Logger.LogInformation("Starting data integrity verification for NHS Numbers.");

                foreach (var nhsNumber in nhsNumbersToCheck)
                {
                    await VerifyNhsNumberAsync(connection, "dbo.PARTICIPANT_MANAGEMENT", nhsNumber);
                    await VerifyNhsNumberAsync(connection, "dbo.PARTICIPANT_DEMOGRAPHIC", nhsNumber);

                    Logger.LogInformation("Data Integrity verification completed.");
                }
            }
        }

        private async Task VerifyNhsNumberAsync(SqlConnection connection, string tableName, string nhsNumber)
        {
            var query = $"SELECT COUNT(*) FROM {tableName} WHERE [NHS_NUMBER] = @NhsNumber";
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@NhsNumber", nhsNumber);

                var count = (int)await command.ExecuteScalarAsync();
                Assert.AreEqual(1, count, $"Expected count of NHS numbers not found in {tableName} table.");
            }
        }
    }
}
