namespace Data.Database;

using System.Data;
using System.Net;
using System.Text.Json;
using Common;
using Microsoft.Extensions.Logging;
using Model;

public class CreateParticipantData : ICreateParticipantData
{
    private readonly IDbConnection _dbConnection;
    private readonly IDatabaseHelper _databaseHelper;
    private readonly string _connectionString;
    private readonly ILogger<CreateParticipantData> _logger;
    private readonly ICallFunction _callFunction;
    private readonly IUpdateParticipantData _updateParticipantData;

    public CreateParticipantData(IDbConnection dbConnection, IDatabaseHelper databaseHelper, ILogger<CreateParticipantData> logger,
        ICallFunction callFunction, IUpdateParticipantData updateParticipantData)
    {
        _dbConnection = dbConnection;
        _databaseHelper = databaseHelper;
        _logger = logger;
        _callFunction = callFunction;
        _updateParticipantData = updateParticipantData;
        _connectionString = Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString") ?? string.Empty;
    }

    public async Task<bool> CreateParticipantEntry(ParticipantCsvRecord participantCsvRecord)
    {
        var participantData = participantCsvRecord.Participant;

        // Check if a participant with the supplied NHS Number already exists
        var existingParticipantData = _updateParticipantData.GetParticipant(participantData.NhsNumber);
        var response = await ValidateData(existingParticipantData, participantData, participantCsvRecord.FileName);
        if (response.ExceptionFlag == "Y")
        {
            participantData = response;
        }

        DateTime dateToday = DateTime.Today;
        var sqlToExecuteInOrder = new List<SQLReturnModel>();

        string insertParticipant = "INSERT INTO [dbo].[PARTICIPANT_MANAGEMENT] ( " +
            " SCREENING_ID," +
            " NHS_NUMBER," +
            " REASON_FOR_REMOVAL," +
            " REASON_FOR_REMOVAL_DT," +
            " BUSINESS_RULE_VERSION," +
            " EXCEPTION_FLAG," +
            " RECORD_INSERT_DATETIME," +
            " RECORD_UPDATE_DATETIME" +
            " ) VALUES( " +
            " @screeningId, " +
            " @NHSNumber, " +
            " @reasonForRemoval, " +
            " @reasonForRemovalDate, " +
            " @businessRuleVersion, " +
            " @exceptionFlag, " +
            " @recordInsertDateTime, " +
            " @recordUpdateDateTime " +
            " ) ";

        var commonParameters = new Dictionary<string, object>
        {
            { "@screeningId", _databaseHelper.CheckIfNumberNull(participantData.ScreeningId) ? DBNull.Value : participantData.ScreeningId},
            { "@NHSNumber", _databaseHelper.CheckIfNumberNull(participantData.NhsNumber)  ? DBNull.Value : participantData.NhsNumber},
            { "@reasonForRemoval", _databaseHelper.ConvertNullToDbNull(participantData.ReasonForRemoval)},
            { "@reasonForRemovalDate", _databaseHelper.CheckIfDateNull(participantData.ReasonForRemovalEffectiveFromDate) ? DBNull.Value : _databaseHelper.ParseDates(participantData.ReasonForRemovalEffectiveFromDate)},
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

        return ExecuteBulkCommand(sqlToExecuteInOrder, commonParameters);
    }

    private bool ExecuteBulkCommand(List<SQLReturnModel> sqlCommands, Dictionary<string, object> commonParams)
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
            long newParticipantPk = 0;
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
            _logger.LogError("An error occurred while updating records: {Message}", ex.Message);
            return false;
        }
    }

    private bool Execute(IDbCommand command)
    {
        try
        {
            var result = command.ExecuteNonQuery();
            _logger.LogInformation("Executing command affected {RowNumber} rows.", result);

            if (result == 0)
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("An error occurred while executing SQL command: {Message}", ex.Message);
            return false;
        }

        return true;
    }

    private long ExecuteCommandAndGetId(string sql, IDbCommand command, IDbTransaction transaction)
    {
        command.Transaction = transaction;
        long newParticipantPk = -1;

        try
        {
            command.CommandText = sql;
            _logger.LogInformation("Command text: {Sql}", sql);

            var newParticipantResult = command.ExecuteNonQuery();
            var SQLGet = $"SELECT PARTICIPANT_ID FROM [dbo].[PARTICIPANT_MANAGEMENT] WHERE NHS_NUMBER = @NHSNumber";

            command.CommandText = SQLGet;
            using (IDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    newParticipantPk = reader.GetInt64(0);
                }
            }

            return newParticipantPk;
        }
        catch (Exception ex)
        {
            _logger.LogError("An error occurred: {Message}", ex.Message);
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
    private async Task<Participant> ValidateData(Participant existingParticipant, Participant newParticipant, string fileName)
    {
        var json = JsonSerializer.Serialize(new LookupValidationRequestBody(existingParticipant, newParticipant, fileName));

        try
        {
            var response = await _callFunction.SendPost(Environment.GetEnvironmentVariable("LookupValidationURL"), json);
            if (response.StatusCode == HttpStatusCode.Created)
            {
                newParticipant.ExceptionFlag = "Y";
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Lookup validation failed.\nMessage: {ex.Message}\nParticipant: {newParticipant}");
            return null;
        }

        return newParticipant;
    }
}
