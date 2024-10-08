using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using ChoETL;
using System.Linq;

public static class DatabaseValidationHelper
{
    private static readonly HashSet<string> AllowedTables = new HashSet<string>
    {
        "BS_COHORT_DISTRIBUTION",
        "PARTICIPANT_MANAGEMENT",
        "PARTICIPANT_DEMOGRAPHIC",
    };

    private static readonly HashSet<string> AllowedFields = new HashSet<string>
    {
        "NHS_NUMBER",
        // Add other allowed fields here
    };

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

    public static async Task VerifyNhsNumbersAsync(string connectionString, string tableName, List<string> nhsNumbers, ILogger logger)
    {
        ValidateTableName(tableName);

        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            foreach (var nhsNumber in nhsNumbers)
            {
                var isVerified = await VerifyNhsNumberAsync(connection, tableName, nhsNumber, logger);
                if (!isVerified)
                {
                    logger.LogError($"Verification failed: NHS number {nhsNumber} not found in {tableName} table.");
                    Assert.Fail($"NHS number {nhsNumber} not found in {tableName} table.");
                }
            }
        }
    }

    public static async Task<bool> VerifyFieldUpdateAsync(string connectionString, string tableName, string nhsNumber, string fieldName, string expectedValue, ILogger logger)
    {
        ValidateTableName(tableName);
        ValidateFieldName(fieldName);

        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            var query = $"SELECT {fieldName} FROM {tableName} WHERE [NHS_NUMBER] = @NhsNumber";
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@NhsNumber", nhsNumber);
                var result = await command.ExecuteScalarAsync();

                if (result == null)
                {
                    logger.LogError($"Field {fieldName} is null for NHS number {nhsNumber} in {tableName} table.");
                    return false;
                }

                var actualValue = result.ToString();

                if (actualValue != expectedValue)
                {
                    logger.LogError($"Field {fieldName} for NHS number {nhsNumber} does not match the expected value. Expected: {expectedValue}, Actual: {actualValue}");
                    return false;
                }

                logger.LogInformation($"Field {fieldName} for NHS number {nhsNumber} successfully updated to {actualValue}.");
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

    private static async Task<bool> VerifyNhsNumberAsync(SqlConnection connection, string tableName, string nhsNumber, ILogger logger)
    {
        ValidateTableName(tableName);

        var query = $"SELECT COUNT(*) FROM {tableName} WHERE [NHS_NUMBER] = @NhsNumber";
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@NhsNumber", nhsNumber);
            var count = (int)await command.ExecuteScalarAsync();
            if (count == 1)
            {
                logger.LogInformation($"NHS number successfully verified in {tableName} table.");
                return true;
            }
            else
            {
                logger.LogError($"NHS number {nhsNumber} not found or found multiple times in {tableName} table.");
                return false;
            }
        }
    }

    public static async Task<bool> VerifyFieldsMatchCsvAsync(string connectionString, string tableName, string nhsNumber, string csvFilePath, ILogger logger)
    {
        ValidateTableName(tableName);

        var csvRecords = CsvHelperService.ReadCsv(csvFilePath);
        var expectedRecord = csvRecords.FirstOrDefault(record => record["NHS Number"] == nhsNumber);

        if (expectedRecord == null)
        {
            logger.LogError($"NHS number {nhsNumber} not found in the CSV file.");
            return false;
        }

        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            var query = $"SELECT * FROM {tableName} WHERE [NHS_NUMBER] = @NhsNumber";
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@NhsNumber", nhsNumber);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (!reader.HasRows)
                    {
                        logger.LogError($"No record found in {tableName} for NHS number {nhsNumber}.");
                        return false;
                    }

                    while (await reader.ReadAsync())
                    {
                        foreach (var key in expectedRecord.Keys)
                        {
                            var expectedValue = expectedRecord[key];
                            var actualValue = reader[key]?.ToString();

                            if (expectedValue != actualValue)
                            {
                                logger.LogError($"Mismatch in {key} for NHS number {nhsNumber}: expected '{expectedValue}', found '{actualValue}'.");
                                return false;
                            }
                        }
                    }
                }
            }
        }

        return true;
    }

    public static async Task<bool> VerifyFieldsMatchParquetAsync(string connectionString, string tableName, string nhsNumber, string parquetFilePath, ILogger logger)
    {
        ValidateTableName(tableName);

        var parquetRecords = ReadParquetFile(parquetFilePath);
        var expectedRecord = parquetRecords.FirstOrDefault(record => record["NHS Number"]?.ToString() == nhsNumber);

        if (expectedRecord == null)
        {
            logger.LogError($"NHS number {nhsNumber} not found in the Parquet file.");
            return false;
        }

        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            var query = $"SELECT * FROM {tableName} WHERE [NHS_NUMBER] = @NhsNumber";
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@NhsNumber", nhsNumber);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (!reader.HasRows)
                    {
                        logger.LogError($"No record found in {tableName} for NHS number {nhsNumber}.");
                        return false;
                    }

                    while (await reader.ReadAsync())
                    {
                        foreach (var key in expectedRecord.Keys)
                        {
                            var expectedValue = expectedRecord[key]?.ToString();
                            var actualValue = reader[key]?.ToString();

                            if (expectedValue != actualValue)
                            {
                                logger.LogError($"Mismatch in {key} for NHS number {nhsNumber}: expected '{expectedValue}', found '{actualValue}'.");
                                return false;
                            }
                        }
                    }
                }
            }
        }

        return true;
    }

    private static List<IDictionary<string, object>> ReadParquetFile(string parquetFilePath)
    {
        var records = new List<IDictionary<string, object>>();
        using (var reader = new ChoParquetReader(parquetFilePath))
        {
            foreach (var record in reader)
            {
                records.Add((IDictionary<string, object>)record);
            }
        }
        return records;
    }
}

