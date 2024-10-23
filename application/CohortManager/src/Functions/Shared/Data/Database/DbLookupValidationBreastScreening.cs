namespace Data.Database;

using Microsoft.Identity.Client;
using Model;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

/// <summary>
/// Various validation methods for use in the breast screening lookup/ cohort rules
/// </summary>
public class DbLookupValidationBreastScreening : IDbLookupValidationBreastScreening
{
    private IDbConnection _connection;
    private string _connectionString;

    private readonly ILogger<DbLookupValidationBreastScreening> _logger;

    private readonly string[] allPossiblePostingCategories = ["ENGLAND", "IOM", "DMS"];

    public DbLookupValidationBreastScreening(IDbConnection IdbConnection, ILogger<DbLookupValidationBreastScreening> logger)
    {
        _connection = IdbConnection;
        _connectionString = Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString") ?? string.Empty;
        _logger = logger;
    }


    /// <summary>
    /// Used in rule 36 in the lookup rules, and rule 54 in the cohort rules.
    /// Validates the participants primary care provider (GP practice code)
    /// </summary>
    /// <param name="primaryCareProvider">The participant's primary care provider.</param>
    /// <returns>bool, whether or not the GP practice code exists in the DB.<returns>
    public bool PrimaryCareProviderExists(string primaryCareProvider)
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
        var outcode = postcode.Substring(0, postcode.IndexOf(" "));

        using (_connection = new SqlConnection(_connectionString))
        {
            _connection.Open();
            using (IDbCommand command = _connection.CreateCommand())
            {
                command.CommandText = $"SELECT OUTCODE FROM [dbo].[BS_SELECT_OUTCODE_MAPPING_LKP] WHERE OUTCODE = @outcode";
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@primaryCareProvider";
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
    public bool ValidateCurrentPosting(string currentPosting, string primaryCareProvider)
    {

        using (_connection = new SqlConnection(_connectionString))
        {
            using (IDbCommand command = _connection.CreateCommand())
            {
                _connection.Open();
                command.CommandText = $"SELECT POSTING_CATEGORY, CASE WHEN IN_USE = 'Y' AND INCLUDED_IN_COHORT = 'Y' THEN 1 ELSE 0 END AS result FROM [dbo].[CURRENT_POSTING_LKP] WHERE POSTING = @currentPosting";
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@currentPosting";
                parameter.Value = currentPosting ?? string.Empty;
                command.Parameters.Add(parameter);

                var isCurrentPostingInDB = false;
                var postingCategory = "";
                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        postingCategory = reader["POSTING_CATEGORY"].ToString();
                        isCurrentPostingInDB = reader.GetInt32(1) == 1;
                    }
                }

                var CurrentPostingDoesNotExistInDB = currentPosting != null && !isCurrentPostingInDB && !validatePostingCategories(currentPosting);
                var PrimaryCareProviderDoesNotExistOnDB = primaryCareProvider != null && !PrimaryCareProviderExists(primaryCareProvider);

                if (CurrentPostingDoesNotExistInDB && PrimaryCareProviderDoesNotExistOnDB)
                {
                    return false;
                }
                return true;
            }
        }
    }

    private bool validatePostingCategories(string postingCategory)
    {
        if (allPossiblePostingCategories.Contains(postingCategory))
        {
            return true;
        }
        return false;
    }
}

