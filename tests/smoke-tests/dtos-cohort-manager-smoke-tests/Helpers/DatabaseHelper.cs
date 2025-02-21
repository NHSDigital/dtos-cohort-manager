using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace dtos_cohort_manager_specflow.Helpers;

public static class DatabaseHelper
{
    // Whitelist of allowed table names
    private static readonly HashSet<string> AllowedTables = new HashSet<string>
    {
        "PARTICIPANT_MANAGEMENT",
        "PARTICIPANT_DEMOGRAPHIC",
        "BS_COHORT_DISTRIBUTION",
    };

    public static async Task<int> ExecuteNonQueryAsync(string connectionString, string managedIdentityClientId, string query, params SqlParameter[] parameters)
    {
        var credential = new DefaultAzureCredential(
            new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = managedIdentityClientId
            });

        using var connection = new SqlConnection(connectionString);
        connection.AccessToken = (await credential.GetTokenAsync(new TokenRequestContext(new[] { "https://database.windows.net/.default" })).ConfigureAwait(false)).Token;

        await connection.OpenAsync();
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddRange(parameters); // Add the parameters to the command
            return await command.ExecuteNonQueryAsync();
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

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        var query = "SELECT COUNT(*) FROM " + tableName;
        using var command = new SqlCommand(query, connection);
        return (int)await command.ExecuteScalarAsync();
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
