namespace Data.Database;

using System;
using System.Data;
using Microsoft.Extensions.Logging;

public class ValidationData : IValidationData
{
    private readonly IDbConnection _dbConnection;
    private readonly string connectionString;
    private readonly ILogger<ValidationData> _logger;

    public ValidationData(IDbConnection IdbConnection, ILogger<ValidationData> logger)
    {
        _dbConnection = IdbConnection;
        _logger = logger;
        connectionString = Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString") ?? string.Empty;
    }

    public List<ValidationDataDto> GetAll()
    {
        var SQL = "SELECT * FROM [dbo].[RULE_VIOLATION]";

        var command = CreateCommand(new Dictionary<string, object>());
        command.CommandText = SQL;
        return ExecuteQuery(command, reader =>
        {
            var rules = new List<ValidationDataDto>();
            while (reader.Read())
            {
                rules.Add(new ValidationDataDto
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

    public bool Create(ValidationDataDto dto)
    {
        var SQL = "INSERT INTO [dbo].[RULE_VIOLATION] ([RULE_ID], [RULE_NAME], [WORKFLOW], [NHS_NUMBER], [DATE_CREATED]) " +
                    "VALUES (@ruleId, @ruleName, @workflow, @nhsNumber, @dateCreated);";

        var parameters = new Dictionary<string, object>()
        {
            {"@ruleId", dto.RuleId},
            {"@ruleName", dto.RuleName},
            {"@workflow", dto.Workflow},
            {"@nhsNumber", dto.NhsNumber},
            {"@dateCreated", dto.DateCreated}
        };

        var command = CreateCommand(parameters);
        var transaction = BeginTransaction();
        try
        {
            command.Transaction = transaction;
            command.CommandText = SQL;

            if (!Execute(command))
            {
                transaction.Rollback();
                _dbConnection.Close();
                return false;
            }

            transaction.Commit();
            _dbConnection.Close();
            return true;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _dbConnection.Close();
            _logger.LogError($"An error occurred while updating records: {ex.Message}");
            return false;
        }
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
            _dbConnection.ConnectionString = connectionString;
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

    private IDbTransaction BeginTransaction()
    {
        _dbConnection.ConnectionString = connectionString;
        _dbConnection.Open();
        return _dbConnection.BeginTransaction();
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
