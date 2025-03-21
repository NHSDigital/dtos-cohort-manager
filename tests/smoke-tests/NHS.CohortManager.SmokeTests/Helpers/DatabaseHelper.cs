using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace NHS.CohortManager.SmokeTests.Helpers;

public static class DatabaseHelper
{
    // Whitelist of allowed table names
    private static readonly HashSet<string> AllowedTables = new HashSet<string>
    {
        "PARTICIPANT_MANAGEMENT",
        "PARTICIPANT_DEMOGRAPHIC",
        "BS_COHORT_DISTRIBUTION",
    };

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
