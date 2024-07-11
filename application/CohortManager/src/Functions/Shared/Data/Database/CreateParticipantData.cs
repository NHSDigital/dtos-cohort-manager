namespace Data.Database;

using System.Data;
using Microsoft.Extensions.Logging;
using Model;

public class CreateParticipantData : ICreateParticipantData
{
    private readonly IDbConnection _dbConnection;
    private readonly IDatabaseHelper _databaseHelper;
    private readonly string _connectionString;
    private readonly ILogger<CreateParticipantData> _logger;

    public CreateParticipantData(IDbConnection dbConnection, IDatabaseHelper databaseHelper, ILogger<CreateParticipantData> logger)
    {
        _dbConnection = dbConnection;
        _databaseHelper = databaseHelper;
        _logger = logger;
        _connectionString = Environment.GetEnvironmentVariable("SqlConnectionString") ?? string.Empty;
    }

    public bool CreateParticipantEntry(ParticipantCsvRecord participantCsvRecord)
    {
        string cohortId = "1";
        DateTime dateToday = DateTime.Today;
        var sqlToExecuteInOrder = new List<SQLReturnModel>();
        var participantData = participantCsvRecord.Participant;

        string insertParticipant = "INSERT INTO [dbo].[PARTICIPANT_MANAGEMENT] ( " +
            " PARTICIPANT_ID, " +
            " SCREENING_ID," +
            " NHS_NUMBER," +
            " REASON_FOR_REMOVAL," +
            " REASON_FOR_REMOVAL_DT," +
            " BUSINESS_RULE_VERSION," +
            " EXCEPTION_FLAG," +
            " RECORD_INSERT_DATETIME," +
            " RECORD_UPDATE_DATETIME," +
            " ) VALUES( " +
            " @participantId, " +
            " @screeningId, " +
            " @NHSNumber, " +
            " @reasonForRemoval, " +
            " @reasonForRemovalDate, " +
            " @businessRuleVersion, " +
            " @exceptionFlag, " +
            " @recordInsertDateTime, " +
            " @recordUpdateDateTime, " +
            " ) ";

        var commonParameters = new Dictionary<string, object>
        {
            { "@participantId", cohortId},
            { "@screeningId", participantData.ScreeningId},
            { "@NHSNumber", participantData.NhsNumber },
            { "@reasonForRemoval", _databaseHelper.CheckIfNumberNull(participantData.ReasonForRemoval) ? DBNull.Value : participantData.ReasonForRemoval},
            { "@reasonForRemovalDate", _databaseHelper.ParseDates(participantData.ReasonForRemovalEffectiveFromDate)},
            { "@businessRuleVersion", _databaseHelper.CheckIfDateNull(participantData.BusinessRuleVersion) ? DBNull.Value : _databaseHelper.ParseDates(participantData.BusinessRuleVersion)},
            { "@exceptionFlag",  _databaseHelper.ConvertNullToDbNull(participantData.ExceptionFlag) },
            { "@recordInsertDateTime", dateToday },
            { "@recordUpdateDateTime", DBNull.Value },
        };

        sqlToExecuteInOrder.Add(new SQLReturnModel()
        {
            CommandType = CommandType.Scalar,
            SQL = insertParticipant,
            Parameters = null
        });

        return ExecuteBulkCommand(sqlToExecuteInOrder, commonParameters, participantData.NhsNumber);
    }

    private bool ExecuteBulkCommand(List<SQLReturnModel> sqlCommands, Dictionary<string, object> commonParams, string NhsNumber)
    {
        var command = CreateCommand(commonParams);
        foreach (var SqlCommand in sqlCommands)
        {
            if (SqlCommand.Parameters != null)
            {
                AddParameters(SqlCommand.Parameters, command);
            }
        }

        var transaction = BeginTransaction();
        command.Transaction = transaction;

        try
        {
            var newParticipantPk = -1;
            foreach (var sqlCommand in sqlCommands)
            {

                if (sqlCommand.CommandType == CommandType.Scalar)
                {
                    newParticipantPk = ExecuteCommandAndGetId(sqlCommand.SQL, command, transaction);
                    AddParameters(new Dictionary<string, object>()
                    {
                        {"@NewParticipantId", newParticipantPk }
                    }, command);
                }
                if (sqlCommand.CommandType == CommandType.Command)
                {
                    command.CommandText = sqlCommand.SQL;
                    if (!Execute(command))
                    {
                        transaction.Rollback();
                        _dbConnection.Close();
                        return false;
                    }
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

    private int ExecuteCommandAndGetId(string SQL, IDbCommand command, IDbTransaction transaction)
    {
        command.Transaction = transaction;
        var newParticipantPk = -1;

        try
        {
            command.CommandText = SQL;
            _logger.LogInformation($"{SQL}");

            var newParticipantResult = command.ExecuteNonQuery();
            var SQLGet = $"SELECT PARTICIPANT_ID FROM [dbo].[PARTICIPANT_MANAGEMENT] WHERE NHS_NUMBER = @NHSNumber AND ACTIVE_FLAG = @ActiveFlag "; //WP - Change properties from Participant to Participant Management

            command.CommandText = SQLGet;
            using (IDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    newParticipantPk = reader.GetInt32(0);
                }
            }

            return newParticipantPk;

        }
        catch (Exception ex)
        {
            _logger.LogError($"an error happened: {ex.Message}");
            return -1;
        }
    }

    private IDbTransaction BeginTransaction()
    {
        _dbConnection.ConnectionString = _connectionString;
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
        if (parameters == null) return dbCommand;
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
