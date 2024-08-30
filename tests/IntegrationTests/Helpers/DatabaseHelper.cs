using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

public static class DatabaseHelper
{
    // Whitelist of allowed table names
    private static readonly HashSet<string> AllowedTables = new HashSet<string>
    {
        "PARTICIPANT_MANAGEMENT",
        "PARTICIPANT_DEMOGRAPHIC",
        "BS_COHORT_DISTRIBUTION",
    };

    public static async Task<int> ExecuteNonQueryAsync(string connectionString, string query)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            using (var command = new SqlCommand(query, connection))
            {
                return await command.ExecuteNonQueryAsync();
            }
        }
    }

    public static async Task<int> GetRecordCountAsync(string connectionString, string tableName)
    {
        // Check if the table name is in the whitelist
        if (!AllowedTables.Contains(tableName.ToUpper()))
        {
            throw new ArgumentException($"Table '{tableName}' is not in the list of allowed tables.");
        }

        // Check if the table actually exists in the database
        if (!await TableExistsAsync(connectionString, tableName))
        {
            throw new ArgumentException($"Table '{tableName}' does not exist in the database.");
        }

        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            var query = "SELECT COUNT(*) FROM " + tableName;
            using (var command = new SqlCommand(query, connection))
            {
                return (int)await command.ExecuteScalarAsync();
            }
        }
    }

    private static async Task<bool> TableExistsAsync(string connectionString, string tableName)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            var query = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName";
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@TableName", tableName);
                return (int)await command.ExecuteScalarAsync() > 0;
            }
        }
    }
}
