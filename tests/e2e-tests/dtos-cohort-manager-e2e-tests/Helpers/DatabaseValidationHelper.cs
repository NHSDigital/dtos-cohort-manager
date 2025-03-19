using ChoETL;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Azure.Identity;
using Azure.Core;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Linq;

namespace dtos_cohort_manager_e2e_tests.Helpers;

public static class DatabaseValidationHelper
{
    private static readonly HashSet<string> AllowedTables = new HashSet<string>
    {
        "BS_COHORT_DISTRIBUTION",
        "PARTICIPANT_MANAGEMENT",
        "PARTICIPANT_DEMOGRAPHIC",
        "EXCEPTION_MANAGEMENT"
    };

    private static readonly HashSet<string> AllowedFields =
    [
        "NHS_NUMBER",
        "GIVEN_NAME",
        "RULE_ID",
        "RULE_DESCRIPTION"
        // Add other allowed fields here
    ];

    private static void ValidateTableName(string tableName)
    {
        if (!AllowedTables.Contains(tableName.ToUpper()))
        {
            throw new ArgumentException($"Table '{tableName}' is not in the list of allowed tables.");
        }
    }

    private static void ValidateFieldName(string fieldName)
    {
        if (!AllowedFields.Contains(fieldName.ToUpper()))
        {
            throw new ArgumentException($"Field '{fieldName}' is not in the list of allowed fields.");
        }
    }

    public static async Task VerifyNhsNumbersAsync(
    SqlConnectionWithAuthentication sqlConnectionWithAuthentication,
    string tableName,
    List<string> nhsNumbers,
    string recordType = null)
    {
        ValidateTableName(tableName);
        using (var connection = await sqlConnectionWithAuthentication.GetOpenConnectionAsync())
        {
            foreach (var nhsNumber in nhsNumbers)
            {
                var isVerified = await VerifyNhsNumberAsync(connection, tableName, nhsNumber, recordType);
                if (!isVerified)
                {
                    string errorMessage = $"Verification failed: NHS number {nhsNumber} not found in {tableName} table";
                    if (!string.IsNullOrEmpty(recordType))
                    {
                        errorMessage += $" with record type {recordType}";
                    }
                    Assert.Fail(errorMessage);
                }
            }
        }
    }
    public static async Task<bool> VerifyFieldUpdateAsync(SqlConnectionWithAuthentication sqlConnectionWithAuthentication, string tableName, string nhsNumber, string fieldName, string expectedValue, ILogger logger)
    {
        List<string> fieldValues = new List<string>();
        ValidateTableName(tableName);
        ValidateFieldName(fieldName);


        using (var connection = await sqlConnectionWithAuthentication.GetOpenConnectionAsync())
        {
            var query = $"SELECT {fieldName} FROM {tableName} WHERE [NHS_NUMBER] = @NhsNumber";


            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@NhsNumber", nhsNumber);

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var value = reader.IsDBNull(0) ? null : reader.GetValue(0);
                        if (value != null)
                        {

                            if (value is int intValue)
                                fieldValues.Add(intValue.ToString());
                            else
                                fieldValues.Add(value.ToString()!);
                        }
                    }
                }

                if (fieldValues.Count == 0)
                {
                    logger.LogError($"Field {fieldName} is null for NHS number {nhsNumber} in {tableName} table.");
                    return false;
                }

                if (!fieldValues.Contains(expectedValue))
                {
                    logger.LogError($"Field {fieldName} for NHS number {nhsNumber} does not match the expected value. Expected: {expectedValue}, Actual: {fieldValues.FirstOrDefault()}");
                    return false;
                }

                logger.LogInformation($"Field {fieldName} for NHS number {nhsNumber} successfully updated to {expectedValue}.");
                return true;
            }
        }
    }
    public static async Task<bool> VerifyRecordCountAsync(string connectionString, string tableName, int expectedCount, ILogger logger, int maxRetries = 10, int delay = 1000)
    {
        ValidateTableName(tableName);

        for (int i = 0; i < maxRetries; i++)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var query = $"SELECT COUNT(*) FROM {tableName}";
                using (var command = new SqlCommand(query, connection))
                {
                    var count = (int)await command.ExecuteScalarAsync();
                    if (count == expectedCount)
                    {
                        logger.LogInformation($"Database record count verified for {tableName}: {count}");
                        return true;
                    }
                    logger.LogInformation($"Database record count not yet updated for {tableName}, retrying... ({i + 1}/{maxRetries})");
                    await Task.Delay(delay);
                }
            }
        }
        logger.LogError($"Failed to verify record count for {tableName} after {maxRetries} retries.");
        return false;
    }

    private static async Task<bool> VerifyNhsNumberAsync(
    SqlConnection connection,
    string tableName,
    string nhsNumber,
    string recordType = null)
    {
        int retryCount = 0;
        const int maxRetries = 8;
        TimeSpan delay = TimeSpan.FromSeconds(5); // Initial delay

        while (retryCount < maxRetries)
        {
            try
            {
                string sql = $"SELECT 1 FROM {tableName} WHERE NHS_Number = @nhsNumber";
                if (!string.IsNullOrEmpty(recordType))
                {
                    sql += " AND RECORD_TYPE = @recordType";
                }
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@nhsNumber", nhsNumber);
                if (!string.IsNullOrEmpty(recordType))
                {
                    command.Parameters.AddWithValue("@recordType", recordType);
                }
                var result = await command.ExecuteScalarAsync();

                if (result != null)
                {
                    return true;
                }


                await Task.Delay(delay);
                delay *= 2; // Double the delay for the next retry attempt
                retryCount++;
            }
            catch (Exception ex)
            {
                // Handle the exception and decide whether to retry
                if (retryCount < maxRetries - 1)
                {
                    // Wait for the delay before retrying
                    await Task.Delay(delay);
                    delay *= 2; // Double the delay for the next retry attempt
                    retryCount++;
                }
                else
                {

                    throw new Exception($"Failed to verify NHS number after {maxRetries} attempts.", ex);
                }
            }
        }
        return false;
    }



    public static async Task<int> GetNhsNumberCount(SqlConnectionWithAuthentication sqlConnectionWithAuthentication, string tableName, string nhsNumber, ILogger logger)
    {
        int nhsNumberCount = 0;

        // Get the open connection (with token if using Managed Identity)
        using (var connection = await sqlConnectionWithAuthentication.GetOpenConnectionAsync())
        {
            var query = $"SELECT COUNT(*) FROM {tableName} WHERE [NHS_NUMBER] = @NhsNumber";

            // Create SQL command and add parameter for NHS Number
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@NhsNumber", nhsNumber);

                // Execute the query and get the count of NHS numbers
                nhsNumberCount = (int)(await command.ExecuteScalarAsync() ?? 0);
            }
        }

        return nhsNumberCount;
    }



}
