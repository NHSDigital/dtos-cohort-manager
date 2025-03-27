namespace NHS.CohortManager.EndToEndTests.Helpers;

using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;


public static class DatabaseHelper
{
    // Whitelist of allowed table names
    private static readonly HashSet<string> AllowedTables = new HashSet<string>
    {
        "PARTICIPANT_MANAGEMENT",
        "PARTICIPANT_DEMOGRAPHIC",
        "BS_COHORT_DISTRIBUTION",
        "EXCEPTION_MANAGEMENT"
    };

     public static async Task<bool> DoesBlobExistAsync(this BlobStorageHelper helper, string fileName, string containerName)
        {
            try
            {
                // Get the BlobServiceClient from the helper through reflection (since we don't have access to its private field)
                var blobServiceClientField = typeof(BlobStorageHelper).GetField("_blobServiceClient",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var blobServiceClient = (BlobServiceClient)blobServiceClientField.GetValue(helper);

                // Get container and blob client
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(fileName);

                // Check if the blob exists
                var response = await blobClient.ExistsAsync();
                return response.Value;
            }
            catch (Exception ex)
            {
                // Get the logger from the helper through reflection
                var loggerField = typeof(BlobStorageHelper).GetField("_logger",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var logger = (ILogger<BlobStorageHelper>)loggerField.GetValue(helper);

                logger.LogError(ex, $"Error checking if blob '{fileName}' exists in container '{containerName}'");
                return false;
            }
        }


          public static async Task AssertLocalFileMatchesBlobAsync(this BlobStorageHelper helper, string localFilePath, string containerName)
        {
            try
            {
                // Get the BlobServiceClient from the helper through reflection
                var blobServiceClientField = typeof(BlobStorageHelper).GetField("_blobServiceClient",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var blobServiceClient = (BlobServiceClient)blobServiceClientField.GetValue(helper);

                // Get the logger from the helper through reflection
                var loggerField = typeof(BlobStorageHelper).GetField("_logger",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var logger = (ILogger<BlobStorageHelper>)loggerField.GetValue(helper);

                if (!File.Exists(localFilePath))
                {
                    throw new FileNotFoundException($"Local file not found at {localFilePath}");
                }

                var fileName = Path.GetFileName(localFilePath);

                // Get container and blob client
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(fileName);

                // Get blob content
                var response = await blobClient.DownloadAsync();
                string blobContent;
                using (var streamReader = new StreamReader(response.Value.Content))
                {
                    blobContent = await streamReader.ReadToEndAsync();
                }

                // Get local file content
                string localContent = await File.ReadAllTextAsync(localFilePath);

                // Compare contents (ignoring differences in line endings)
                blobContent = NormalizeLineEndings(blobContent);
                localContent = NormalizeLineEndings(localContent);

                blobContent.Should().Be(localContent,
                    $"The content of blob '{fileName}' should match the content of local file '{localFilePath}'");

                logger.LogInformation($"Successfully verified content match for blob '{fileName}'");
            }
            catch (Exception ex)
            {
                // Get the logger from the helper through reflection
                var loggerField = typeof(BlobStorageHelper).GetField("_logger",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var logger = (ILogger<BlobStorageHelper>)loggerField.GetValue(helper);

                logger.LogError(ex, $"Error comparing local file '{localFilePath}' with blob in container '{containerName}'");
                throw;
            }
        }

         private static string NormalizeLineEndings(string text)
        {
            return text.Replace("\r\n", "\n").Replace("\r", "\n");
        }


    public static async Task<int> ExecuteNonQueryAsync(
     SqlConnectionWithAuthentication sqlAuthConnection,
     string query,
     params SqlParameter[] parameters)
    {
        await using var connection = await sqlAuthConnection.GetOpenConnectionAsync();
        await using var command = new SqlCommand(query, connection);

        command.Parameters.AddRange(parameters);

        return await command.ExecuteNonQueryAsync();
    }

    public static async Task<int> GetRecordCountAsync(SqlConnectionWithAuthentication sqlConnectionWithAuthentication, string tableName)
    {
        // Check if the table name is in the whitelist
        if (!AllowedTables.Contains(tableName.ToUpper()))
        {
            throw new ArgumentException($"Table '{tableName}' is not in the list of allowed tables.");
        }

        // Get the open connection (with token if using Managed Identity)
        using var connection = await sqlConnectionWithAuthentication.GetOpenConnectionAsync();

        // Check if the table actually exists in the database
        if (!await TableExistsAsync(connection, tableName))
        {
            throw new ArgumentException($"Table '{tableName}' does not exist in the database.");
        }

        var query = "SELECT COUNT(*) FROM " + tableName;
        using var command = new SqlCommand(query, connection);
        return (int)await command.ExecuteScalarAsync();
    }

    private static async Task<bool> TableExistsAsync(SqlConnection connection, string tableName)
    {
        var query = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName";
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@TableName", tableName);
            return (int)await command.ExecuteScalarAsync() > 0;
        }
    }
}
