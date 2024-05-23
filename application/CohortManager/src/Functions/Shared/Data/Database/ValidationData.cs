using System.Data;
using System.Security;
using Data.Database;
using Microsoft.Extensions.Logging;

namespace Data.Database;

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

    public List<ValidationDataDto> GetAllBrokenRules()
    {
        var SQL = "SELECT [RULE] " +
        ", [TIME_VIOLATED] " +
        ", [PARTICIPANT_ID] " +
        " FROM [dbo].[RULE_VIOLATED] ";

        var command = CreateCommand(new Dictionary<string, object>());
        command.CommandText = SQL;
        return ExecuteQuery(command, reader =>
        {
            var rules = new List<ValidationDataDto>();
            while (reader.Read())
            {
                rules.Add(new ValidationDataDto
                {
                    Rule = reader["RULE"] == DBNull.Value ? null : reader["RULE"].ToString(),
                    TimeViolated = DateTime.TryParse(reader["TIME_VIOLATED"].ToString(), out var result) ? result : null,
                    ParticipantId = reader["PARTICIPANT_ID"] == DBNull.Value ? null : reader["PARTICIPANT_ID"].ToString()
                });
            }

            return rules;
        });
    }

    public bool UpdateRecords(SQLReturnModel sqlToExecute)
    {
        var command = CreateCommand(sqlToExecute.parameters);
        var transaction = BeginTransaction();
        try
        {
            command.Transaction = transaction;

            command.CommandText = sqlToExecute.SQL;
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