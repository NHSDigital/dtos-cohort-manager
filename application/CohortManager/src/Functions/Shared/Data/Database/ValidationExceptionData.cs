namespace Data.Database;

using System;
using System.Data;
using System.Text;
using Microsoft.Extensions.Logging;
using Model;

public class ValidationExceptionData : IValidationExceptionData
{
    private readonly IDbConnection _dbConnection;
    private readonly string _connectionString;
    private readonly ILogger<ValidationExceptionData> _logger;

    public ValidationExceptionData(IDbConnection IdbConnection, ILogger<ValidationExceptionData> logger)
    {
        _dbConnection = IdbConnection;
        _logger = logger;
        _connectionString = Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString") ?? string.Empty;
    }

    public List<ValidationException> GetAllExceptions(bool todayOnly)
    {
        var today = DateTime.Today.Date;

        var sql = new StringBuilder(@"SELECT
                 [EXCEPTION_ID]
                ,[FILE_NAME]
                ,[NHS_NUMBER]
                ,[DATE_CREATED]
                ,[DATE_RESOLVED]
                ,[RULE_ID]
                ,[RULE_DESCRIPTION]
                ,[ERROR_RECORD]
                ,[CATEGORY]
                ,[SCREENING_NAME]
                ,[EXCEPTION_DATE]
                ,[COHORT_NAME]
                ,[IS_FATAL]
                FROM [dbo].[EXCEPTION_MANAGEMENT]");

        var parameters = new Dictionary<string, object>();

        if (todayOnly)
        {
            sql.Append(" WHERE CAST([DATE_CREATED] AS DATE) = @today");
            parameters.Add("@today", today);
        }

        sql.Append(" ORDER BY [DATE_CREATED] DESC");

        var command = CreateCommand(parameters);
        command.CommandText = sql.ToString();
        return GetException(command, false);
    }

    private List<ValidationException> GetException(IDbCommand command, bool includeDetails)
    {
        var exceptions = new List<ValidationException>();
        return ExecuteQuery(command, reader =>
        {
            while (reader.Read())
            {
                var exception = new ValidationException
                {
                    ExceptionId = DatabaseHelper.GetValue<int>(reader, "EXCEPTION_ID"),
                    FileName = DatabaseHelper.GetValue<string>(reader, "FILE_NAME"),
                    NhsNumber = DatabaseHelper.GetValue<string>(reader, "NHS_NUMBER"),
                    DateCreated = DatabaseHelper.GetValue<DateTime>(reader, "DATE_CREATED"),
                    DateResolved = DatabaseHelper.GetValue<DateTime>(reader, "DATE_RESOLVED"),
                    RuleId = DatabaseHelper.GetValue<int>(reader, "RULE_ID"),
                    RuleDescription = DatabaseHelper.GetValue<string>(reader, "RULE_DESCRIPTION"),
                    ErrorRecord = DatabaseHelper.GetValue<string>(reader, "ERROR_RECORD"),
                    Category = DatabaseHelper.GetValue<int>(reader, "CATEGORY"),
                    ScreeningName = DatabaseHelper.GetValue<string>(reader, "SCREENING_NAME"),
                    ExceptionDate = DatabaseHelper.GetValue<DateTime>(reader, "EXCEPTION_DATE"),
                    CohortName = DatabaseHelper.GetValue<string>(reader, "COHORT_NAME"),
                    Fatal = DatabaseHelper.GetValue<int>(reader, "IS_FATAL")
                };

                if (includeDetails)
                {
                    exception.ExceptionDetails = new ExceptionDetails
                    {
                        GivenName = DatabaseHelper.GetValue<string>(reader, "GIVEN_NAME"),
                        FamilyName = DatabaseHelper.GetValue<string>(reader, "FAMILY_NAME"),
                        DateOfBirth = DatabaseHelper.GetValue<string>(reader, "DATE_OF_BIRTH"),
                        ParticipantAddressLine1 = DatabaseHelper.GetValue<string>(reader, "PARTICIPANT_ADDRESS_LINE_1"),
                        ParticipantAddressLine2 = DatabaseHelper.GetValue<string>(reader, "PARTICIPANT_ADDRESS_LINE_2"),
                        ParticipantAddressLine3 = DatabaseHelper.GetValue<string>(reader, "PARTICIPANT_ADDRESS_LINE_3"),
                        ParticipantAddressLine4 = DatabaseHelper.GetValue<string>(reader, "PARTICIPANT_ADDRESS_LINE_4"),
                        ParticipantAddressLine5 = DatabaseHelper.GetValue<string>(reader, "PARTICIPANT_ADDRESS_LINE_5"),
                        ParticipantPostCode = DatabaseHelper.GetValue<string>(reader, "PARTICIPANT_POSTCODE"),
                        TelephoneNumberHome = DatabaseHelper.GetValue<string>(reader, "TELEPHONE_NUMBER_HOME"),
                        EmailAddressHome = DatabaseHelper.GetValue<string>(reader, "EMAIL_ADDRESS_HOME"),
                        PrimaryCareProvider = DatabaseHelper.GetValue<string>(reader, "PRIMARY_CARE_PROVIDER"),
                        GpPracticeCode = DatabaseHelper.GetValue<string>(reader, "GP_PRACTICE_CODE"),
                        GpAddressLine1 = DatabaseHelper.GetValue<string>(reader, "GP_ADDRESS_LINE_1"),
                        GpAddressLine2 = DatabaseHelper.GetValue<string>(reader, "GP_ADDRESS_LINE_2"),
                        GpAddressLine3 = DatabaseHelper.GetValue<string>(reader, "GP_ADDRESS_LINE_3"),
                        GpAddressLine4 = DatabaseHelper.GetValue<string>(reader, "GP_ADDRESS_LINE_4"),
                        GpAddressLine5 = DatabaseHelper.GetValue<string>(reader, "GP_ADDRESS_LINE_5"),
                        GpPostCode = DatabaseHelper.GetValue<string>(reader, "GP_POSTCODE"),
                    };
                }

                exceptions.Add(exception);
            }
            return exceptions;
        });
    }

    public ValidationException GetExceptionById(int exceptionId)
    {
        var SQL = @" SELECT
                    pd.NHS_NUMBER,
                    pd.GIVEN_NAME,
                    pd.FAMILY_NAME,
                    pd.DATE_OF_BIRTH,
                    pd.ADDRESS_LINE_1 AS PARTICIPANT_ADDRESS_LINE_1,
                    pd.ADDRESS_LINE_2 AS PARTICIPANT_ADDRESS_LINE_2,
                    pd.ADDRESS_LINE_3 AS PARTICIPANT_ADDRESS_LINE_3,
                    pd.ADDRESS_LINE_4 AS PARTICIPANT_ADDRESS_LINE_4,
                    pd.ADDRESS_LINE_5 AS PARTICIPANT_ADDRESS_LINE_5,
                    pd.POST_CODE AS PARTICIPANT_POSTCODE,
                    pd.TELEPHONE_NUMBER_HOME,
                    pd.EMAIL_ADDRESS_HOME,
                    pd.PRIMARY_CARE_PROVIDER,
                    gp.GP_PRACTICE_CODE,
                    gp.ADDRESS_LINE_1 AS GP_ADDRESS_LINE_1,
                    gp.ADDRESS_LINE_2 AS GP_ADDRESS_LINE_2,
                    gp.ADDRESS_LINE_3 AS GP_ADDRESS_LINE_3,
                    gp.ADDRESS_LINE_4 AS GP_ADDRESS_LINE_4,
                    gp.ADDRESS_LINE_5 AS GP_ADDRESS_LINE_5,
                    gp.POSTCODE AS GP_POSTCODE,
                    em.EXCEPTION_ID,
                    em.FILE_NAME,
                    em.DATE_CREATED,
                    em.DATE_RESOLVED,
                    em.RULE_ID,
                    em.RULE_DESCRIPTION,
                    em.ERROR_RECORD,
                    em.CATEGORY,
                    em.SCREENING_NAME,
                    em.EXCEPTION_DATE,
                    em.COHORT_NAME,
                    em.IS_FATAL
                    FROM
                    [dbo].[EXCEPTION_MANAGEMENT] em
                    JOIN
                    [dbo].[PARTICIPANT_DEMOGRAPHIC] pd
                    ON CAST(pd.NHS_NUMBER AS VARCHAR(50)) = em.NHS_NUMBER
                    JOIN
                    [dbo].[GP_PRACTICES] gp
                    ON pd.PRIMARY_CARE_PROVIDER = gp.GP_PRACTICE_CODE
                    WHERE
                    em.[EXCEPTION_ID] = @ExceptionId";

        var parameters = new Dictionary<string, object>
        {
            {"@ExceptionId", exceptionId },
        };

        var command = CreateCommand(parameters);
        command.CommandText = SQL;

        return GetException(command, true).FirstOrDefault();
    }

    public bool Create(ValidationException exception)
    {

        var SQL = @"INSERT INTO [dbo].[EXCEPTION_MANAGEMENT] (
                    FILE_NAME,
                    NHS_NUMBER,
                    DATE_CREATED,
                    DATE_RESOLVED,
                    RULE_ID,
                    RULE_DESCRIPTION,
                    ERROR_RECORD,
                    CATEGORY,
                    SCREENING_NAME,
                    EXCEPTION_DATE,
                    COHORT_NAME,
                    IS_FATAL
                    ) VALUES (
                    @fileName,
                    @nhsNumber,
                    @dateCreated,
                    @dateResolved,
                    @ruleId,
                    @ruleDescription,
                    @errorRecord,
                    @category,
                    @screeningName,
                    @exceptionDate,
                    @cohortName,
                    @fatal
                );";

        var parameters = new Dictionary<string, object>()
        {
            {"@fileName", exception.FileName},
            {"@nhsNumber", exception.NhsNumber},
            {"@dateCreated", exception.DateCreated},
            {"@dateResolved", exception.DateResolved.HasValue ? exception.DateResolved : DBNull.Value},
            {"@ruleId", exception.RuleId},
            {"@ruleDescription", exception.RuleDescription},
            {"@errorRecord", exception.ErrorRecord},
            {"@category", exception.Category},
            {"@screeningName", exception.ScreeningName},
            {"@exceptionDate", exception.ExceptionDate},
            {"@cohortName", exception.CohortName},
            {"@fatal", exception.Fatal}
        };

        var command = CreateCommand(parameters);
        command.CommandText = SQL;

        try
        {
            return ExecuteCommand(command);
        }
        finally
        {
            _dbConnection.Close();
        }
    }

    public bool RemoveOldException(string nhsNumber, string screeningName)
    {

        if (!RecordExists(nhsNumber, screeningName))
        {
            return false;
        }

        // we only need to get the last unresolved exception for the nhs number and screening service
        var SQL = @"UPDATE [dbo].EXCEPTION_MANAGEMENT
                    SET DATE_RESOLVED = @todaysDate
                    WHERE NHS_NUMBER = @nhsNumber AND DATE_RESOLVED = @MaxDate AND SCREENING_NAME = @screeningName";

        var command = CreateCommand(new Dictionary<string, object>()
        {
            {"@nhsNumber", nhsNumber},
            {"@todaysDate", DateTime.Today},
            {"@MaxDate", "9999-12-31"},
            {"@screeningName", screeningName},
        });

        command.CommandText = SQL;
        try
        {
            var removed = ExecuteCommand(command);
            if (removed)
            {
                _logger.LogInformation("Removed old exception record successfully");
                return true;
            }
            _logger.LogWarning("An exception record was found but not Removed successfully");
            return false;
        }
        finally
        {
            _dbConnection.Close();
        }


    }

    private bool RecordExists(string nhsNumber, string screeningName)
    {
        try
        {
            var recordExists = false;
            var SQL = "SELECT 1 FROM [dbo].[EXCEPTION_MANAGEMENT] WHERE NHS_NUMBER = @nhsNumber AND SCREENING_NAME = @screeningName";

            var command = CreateCommand(new Dictionary<string, object>()
        {
            {"@nhsNumber", nhsNumber},
            {"@screeningName", screeningName}
        });
            command.CommandText = SQL;
            using (_dbConnection)
            {
                _dbConnection.ConnectionString = _connectionString;
                _dbConnection.Open();
                using (command)
                {
                    using (IDataReader reader = command.ExecuteReader())
                    {
                        // Return true if the reader has at least one row.
                        recordExists = reader.Read();
                    }
                }
            }

            return recordExists;
        }
        finally
        {
            if (_dbConnection != null)
            {
                _dbConnection.Close();
            }
        }
    }

    private bool ExecuteCommand(IDbCommand command)
    {
        _dbConnection.ConnectionString = _connectionString;
        _dbConnection.Open();
        var inserted = Execute(command);
        _dbConnection.Close();

        if (inserted)
        {
            return true;
        }
        return false;
    }

    private bool Execute(IDbCommand command)
    {
        try
        {
            var result = command.ExecuteNonQuery();
            _logger.LogInformation(result.ToString());

            if (result == 0)
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"an error happened: {ex.Message}");
            return false;
        }

        return true;
    }

    private T ExecuteQuery<T>(IDbCommand command, Func<IDataReader, T> mapFunction)
    {
        try
        {
            var result = default(T);
            using (_dbConnection)
            {
                _dbConnection.ConnectionString = _connectionString;
                _dbConnection.Open();
                using (command)
                {
                    using (IDataReader reader = command.ExecuteReader())
                    {
                        result = mapFunction(reader);
                    }
                }
                return result;
            }
        }
        finally
        {
            if (_dbConnection != null)
            {
                _dbConnection.Close();
            }
        }
    }

    private IDbCommand CreateCommand(Dictionary<string, object> parameters)
    {
        var dbCommand = _dbConnection.CreateCommand();
        return AddParameters(parameters, dbCommand);
    }

    private IDbCommand AddParameters(Dictionary<string, object> parameters, IDbCommand dbCommand)
    {
        foreach (var param in parameters)
        {
            var parameter = dbCommand.CreateParameter();

            parameter.ParameterName = param.Key;
            parameter.Value = param.Value;

            dbCommand.Parameters.Add(parameter);
        }

        return dbCommand;
    }
}
