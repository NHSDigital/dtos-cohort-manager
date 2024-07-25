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
        var SQL = "SELECT * FROM [dbo].[VALIDATION_EXCEPTION]";

        var command = CreateCommand(new Dictionary<string, object>());
        command.CommandText = SQL;
        return ExecuteQuery(command, reader =>
        {
            var rules = new List<ValidationException>();
            while (reader.Read())
            {
                rules.Add(new ValidationException
                {
                    NhsNumber = reader.GetString(reader.GetOrdinal("NHS_NUMBER")) ?? null,
                    DateCreated = reader.GetDateTime(reader.GetOrdinal("DATE_CREATED")),
                    DateResolved = reader.GetDateTime(reader.GetOrdinal("DATE_RESOLVED")),
                    RuleId = reader.GetInt32(reader.GetOrdinal("RULE_ID")),
                    RuleDescription = reader.GetString(reader.GetOrdinal("RULE_DESCRIPTION")) ?? null,
                    RuleContent = reader.GetString(reader.GetOrdinal("RULE_CONTENT")) ?? null,
                    Category = reader.GetInt16(reader.GetOrdinal("CATEGORY")),
                    ScreeningService = reader.GetInt32(reader.GetOrdinal("SCREENING_SERVICE")),
                    Cohort = reader.GetString(reader.GetOrdinal("COHORT")) ?? null,
                    Fatal = reader.GetInt16(reader.GetOrdinal("FATAL"))
                });
            }

            return rules;
        });
    }

    public bool Create(ValidationException exception)
    {
        var SQL = @"INSERT INTO [dbo].[VALIDATION_EXCEPTION] (
                    NHS_NUMBER,
                    DATE_CREATED,
                    DATE_RESOLVED,
                    RULE_ID,
                    RULE_DESCRIPTION,
                    RULE_CONTENT,
                    CATEGORY,
                    SCREENING_SERVICE,
                    COHORT,
                    FATAL
                    ) VALUES (
                    @nhsNumber,
                    @dateCreated,
                    @dateResolved,
                    @ruleId,
                    @ruleDescription,
                    @ruleContent,
                    @category,
                    @screeningService,
                    @cohort,
                    @fatal
                );";

        var parameters = new Dictionary<string, object>()
        {
            {"@nhsNumber", exception.NhsNumber},
            {"@dateCreated", exception.DateCreated},
            {"@dateResolved", exception.DateResolved.HasValue ? exception.DateResolved : DBNull.Value},
            {"@ruleId", exception.RuleId},
            {"@ruleDescription", exception.RuleDescription},
            {"@ruleContent", exception.RuleContent},
            {"@category", exception.Category},
            {"@screeningService", exception.ScreeningService},
            {"@cohort", exception.Cohort},
            {"@fatal", exception.Fatal}
        };

        var command = CreateCommand(parameters);
        command.CommandText = SQL;

        return ExecuteCommand(command);
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
                _dbConnection.Close();
            }
            return result;
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
