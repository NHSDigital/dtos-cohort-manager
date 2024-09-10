namespace Data.Database;

using Model;
using System.Data;
using System.Data.SqlClient;

/// <summary>
/// Various validation methods for use in the breast screening lookup/ cohort rules
/// </summary>
public static class DbLookupValidationBreastScreening {
    // private static SqlConnection _connection {get; set;} = new SqlConnection(Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString"));

    /// <summary>
    /// Used in rule 36 in the lookup rules, and rule 54 in the cohort rules.
    /// Validates the participants primary care provider (GP practice code)
    /// </summary>
    /// <param name="primaryCareProvider">The participant's primary care provider.</param>
    /// <returns>bool, whether or not the GP practice code exists in the DB.<returns>
    public static bool ValidatePrimaryCareProvider(string primaryCareProvider)
    {
        var connection = new SqlConnection(Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString"));
        string sql = $"SELECT GP_PRACTICE_CODE FROM [dbo].[BS_SELECT_GP_PRACTICE_LKP] WHERE GP_PRACTICE_CODE = @primaryCareProvider";
        using (connection)
        {
            connection.Open();
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@primaryCareProvider", primaryCareProvider);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    var isValid = reader.Read();
                    System.Console.WriteLine("primary care provider is valid: " + isValid);
                    return isValid;
                }
            }
        }
    }

    /// <summary>
    /// Used in rule 54 in the cohort rules. Validates the participants outcode (1st part of the postcode)
    /// </summary>
    /// <param name="postcode">The participant's postcode.</param>
    /// <returns>bool, whether or not the outcode code exists in the DB.<returns>
    public static bool ValidateOutcode(string postcode)
    {
        var connection = new SqlConnection(Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString"));
        var outcode = postcode.Substring(0, postcode.IndexOf(" "));
        string sql = $"SELECT OUTCODE FROM [dbo].[BS_SELECT_OUTCODE_MAPPING_LKP] WHERE OUTCODE = @outcode";

        using (connection)
        {
            connection.Open();
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@outcode", outcode);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    return reader.Read();
                }
            }
        }
    }
}
