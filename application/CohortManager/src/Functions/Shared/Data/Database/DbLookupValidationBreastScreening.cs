namespace Data.Database;
using System.Data;
using DataServices.Client;
using Model;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

/// <summary>
/// Various validation methods for use in the breast screening lookup/ cohort rules
/// </summary>
///
[Obsolete("Deprecated Please use DataLookupFacade Instead",true)]
public class DbLookupValidationBreastScreening : IDbLookupValidationBreastScreening
{
    private IDbConnection _connection;
    private readonly string _connectionString;

    private readonly ILogger<DbLookupValidationBreastScreening> _logger;

    private readonly IDataServiceClient<BsSelectGpPractice> _gpPracticeServiceClient;
    private readonly IDataServiceClient<BsSelectOutCode> _outcodeClient;
    private readonly IDataServiceClient<LanguageCode> _languageCodeClient;
    private readonly string[] allPossiblePostingCategories = ["ENGLAND", "IOM", "DMS"];

    public DbLookupValidationBreastScreening(
        IDbConnection IdbConnection,
        ILogger<DbLookupValidationBreastScreening> logger,
        IDataServiceClient<BsSelectGpPractice> gpPracticeServiceClient,
        IDataServiceClient<BsSelectOutCode> outcodeClient,
        IDataServiceClient<LanguageCode> languageCodeClient
        )
    {
        _connection = IdbConnection;
        _connectionString = Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString") ?? string.Empty;
        _logger = logger;
        _gpPracticeServiceClient = gpPracticeServiceClient;
        _outcodeClient = outcodeClient;
        _languageCodeClient = languageCodeClient;
    }

    /// <summary>
    /// Used in rule 36 in the lookup rules, and rule 54 in the cohort rules.
    /// Validates the participants primary care provider (GP practice code)
    /// </summary>
    /// <param name="primaryCareProvider">The participant's primary care provider.</param>
    /// <returns>bool, whether or not the GP practice code exists in the DB.<returns>
    public bool CheckIfPrimaryCareProviderExists(string primaryCareProvider)
    {
        _logger.LogInformation("Checking Primary Care Provider {primaryCareProvider} Exists", primaryCareProvider);
        var result =  _gpPracticeServiceClient.GetSingle(primaryCareProvider).Result;
        return result != null;
    }



    /// <summary>
    /// Used in rule 54 in the cohort rules. Validates the participants outcode (1st part of the postcode)
    /// </summary>
    /// <param name="postcode">The participant's postcode.</param>
    /// <returns>bool, whether or not the outcode code exists in the DB.<returns>
    public bool ValidateOutcode(string postcode)
    {


        var outcode = postcode.Substring(0, postcode.IndexOf(" "));
        _logger.LogInformation("Valdating Outcode: {outcode}",outcode);
        var result = _outcodeClient.GetSingle(outcode);

        return result != null;


    }

    /// <summary>
    /// Retrieves the participant's BSO code (using the participant's outcode)
    /// </summary>
    /// <param name="postcode">The participant's postcode.</param>
    /// <returns>string, BSO code<returns>
    public string RetrieveBSOCode(string postcode)
    {
        try
        {
            var outcode = postcode.Substring(0, postcode.IndexOf(" "));

            using (_connection = new SqlConnection(_connectionString))
            {
                _connection.Open();
                using (IDbCommand command = _connection.CreateCommand())
                {
                    command.CommandText = $"SELECT BSO FROM [dbo].[BS_SELECT_OUTCODE_MAPPING_LKP] WHERE OUTCODE = @outcode";
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@outcode";
                    parameter.Value = outcode;
                    command.Parameters.Add(parameter);

                    using (IDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader["BSO"].ToString() ?? string.Empty;
                        }
                    }
                    return string.Empty;
                }
            }
        }
        catch (Exception)
        {
            throw new System.ComponentModel.DataAnnotations.ValidationException();
        }
        finally
        {
            _connection.Close();
        }
    }

    /// <summary>
    /// Used in rule 00 in the lookup rules. Validates the participants preferred language code.
    /// </summary>
    /// <param name="languageCode">The participant's preferred language code.</param>
    /// <returns>bool, whether or not the language code exists in the DB.<returns>
    public bool ValidateLanguageCode(string languageCode)
    {
        _logger.LogInformation("Valdating Language Code: {languageCode}",languageCode);
        var result = _languageCodeClient.GetSingle(languageCode).Result;
        return result != null;
    }

    /// Used in rule 58 of the lookup rules.
    /// Validates that the current posting exists, and that it is in the cohort and in use.
    /// </summary>
    /// <param name="currentPosting">The participant's current posting (area code).</param>
    /// <returns>bool, whether or not the current posting is valid.<returns>
    public bool CheckIfCurrentPostingExists(string currentPosting)
    {
        try
        {
            using (_connection = new SqlConnection(_connectionString))
            {
                using (IDbCommand command = _connection.CreateCommand())
                {
                    _connection.Open();
                    command.CommandText = $"SELECT CASE WHEN IN_USE = 'Y' AND INCLUDED_IN_COHORT = 'Y' THEN 1 ELSE 0 END AS result FROM [dbo].[CURRENT_POSTING_LKP] WHERE POSTING = @currentPosting;";
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@currentPosting";
                    parameter.Value = currentPosting ?? string.Empty;
                    command.Parameters.Add(parameter);

                    var isCurrentPostingInDB = false;
                    using (IDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            isCurrentPostingInDB = reader.GetInt32(0) == 1;
                        }
                    }

                    return isCurrentPostingInDB;
                }
            }
        }
        finally
        {
            _connection.Close();
        }

    }

    /// <summary>
    /// takes in posting and returns if that posting has a valid posting category in the database
    /// </summary>
    /// <param name="postingCategory"></param>
    /// <returns></returns>
    public bool ValidatePostingCategories(string currentPosting)
    {
        try
        {
            using (_connection = new SqlConnection(_connectionString))
            {
                using (IDbCommand command = _connection.CreateCommand())
                {
                    _connection.Open();
                    command.CommandText = $"SELECT POSTING_CATEGORY FROM [dbo].[CURRENT_POSTING_LKP] WHERE POSTING = @currentPosting;";
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@currentPosting";
                    parameter.Value = currentPosting ?? string.Empty;
                    command.Parameters.Add(parameter);

                    var postingCategory = "";
                    using (IDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            postingCategory = reader["POSTING_CATEGORY"].ToString();
                        }
                    }
                    return allPossiblePostingCategories.Contains(postingCategory);

                }
            }
        }
        finally
        {
            _connection.Close();
        }
    }

    /// <summary>
    /// takes in posting and returns a valid posting category (if exists) from the database
    /// </summary>
    /// <param name="currentPosting"></param>
    /// <returns></returns>
    public string RetrievePostingCategory(string currentPosting)
    {
        try
        {
            using (_connection = new SqlConnection(_connectionString))
            {
                using (IDbCommand command = _connection.CreateCommand())
                {
                    _connection.Open();
                    command.CommandText = $"SELECT POSTING_CATEGORY FROM [dbo].[CURRENT_POSTING_LKP] WHERE POSTING = @currentPosting;";
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@currentPosting";
                    parameter.Value = currentPosting ?? string.Empty;
                    command.Parameters.Add(parameter);

                    var postingCategory = "";
                    using (IDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            postingCategory = reader["POSTING_CATEGORY"].ToString() ?? string.Empty;
                        }
                    }
                    return string.Empty;
                }
            }
        }
        finally
        {
            _connection.Close();
        }
    }

    /// <summary>
    /// Check if the Primary Care Provider is on the 'Excluded SMU list'
    /// </summary>
    /// <param name="primaryCareProvider"></param>
    /// <returns></returns>
    public bool CheckIfPrimaryCareProviderInExcludedSmuList(string primaryCareProvider)
    {
        try
        {
            using (_connection = new SqlConnection(_connectionString))
            {
                using (IDbCommand command = _connection.CreateCommand())
                {
                    _connection.Open();
                    command.CommandText = $"SELECT GP_PRACTICE_CODE FROM [dbo].[EXCLUDED_SMU_LKP] WHERE GP_PRACTICE_CODE = @primaryCareProvider;";
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@primaryCareProvider";
                    parameter.Value = primaryCareProvider;
                    command.Parameters.Add(parameter);

                    using (IDataReader reader = command.ExecuteReader())
                    {
                        return reader.Read();
                    }
                }
            }
        }
        finally
        {
            _connection.Close();
        }
    }
}
