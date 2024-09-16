namespace Data.Database;

using Model;
using System.Data;
using System.Data.SqlClient;

/// <summary>
/// Various validation methods for use in the breast screening lookup/ cohort rules
/// </summary>
public class DbLookupValidationBreastScreening : IDbLookupValidationBreastScreening
{
    private SqlConnection _connection = new SqlConnection(Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString"));

    /// <summary>
    /// Used in rule 36 in the lookup rules, and rule 54 in the cohort rules.
    /// Validates the participants primary care provider (GP practice code)
    /// </summary>
    /// <param name="primaryCareProvider">The participant's primary care provider.</param>
    /// <returns>bool, whether or not the GP practice code exists in the DB.<returns>
    public bool ValidatePrimaryCareProvider(string primaryCareProvider)
    {
        string sql = $"SELECT GP_PRACTICE_CODE FROM [dbo].[BS_SELECT_GP_PRACTICE_LKP] WHERE GP_PRACTICE_CODE = @primaryCareProvider";
        using (_connection)
        {
            _connection.Open();
            using (SqlCommand command = new SqlCommand(sql, _connection))
            {
                command.Parameters.AddWithValue("@primaryCareProvider", primaryCareProvider);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    return reader.Read();
                }
            }
        }
    }

    /// <summary>
    /// Used in rule 54 in the cohort rules. Validates the participants outcode (1st part of the postcode)
    /// </summary>
    /// <param name="postcode">The participant's postcode.</param>
    /// <returns>bool, whether or not the outcode code exists in the DB.<returns>
    public bool ValidateOutcode(string postcode)
    {
        var outcode = postcode.Substring(0, postcode.IndexOf(" "));
        string sql = $"SELECT OUTCODE FROM [dbo].[BS_SELECT_OUTCODE_MAPPING_LKP] WHERE OUTCODE = @outcode";

        using (_connection)
        {
            _connection.Open();
            using (SqlCommand command = new SqlCommand(sql, _connection))
            {
                command.Parameters.AddWithValue("@outcode", outcode);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    return reader.Read();
                }
            }
        }
    }

    /// <summary>
    /// Used in rule 58 of the lookup rules.
    /// Validates that the current posting exists, and that it is in the cohort and in use.
    /// </summary>
    /// <param name="currentPosting">The participant's current posting (area code).</param>
    /// <returns>bool, whether or not the current posting is valid.<returns>
    public bool ValidateCurrentPosting(string currentPosting)
    {
        string sql = $"SELECT IN_USE, INCLUDED_IN_COHORT FROM [dbo].[CURRENT_POSTING_LKP] WHERE POSTING = @currentPosting";

        using (_connection)
        {
            _connection.Open();
            using (SqlCommand command = new SqlCommand(sql, _connection))
            {
                command.Parameters.AddWithValue("@currentPosting", currentPosting);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while(reader.Read())
                    {
                        return reader["IN_USE"].ToString().Equals("Y") && reader["INCLUDED_IN_COHORT"].ToString().Equals("Y");
                    }
                    return false;
                }
            }
        }
    }

}
