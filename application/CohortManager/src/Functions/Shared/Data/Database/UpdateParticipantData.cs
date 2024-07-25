namespace Data.Database;

using System.Data;
using Microsoft.Extensions.Logging;
using Model;
using Common;
using System.Text.Json;
using System.Net;

public class UpdateParticipantData : IUpdateParticipantData
{
    private readonly IDbConnection _dbConnection;
    private readonly IDatabaseHelper _databaseHelper;
    private readonly string _connectionString;
    private readonly ILogger<UpdateParticipantData> _logger;
    private readonly ICallFunction _callFunction;

    public UpdateParticipantData(IDbConnection IdbConnection, IDatabaseHelper databaseHelper, ILogger<UpdateParticipantData> logger, ICallFunction callFunction)
    {
        _dbConnection = IdbConnection;
        _databaseHelper = databaseHelper;
        _logger = logger;
        _callFunction = callFunction;
        _connectionString = Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString") ?? string.Empty;
    }

    public bool UpdateParticipantAsEligible(Participant participant, char isActive)
    {
        var oldParticipant = GetParticipant(participant.NhsNumber);

        var allRecordsToUpdate = UpdateOldRecords(int.Parse(oldParticipant.ParticipantId));
        return UpdateRecords(allRecordsToUpdate);
    }

    public async Task<bool> UpdateParticipantDetails(ParticipantCsvRecord participantCsvRecord)
    {
        var participantData = participantCsvRecord.Participant;
        var dateToday = DateTime.Today;
        var SQLToExecuteInOrder = new List<SQLReturnModel>();

        var oldParticipant = GetParticipant(participantData.NhsNumber);
        var response = await ValidateData(oldParticipant, participantData, participantCsvRecord.FileName);
        if (response.ExceptionFlag == "Y")
        {
            participantData = response;
        }

        var oldRecordsToEnd = EndOldRecords(int.Parse(oldParticipant.ParticipantId));
        if (oldRecordsToEnd.Count == 0)
        {
            return false;
        }

        foreach (var oldRecordsSQL in oldRecordsToEnd)
        {
            SQLToExecuteInOrder.Add(oldRecordsSQL);
        }

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
            { "@reasonForRemoval", _databaseHelper.CheckIfNumberNull(participantData.ReasonForRemoval) ? DBNull.Value : participantData.ReasonForRemoval},
            { "@reasonForRemovalDate", _databaseHelper.CheckIfDateNull(participantData.ReasonForRemovalEffectiveFromDate) ? DBNull.Value : _databaseHelper.ParseDates(participantData.ReasonForRemovalEffectiveFromDate)},
            { "@businessRuleVersion", _databaseHelper.CheckIfDateNull(participantData.BusinessRuleVersion) ? DBNull.Value : _databaseHelper.ParseDates(participantData.BusinessRuleVersion)},
            { "@exceptionFlag",  _databaseHelper.ConvertNullToDbNull(participantData.ExceptionFlag) },
            { "@recordInsertDateTime", _databaseHelper.ConvertNullToDbNull(participantData.RecordUpdateDateTime) },
            { "@recordUpdateDateTime", dateToday },
        };

        SQLToExecuteInOrder.Add(new SQLReturnModel()
        {
            CommandType = CommandType.Scalar,
            SQL = insertParticipant,
            Parameters = null
        });
        return ExecuteBulkCommand(SQLToExecuteInOrder, commonParameters);
    }

    private bool ExecuteBulkCommand(List<SQLReturnModel> sqlCommands, Dictionary<string, object> commonParams)
    {
        var command = CreateCommand(commonParams);

        sqlCommands
            .Where(sqlCommand => sqlCommand.Parameters != null)
            .Select(sqlCommand => sqlCommand.Parameters)
            .ToList()
            .ForEach(parameters => AddParameters(parameters, command));

        var transaction = BeginTransaction();
        command.Transaction = transaction;

        try
        {
            long newParticipantPk = -1;
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
            _logger.LogError("An error occurred while updating records: {ex}", ex);
            return false;
        }
    }

    private List<SQLReturnModel> EndOldRecords(int oldId)
    {
        if (oldId <= 0)
        {
            return new List<SQLReturnModel>();
        }

        return UpdateOldRecords(oldId);
    }

    private static List<SQLReturnModel> UpdateOldRecords(int participantId)
    {
        var recordUpdateTime = DateTime.Now;
        var listToReturn = new List<SQLReturnModel>()
        {
            new()
            {
                CommandType = CommandType.Command,
                SQL = " UPDATE [dbo].[PARTICIPANT_MANAGEMENT] " +
                " SET RECORD_UPDATE_DATETIME = @recordEndDateOldRecords " +
                " WHERE PARTICIPANT_ID = @participantId ",
                Parameters = new Dictionary<string, object>
                {
                    {"@participantId", participantId },
                    {"@recordEndDateOldRecords", recordUpdateTime }
                }
            }
        };
        return listToReturn;
    }

    public Participant GetParticipant(string NhsNumber)
    {
        var SQL = "SELECT " +
            "[PARTICIPANT_MANAGEMENT].[PARTICIPANT_ID], " +
            "[PARTICIPANT_MANAGEMENT].[SCREENING_ID], " +
            "[PARTICIPANT_MANAGEMENT].[NHS_NUMBER], " +
            "[PARTICIPANT_MANAGEMENT].[REASON_FOR_REMOVAL], " +
            "[PARTICIPANT_MANAGEMENT].[REASON_FOR_REMOVAL_DT], " +
            "[PARTICIPANT_MANAGEMENT].[BUSINESS_RULE_VERSION], " +
            "[PARTICIPANT_MANAGEMENT].[EXCEPTION_FLAG], " +
            "[PARTICIPANT_MANAGEMENT].[RECORD_INSERT_DATETIME], " +
            "[PARTICIPANT_MANAGEMENT].[RECORD_UPDATE_DATETIME] " +
        "FROM [dbo].[PARTICIPANT_MANAGEMENT] " +
        "WHERE [PARTICIPANT_MANAGEMENT].[NHS_NUMBER] = @NhsNumber";

        var parameters = new Dictionary<string, object>
        {
            {"@NhsNumber", NhsNumber }
        };

        var command = CreateCommand(parameters);
        command.CommandText = SQL;

        return GetParticipant(command);
    }

    public Participant GetParticipant(IDbCommand command)
    {
        return ExecuteQuery(command, reader =>
        {
            var participant = new Participant();
            while (reader.Read())
            {
                participant.ParticipantId = reader["PARTICIPANT_ID"] == DBNull.Value ? "-1" : reader["PARTICIPANT_ID"].ToString();
                participant.ScreeningId = reader["SCREENING_ID"] == DBNull.Value ? null : reader["SCREENING_ID"].ToString();
                participant.NhsNumber = reader["NHS_NUMBER"] == DBNull.Value ? null : reader["NHS_NUMBER"].ToString();
                participant.ReasonForRemoval = reader["REASON_FOR_REMOVAL"] == DBNull.Value ? null : reader["REASON_FOR_REMOVAL"].ToString();
                participant.ReasonForRemovalEffectiveFromDate = reader["REASON_FOR_REMOVAL_DT"] == DBNull.Value ? null : reader["REASON_FOR_REMOVAL_DT"].ToString();
                participant.BusinessRuleVersion = reader["BUSINESS_RULE_VERSION"] == DBNull.Value ? null : reader["BUSINESS_RULE_VERSION"].ToString();
                participant.ExceptionFlag = reader["EXCEPTION_FLAG"] == DBNull.Value ? null : reader["EXCEPTION_FLAG"].ToString();
                participant.RecordInsertDateTime = reader["RECORD_INSERT_DATETIME"] == DBNull.Value ? null : reader["RECORD_INSERT_DATETIME"].ToString();
                participant.RecordUpdateDateTime = reader["RECORD_UPDATE_DATETIME"] == DBNull.Value ? null : reader["RECORD_UPDATE_DATETIME"].ToString();
            }
            return participant;
        });
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
        catch (Exception EX)
        {
            _logger.LogError("an error happened, {EX}", EX);
            return false;
        }

        return true;
    }

    private long ExecuteCommandAndGetId(string SQL, IDbCommand command, IDbTransaction transaction)
    {
        command.Transaction = transaction;
        long newParticipantPk = -1;

        try
        {
            command.CommandText = SQL;
            _logger.LogInformation($"{SQL}");

            command.ExecuteNonQuery();
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
            _logger.LogError($"an error happened: {ex.Message}");
            return -1;
        }
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

    private bool UpdateRecords(List<SQLReturnModel> sqlToExecute)
    {
        var command = CreateCommand(sqlToExecute[0].Parameters);
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
