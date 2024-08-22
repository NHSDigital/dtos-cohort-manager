namespace Data.Database;

using System.Data;
using Microsoft.Extensions.Logging;
using Model;
using Common;
using System.Text.Json;
using System.Net;
using NHS.CohortManager.CohortDistribution;

public class ParticipantManagerData : IParticipantManagerData
{
    private readonly IDbConnection _dbConnection;
    private readonly IDatabaseHelper _databaseHelper;
    private readonly string _connectionString;
    private readonly ILogger<ParticipantManagerData> _logger;
    private readonly ICallFunction _callFunction;

    public ParticipantManagerData(IDbConnection IdbConnection, IDatabaseHelper databaseHelper, ILogger<ParticipantManagerData> logger, ICallFunction callFunction)
    {
        _dbConnection = IdbConnection;
        _databaseHelper = databaseHelper;
        _logger = logger;
        _callFunction = callFunction;
        _connectionString = Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString") ?? string.Empty;
    }

    #region Update methods
    public bool UpdateParticipantAsEligible(Participant participant, char isActive)
    {
        try
        {
            var Participant = GetParticipant(participant.NhsNumber);

            var recordUpdateTime = DateTime.Now;

            var SQL = " UPDATE [dbo].[PARTICIPANT_MANAGEMENT] " +
                " SET RECORD_UPDATE_DATETIME = @recordEndDateOldRecords " +
                " WHERE PARTICIPANT_ID = @participantId ";

            var Parameters = new Dictionary<string, object>
            {
                {"@participantId", Participant.ParticipantId },
                {"@recordEndDateOldRecords", recordUpdateTime }
            };

            return ExecuteCommand(SQL, Parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError("{MessageType} UpdateParticipantAsEligible failed.\nMessage:{ExMessage}\nStack Trace: {ExStackTrace}", ex.GetType().Name, ex.Message, ex.StackTrace);
            return false;
        }
    }

    public async Task<bool> UpdateParticipantDetails(ParticipantCsvRecord participantCsvRecord)
    {
        try
        {
            var participantData = participantCsvRecord.Participant;
            var dateToday = DateTime.Now;

            var oldParticipant = GetParticipant(participantData.NhsNumber);

            var response = await ValidateData(oldParticipant, participantData, participantCsvRecord.FileName);
            if (response.ExceptionFlag == "Y")
            {
                participantData = response;
            }

            string insertParticipant = "UPDATE [dbo].[PARTICIPANT_MANAGEMENT] SET " +
                " REASON_FOR_REMOVAL = @reasonForRemoval, " +
                " RECORD_TYPE = @recordType, " +
                " REASON_FOR_REMOVAL_DT = @reasonForRemovalDate, " +
                " BUSINESS_RULE_VERSION = @businessRuleVersion, " +
                " EXCEPTION_FLAG = @exceptionFlag, " +
                " RECORD_INSERT_DATETIME = @recordInsertDateTime," +
                " RECORD_UPDATE_DATETIME = @recordUpdateDateTime " +
                " WHERE SCREENING_ID = @screeningId " +
                " AND NHS_NUMBER = @NHSNumber";

            var commonParameters = new Dictionary<string, object>
            {
                { "@screeningId", _databaseHelper.CheckIfNumberNull(participantData.ScreeningId) ? DBNull.Value : participantData.ScreeningId},
                { "@recordType", _databaseHelper.CheckIfNumberNull(participantData.RecordType)  ? DBNull.Value : participantData.RecordType},
                { "@NHSNumber", _databaseHelper.CheckIfNumberNull(participantData.NhsNumber)  ? DBNull.Value : participantData.NhsNumber},
                { "@reasonForRemoval", _databaseHelper.ConvertNullToDbNull(participantData.ReasonForRemoval)},
                { "@reasonForRemovalDate", _databaseHelper.CheckIfDateNull(participantData.ReasonForRemovalEffectiveFromDate) ? DBNull.Value : _databaseHelper.ParseDates(participantData.ReasonForRemovalEffectiveFromDate)},
                { "@businessRuleVersion", _databaseHelper.CheckIfDateNull(participantData.BusinessRuleVersion) ? DBNull.Value : _databaseHelper.ParseDates(participantData.BusinessRuleVersion)},
                { "@exceptionFlag",  _databaseHelper.ParseExceptionFlag(_databaseHelper.ConvertNullToDbNull(participantData.ExceptionFlag)) },
                { "@recordInsertDateTime", dateToday },
                { "@recordUpdateDateTime", DBNull.Value },
            };

            return ExecuteCommand(insertParticipant, commonParameters);
        }
        catch (Exception ex)
        {
            _logger.LogError("{MessageType} UpdateParticipantDetails failed.\nMessage:{ExMessage}\nStack Trace: {ExStackTrace}", ex.GetType().Name, ex.Message, ex.StackTrace);
            return false;
        }
    }
    #endregion

    #region  get methods
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

        return GetParticipantWithScreeningName(command, false);
    }

    public Participant GetParticipantFromIDAndScreeningService(RetrieveParticipantRequestBody retrieveParticipantRequestBody)
    {
        var SQL = " SELECT TOP (1) * " +
        " FROM [PARTICIPANT_MANAGEMENT] AS P " +
        " JOIN SCREENING_LKP AS SLPK ON P.SCREENING_ID = SLPK.SCREENING_ID " +
        " WHERE P.[NHS_NUMBER] = @NhsNumber AND P.[SCREENING_ID] = @ScreeningId  AND SLPK.SCREENING_ID = @ScreeningId " +
        " ORDER BY PARTICIPANT_ID DESC ";

        var parameters = new Dictionary<string, object>
        {
            {"@NhsNumber", retrieveParticipantRequestBody.NhsNumber },
            {"@ScreeningId", retrieveParticipantRequestBody.ScreeningService }
        };

        var command = CreateCommand(parameters);
        command.CommandText = SQL;

        return GetParticipantWithScreeningName(command, true);
    }
    #endregion

    #region private methods

    private Participant GetParticipantWithScreeningName(IDbCommand command, bool withScreeningName)
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
                participant.ScreeningAcronym = withScreeningName ? (reader["SCREENING_ACRONYM"] == DBNull.Value ? null : reader["SCREENING_ACRONYM"].ToString()) : null;
                participant.ScreeningName = withScreeningName ? (reader["SCREENING_NAME"] == DBNull.Value ? null : reader["SCREENING_NAME"].ToString()) : null;
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

    private bool ExecuteCommand(string sqlCommandText, Dictionary<string, object> commonParams)
    {
        var command = CreateCommand(commonParams);
        command.Transaction = BeginTransaction();
        command.CommandText = sqlCommandText;

        try
        {
            var result = command.ExecuteNonQuery();
            _logger.LogInformation(result.ToString());

            if (result == 0)
            {
                command.Transaction.Rollback();
                _dbConnection.Close();
                return false;
            }

            command.Transaction.Commit();
            _dbConnection.Close();
            return true;
        }
        catch (Exception ex)
        {
            command.Transaction.Rollback();
            _dbConnection.Close();
            _logger.LogError("{MessageType} ExecuteCommand failed.\nMessage:{ExMessage}\nStack Trace: {ExStackTrace}", ex.GetType().Name, ex.Message, ex.StackTrace);
            return false;
        }
    }

    private async Task<Participant> ValidateData(Participant existingParticipant, Participant newParticipant, string fileName)
    {
        var json = JsonSerializer.Serialize(new LookupValidationRequestBody(existingParticipant, newParticipant, fileName, Model.Enums.RulesType.ParticipantManagement));

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
    #endregion
}
