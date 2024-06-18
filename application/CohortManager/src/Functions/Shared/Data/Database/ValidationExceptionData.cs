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
                    RuleId = reader["RULE_ID"] == DBNull.Value ? null : reader["RULE_ID"].ToString(),
                    RuleName = reader["RULE_NAME"] == DBNull.Value ? null : reader["RULE_NAME"].ToString(),
                    Workflow = reader["WORKFLOW"] == DBNull.Value ? null : reader["WORKFLOW"].ToString(),
                    NhsNumber = reader["NHS_NUMBER"] == DBNull.Value ? null : reader["NHS_NUMBER"].ToString(),
                    DateCreated = reader["DATE_CREATED"] == DBNull.Value ? null : DateTime.Parse(reader["DATE_CREATED"].ToString())
                });
            }

            return rules;
        });
    }

    public bool Create(ValidationException exception)
    {
        var SQL = "INSERT INTO [dbo].[VALIDATION_EXCEPTION] ([RULE_ID], [RULE_NAME], [WORKFLOW], [NHS_NUMBER], [DATE_CREATED]) " +
                    "VALUES (@ruleId, @ruleName, @workflow, @nhsNumber, @dateCreated);";

        var parameters = new Dictionary<string, object>()
        {
            {"@ruleId", exception.RuleId},
            {"@ruleName", exception.RuleName},
            {"@workflow", exception.Workflow},
            {"@nhsNumber", exception.NhsNumber},
            {"@dateCreated", exception.DateCreated}
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
