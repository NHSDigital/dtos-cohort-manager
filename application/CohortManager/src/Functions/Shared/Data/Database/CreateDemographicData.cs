namespace Data.Database;
using System.Data;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using Model;

public class CreateDemographicData : ICreateDemographicData
{
    private IDbConnection _dbConnection;
    private IDatabaseHelper _databaseHelper;
    private readonly string connectionString;
    private readonly ILogger _logger;

    public CreateDemographicData(IDbConnection IdbConnection, IDatabaseHelper databaseHelper, ILogger logger)
    {
        _dbConnection = IdbConnection;
        _databaseHelper = databaseHelper;
        _logger = logger;
        connectionString = Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString");
    }

    public bool InsertDemographicData(Participant participant)
    {
        var command = new List<SQLReturnModel>()
        {
            new SQLReturnModel()
            {
                commandType = CommandType.Command,
                SQL = " INSERT INTO DEMOGRAPHIC_DATA (column1, column2, column3)" +
                    "VALUES (value1, value2, value3)",
                // we don't need to add params to all items as we don't want to duplicate them
                parameters = new Dictionary<string, object>
                {
                    {"@NHSID", null},
                },
            }
        };

        return UpdateRecords(command);
    }

    private bool UpdateRecords(List<SQLReturnModel> sqlToExecute)
    {
        var command = CreateCommand(sqlToExecute[0].parameters);
        var transaction = BeginTransaction();
        try
        {
            command.Transaction = transaction;
            foreach (var sqlCommand in sqlToExecute)
            {
                command.CommandText = sqlCommand.SQL;
                if (!Execute(command))
                {
                    transaction.Rollback();
                    _dbConnection.Close();
                    return false;
                }
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