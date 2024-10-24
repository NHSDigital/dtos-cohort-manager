namespace Data.Database;

using Microsoft.Identity.Client;
using Model;
using System.Data;
using Microsoft.Data.SqlClient;

/// <summary>
/// Various validation methods for use in the breast screening lookup/ cohort rules
/// </summary>
public class DbLookupValidationBreastScreening : IDbLookupValidationBreastScreening
{
    private IDbConnection _connection;
    private string _connectionString;

    public DbLookupValidationBreastScreening(IDbConnection IdbConnection)
    {
        _connection = IdbConnection;
        _connectionString = Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString") ?? string.Empty;
    }


    /// <summary>
    /// Used in rule 36 in the lookup rules, and rule 54 in the cohort rules.
    /// Validates the participants primary care provider (GP practice code)
    /// </summary>
    /// <param name="primaryCareProvider">The participant's primary care provider.</param>
    /// <returns>bool, whether or not the GP practice code exists in the DB.<returns>
    public bool ValidatePrimaryCareProvider(string primaryCareProvider)
    {
        using (_connection = new SqlConnection(_connectionString))
        {
            _connection.Open();
            using (IDbCommand command = _connection.CreateCommand())
            {
                command.CommandText = $"SELECT GP_PRACTICE_CODE FROM [dbo].[BS_SELECT_GP_PRACTICE_LKP] WHERE GP_PRACTICE_CODE = @primaryCareProvider";
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@primaryCareProvider";
                parameter.Value = primaryCareProvider ?? string.Empty;
                command.Parameters.Add(parameter);

                using (IDataReader reader = command.ExecuteReader())
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
        if (string.IsNullOrWhiteSpace(postcode)) return false;
        var outcode = postcode.Substring(0, postcode.IndexOf(" "));

        using (_connection = new SqlConnection(_connectionString))
        {
            _connection.Open();
            using (IDbCommand command = _connection.CreateCommand())
            {
                command.CommandText = $"SELECT OUTCODE FROM [dbo].[BS_SELECT_OUTCODE_MAPPING_LKP] WHERE OUTCODE = @outcode";
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@outcode";
                parameter.Value = outcode ?? string.Empty;
                command.Parameters.Add(parameter);

                using (IDataReader reader = command.ExecuteReader())
                {
                    return reader.Read();
                }
            }
        }
    }

    /// <summary>
    /// Retrieves the participant's BSO code (using the participant's outcode)
    /// </summary>
    /// <param name="postcode">The participant's postcode.</param>
    /// <returns>string, BSO code<returns>
    public string GetBSOCode(string postcode)
    {
        return "ELD";
    }

    /// <summary>
    /// Used in rule 00 in the lookup rules. Validates the participants preferred language code.
    /// </summary>
    /// <param name="languageCode">The participant's preferred language code.</param>
    /// <returns>bool, whether or not the language code exists in the DB.<returns>
    public bool ValidateLanguageCode(string languageCode)
    {

        using (_connection = new SqlConnection(_connectionString))
        {
            _connection.Open();
            using (IDbCommand command = _connection.CreateCommand())
            {
                command.CommandText = $"SELECT LANGUAGE_CODE FROM [dbo].[LANGUAGE_CODES] WHERE LANGUAGE_CODE = @languageCode";
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@languageCode";
                parameter.Value = languageCode ?? string.Empty;
                command.Parameters.Add(parameter);

                using (IDataReader reader = command.ExecuteReader())
                {
                    return reader.Read();
                }
            }
        }
    }

    /// Used in rule 58 of the lookup rules.
    /// Validates that the current posting exists, and that it is in the cohort and in use.
    /// </summary>
    /// <param name="currentPosting">The participant's current posting (area code).</param>
    /// <returns>bool, whether or not the current posting is valid.<returns>
    public bool ValidateCurrentPosting(string currentPosting)
    {
        using (_connection = new SqlConnection(_connectionString))
        {
            _connection.Open();
            using (IDbCommand command = _connection.CreateCommand())
            {
                command.CommandText = $"SELECT CASE WHEN IN_USE = 'Y' AND INCLUDED_IN_COHORT = 'Y' THEN 1 ELSE 0 END AS result FROM [dbo].[CURRENT_POSTING_LKP] WHERE POSTING = @currentPosting";
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@currentPosting";
                parameter.Value = currentPosting ?? string.Empty;
                command.Parameters.Add(parameter);

                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        return reader.GetInt32(0) == 1;
                    }
                    return false;
                }
            }
        }
    }
}
