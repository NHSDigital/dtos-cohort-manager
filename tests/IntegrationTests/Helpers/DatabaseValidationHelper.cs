using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

public static class DatabaseValidationHelper
{
    public static async Task VerifyNhsNumbersAsync(string connectionString, string tableName, List<string> nhsNumbers, ILogger logger)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            foreach (var nhsNumber in nhsNumbers)
            {
                await VerifyNhsNumberAsync(connection, tableName, nhsNumber, logger);
            }
        }
    }

    private static async Task VerifyNhsNumberAsync(SqlConnection connection, string tableName, string nhsNumber, ILogger logger)
    {
        var query = $"SELECT COUNT(*) FROM {tableName} WHERE [NHS_NUMBER] = @NhsNumber";
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@NhsNumber", nhsNumber);
            var count = (int)await command.ExecuteScalarAsync();
            if (count != 1)
            {
                logger.LogWarning($"Expected count of NHS number {nhsNumber} not found in {tableName} table.");
            }
        }
    }
}
