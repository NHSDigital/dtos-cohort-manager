using System.Data.SqlClient;
using System.Threading.Tasks;

public static class DatabaseHelper
{
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
        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            var query = $"SELECT COUNT(*) FROM {tableName}";
            using (var command = new SqlCommand(query, connection))
            {
                return (int)await command.ExecuteScalarAsync();
            }
        }
    }
}
