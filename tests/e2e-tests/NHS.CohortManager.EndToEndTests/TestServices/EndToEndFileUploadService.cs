namespace NHS.CohortManager.EndToEndTests.TestServices;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NHS.CohortManager.EndToEndTests.Config;
using NHS.CohortManager.EndToEndTests.Helpers;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;



public class EndToEndFileUploadService
{
    private readonly ILogger<EndToEndFileUploadService> _logger;
    private readonly AppSettings _appSettings;
    private readonly BlobStorageHelper _blobStorageHelper;
    private readonly SqlConnectionWithAuthentication _sqlConnectionWithAuthentication;

    public EndToEndFileUploadService(ILogger<EndToEndFileUploadService> logger, AppSettings appSettings, BlobStorageHelper blobStorageHelper)
    {
        _logger = logger;
        _appSettings = appSettings;
        _blobStorageHelper = blobStorageHelper;

        // Read from appsettings.json
        string connectionString = _appSettings.ConnectionStrings.DtOsDatabaseConnectionString;
        string? managedIdentityClientId = _appSettings.ManagedIdentityClientId;
        bool isCloudEnvironment = _appSettings.AzureSettings.IsCloudEnvironment; // Instead of hardcoded AZURE_ENVIRONMENT

        // Pass to SqlConnectionWithAuthentication
        _sqlConnectionWithAuthentication = new SqlConnectionWithAuthentication(connectionString, managedIdentityClientId, isCloudEnvironment);
    }


    public async Task CleanDatabaseAsync(IEnumerable<string> nhsNumbers)
    {
        _logger.LogInformation("Starting database cleanup.");

        try
        {
            foreach (var nhsNumber in nhsNumbers)
            {
                //  parameterized queries to prevent SQL injection
                await DatabaseHelper.ExecuteNonQueryAsync(_sqlConnectionWithAuthentication,
                    "DELETE FROM PARTICIPANT_MANAGEMENT WHERE NHS_Number = @nhsNumber",
                    new SqlParameter("@nhsNumber", nhsNumber));

                await DatabaseHelper.ExecuteNonQueryAsync(_sqlConnectionWithAuthentication,
                    "DELETE FROM PARTICIPANT_DEMOGRAPHIC WHERE NHS_Number = @nhsNumber",
                    new SqlParameter("@nhsNumber", nhsNumber));

                await DatabaseHelper.ExecuteNonQueryAsync(_sqlConnectionWithAuthentication,
                    "DELETE FROM BS_COHORT_DISTRIBUTION WHERE NHS_Number = @nhsNumber",
                    new SqlParameter("@nhsNumber", nhsNumber));

                await DatabaseHelper.ExecuteNonQueryAsync(_sqlConnectionWithAuthentication,
                    "DELETE FROM EXCEPTION_MANAGEMENT WHERE NHS_Number = @nhsNumber",
                    new SqlParameter("@nhsNumber", nhsNumber));
            }

            _logger.LogInformation("Database cleanup completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during database cleanup.");
            throw; // Re-throw the exception to be handled elsewhere
        }
    }

    public List<string> ExtractNhsNumbersFromParquet(string filePath)
    {
        _logger.LogInformation("Extracting NHS numbers from Parquet file: {FilePath}", filePath);

        // Hypothetical helper that parses the Parquet file and returns NHS numbers
        var nhsNumbers = ParquetHelperService.ExtractNhsNumbersFromParquet(filePath);

        _logger.LogInformation("Extracted {Count} NHS numbers from the Parquet file.", nhsNumbers.Count);
        return nhsNumbers;
    }

    public async Task UploadFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogError("File not found at {FilePath}", filePath);
            throw new FileNotFoundException($"File not found at {filePath}");
        }

        int retryCount = 0;
        const int maxRetries = 5;
        TimeSpan delay = TimeSpan.FromSeconds(1);

        while (retryCount < maxRetries)
        {
            try
            {
                _logger.LogInformation("Uploading file {FilePath} to Blob Storage (Attempt {AttemptNumber}).", filePath, retryCount + 1);
                await _blobStorageHelper.UploadFileToBlobStorageAsync(filePath, _appSettings.BlobContainerName);
                _logger.LogInformation("File uploaded successfully.");
                return; // Exit the loop if successful
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {FilePath} to Blob Storage (Attempt {AttemptNumber}).", filePath, retryCount + 1);
                retryCount++;
                await Task.Delay(delay);
                delay = delay * 2; // Exponential backoff
            }
        }

        _logger.LogError("Failed to upload file {FilePath} to Blob Storage after {MaxRetries} retries.", filePath, maxRetries);
        throw new Exception($"Failed to upload file {filePath} to Blob Storage after {maxRetries} retries.");
    }

    public async Task<bool> VerifyRecordCountAsync(string tableName, int originalCount, int expectedIncrement, int retries = 10, int delay = 1000)
    {
        _logger.LogInformation("Verifying record count for table {TableName}.", tableName);

        for (int i = 0; i < retries; i++)
        {
            var newCount = await DatabaseHelper.GetRecordCountAsync(_sqlConnectionWithAuthentication, tableName);
            if (newCount == originalCount + expectedIncrement)
            {
                _logger.LogInformation("Record count verified: Expected = {Expected}, Actual = {Actual}.", originalCount + expectedIncrement, newCount);
                return true;
            }

            _logger.LogWarning("Record count not updated for {TableName}. Retry {Retry}/{MaxRetries}.", tableName, i + 1, retries);
            await Task.Delay(delay);
        }

        _logger.LogError("Failed to verify record count for {TableName} after {MaxRetries} retries.", tableName, retries);
        return false;
    }

    public async Task VerifyNhsNumbersAsync(string firstTableName, List<string> nhsNumbers, string recordType = null, string secondTableName = null)
    {
        if (string.IsNullOrEmpty(secondTableName))
        {
            // Single table validation
            _logger.LogInformation("Validating NHS numbers in table {TableName} with record type {RecordType}.", firstTableName, recordType ?? "null");
            await DatabaseValidationHelper.VerifyNhsNumbersAsync(_sqlConnectionWithAuthentication, firstTableName, nhsNumbers, recordType);
            _logger.LogInformation("Validation of NHS numbers completed successfully.");
        }
        else
        {
            // Two table validation
            _logger.LogInformation("Validating NHS numbers in tables {FirstTableName} and {SecondTableName} with record type {RecordType}.", firstTableName, secondTableName, recordType ?? "null");

            await DatabaseValidationHelper.VerifyNhsNumbersAsync(_sqlConnectionWithAuthentication, firstTableName, nhsNumbers, recordType);
            _logger.LogInformation("Validation of NHS numbers in {FirstTableName} completed successfully.", firstTableName);

            await DatabaseValidationHelper.VerifyNhsNumbersAsync(_sqlConnectionWithAuthentication, secondTableName, nhsNumbers);
            _logger.LogInformation("Validation of NHS numbers in {SecondTableName} completed successfully.", secondTableName);
            _logger.LogInformation("Validation of NHS numbers in both tables completed successfully.");
        }
    }

    public async Task VerifyNhsNumbersCountAsync(string tableName, string nhsNumber, int expectedCount)
    {
        _logger.LogInformation("Validating NHS number count in table {TableName}.", tableName);
        Func<Task> act = async () =>
        {
            var nhsNumberCount = await DatabaseValidationHelper.GetNhsNumberCount(_sqlConnectionWithAuthentication, tableName, nhsNumber, _logger);
            nhsNumberCount.Should().Be(expectedCount);
        };

        await act.Should().NotThrowAfterAsync(TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(5));
        _logger.LogInformation("Validation of NHS number count completed successfully.");
    }


    public async Task VerifyFieldUpdateAsync(string tableName, string nhsNumber, string fieldName, string expectedValue)
    {
        Func<Task> act = async () =>
        {
            var result = await DatabaseValidationHelper.VerifyFieldUpdateAsync(_sqlConnectionWithAuthentication, tableName, nhsNumber, fieldName, expectedValue, _logger);
            result.Should().BeTrue();
        };

        await act.Should().NotThrowAfterAsync(TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(5));

    }


}
