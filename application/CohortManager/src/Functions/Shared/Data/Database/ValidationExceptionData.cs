namespace Data.Database;

using System;
using System.Data;
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

    public List<ValidationException> GetAll()
    {
        var SQL = @"SELECT
                [FILE_NAME]
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
                FROM [dbo].[EXCEPTION_MANAGEMENT]";

        var command = CreateCommand(new Dictionary<string, object>());
        command.CommandText = SQL;
        return ExecuteQuery(command, reader =>
        {
            var rules = new List<ValidationException>();
            while (reader.Read())
            {
                rules.Add(new ValidationException
                {
                    FileName = reader.GetString(reader.GetOrdinal("FILE_NAME")) ?? null,
                    NhsNumber = reader.GetString(reader.GetOrdinal("NHS_NUMBER")) ?? null,
                    DateCreated = reader.GetDateTime(reader.GetOrdinal("DATE_CREATED")),
                    DateResolved = reader.GetDateTime(reader.GetOrdinal("DATE_RESOLVED")),
                    RuleId = reader.GetInt32(reader.GetOrdinal("RULE_ID")),
                    RuleDescription = reader.GetString(reader.GetOrdinal("RULE_DESCRIPTION")) ?? null,
                    ErrorRecord = reader.GetString(reader.GetOrdinal("ERROR_RECORD")) ?? null,
                    Category = reader.GetInt32(reader.GetOrdinal("CATEGORY")),
                    ScreeningName = reader.GetString(reader.GetOrdinal("SCREENING_NAME")),
                    ExceptionDate = reader.GetDateTime(reader.GetOrdinal("EXCEPTION_DATE")),
                    CohortName = reader.GetString(reader.GetOrdinal("COHORT_NAME")) ?? null,
                    Fatal = reader.GetInt16(reader.GetOrdinal("IS_FATAL"))
                });
            }

            return rules;
        });
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
