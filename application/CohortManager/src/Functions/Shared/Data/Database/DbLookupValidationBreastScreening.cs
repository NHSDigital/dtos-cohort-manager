namespace Data.Database;

using Model;
using System.Data;
using System.Data.SqlClient;

public class DbLookupValidationBreastScreening {
    private IDbConnection _connection;
    public DbLookupValidationBreastScreening(IDbConnection connection) {
        _connection = connection;
    }
    public bool ValidatePrimaryCareProvider(string primaryCareProvider) {
        string sql = $"SELECT GP_PRACTICE_CODE FROM [dbo].[BS_SELECT_GP_PRACTICE_LKP] WHERE GP_PRACTICE_CODE = @primaryCareProvider";
        using (_connection)
        {
            _connection.Open();
            using (SqlCommand command = new SqlCommand(sql, (SqlConnection) _connection))
            {
                command.Parameters.AddWithValue("@primaryCareProvider", primaryCareProvider);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    return reader.Read();
                }
            }
        }
    }
}