namespace Data.Database;

using System.Data;
using Microsoft.Extensions.Logging;
using Model;
using Common;
using System.Text.Json;
using System.Net;
using Model.Enums;

public class GetParticipantData : IGetParticipantData
{
    private readonly IDbConnection _dbConnection;
    private readonly IDatabaseHelper _databaseHelper;
    private readonly string _connectionString;
    private readonly ILogger<GetParticipantData> _logger;
    private readonly ICallFunction _callFunction;

    public GetParticipantData(IDbConnection IdbConnection, IDatabaseHelper databaseHelper, ILogger<GetarticipantData> logger, ICallFunction callFunction)
    {
        _dbConnection = IdbConnection;
        _databaseHelper = databaseHelper;
        _logger = logger;
        _callFunction = callFunction;
        _connectionString = Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString") ?? string.Empty;
    }

    public bool GetParticipantAsEligible(Participant participant, char isActive)
    {
        var participantMng = GetParticipant(participant.NhsNumber);
    }

    public Participant GetParticipant(string NhsNumber)
    {
        var SQL = "SELECT " +
            "[PARTICIPANT_MANAGEMENT].[PARTICIPANT_ID], " +
            "[PARTICIPANT_MANAGEMENT].[NHS_NUMBER], " +
            "[PARTICIPANT_MANAGEMENT].[SCREENING_ID], " +
            "[PARTICIPANT_MANAGEMENT].[REASON_FOR_REMOVAL], " +
            "[PARTICIPANT_MANAGEMENT].[REASON_FOR_REMOVAL_DT], " +
            "[PARTICIPANT_MANAGEMENT].[BUSINESS_RULE_VERSION], " +
            "[PARTICIPANT_MANAGEMENT].[EXCEPTION_FLAG], " +
            "[PARTICIPANT_MANAGEMENT].[RECORD_INSERT_DATETIME], " +
            "[PARTICIPANT_MANAGEMENT].[RECORD_UPDATE_DATETIME], " +
        "FROM [dbo].[PARTICIPANT_MANAGEMENT] " +
        "WHERE [PARTICIPANT_MANAGEMENT].[NHS_NUMBER] = @NhsNumber AND [PARTICIPANT_MANAGEMENT].[ACTIVE_FLAG] = @IsActive ";

        var parameters = new Dictionary<string, object>
        {
            {"@IsActive", 'Y' },
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
                participant.NhsNumber = reader["NHS_NUMBER"] == DBNull.Value ? null : reader["NHS_NUMBER"].ToString();
                participant.SupersededByNhsNumber = reader["SUPERSEDED_BY_NHS_NUMBER"] == DBNull.Value ? null : reader["SUPERSEDED_BY_NHS_NUMBER"].ToString();
                participant.PrimaryCareProvider = reader["PRIMARY_CARE_PROVIDER"] == DBNull.Value ? null : reader["PRIMARY_CARE_PROVIDER"].ToString();
                participant.NamePrefix = reader["PARTICIPANT_PREFIX"] == DBNull.Value ? null : reader["PARTICIPANT_PREFIX"].ToString();
                participant.FirstName = reader["PARTICIPANT_FIRST_NAME"] == DBNull.Value ? null : reader["PARTICIPANT_FIRST_NAME"].ToString();
                participant.OtherGivenNames = reader["OTHER_NAME"] == DBNull.Value ? null : reader["OTHER_NAME"].ToString();
                participant.Surname = reader["PARTICIPANT_LAST_NAME"] == DBNull.Value ? null : reader["PARTICIPANT_LAST_NAME"].ToString();
                participant.DateOfBirth = reader["PARTICIPANT_BIRTH_DATE"] == DBNull.Value ? null : reader["PARTICIPANT_BIRTH_DATE"].ToString();
                participant.Gender = reader["PARTICIPANT_GENDER"] == DBNull.Value ? Gender.NotKnown : (Gender)(int)reader["PARTICIPANT_GENDER"];
                participant.ReasonForRemoval = reader["REASON_FOR_REMOVAL_CD"] == DBNull.Value ? null : reader["REASON_FOR_REMOVAL_CD"].ToString();
                participant.ReasonForRemovalEffectiveFromDate = reader["REMOVAL_DATE"] == DBNull.Value ? null : reader["REMOVAL_DATE"].ToString();
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

    private async Task<bool> ValidateData(Participant existingParticipant, Participant newParticipant, string fileName)
    {
        var json = JsonSerializer.Serialize(new LookupValidationRequestBody(existingParticipant, newParticipant, fileName));

        try
        {
            var response = await _callFunction.SendPost(Environment.GetEnvironmentVariable("LookupValidationURL"), json);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Lookup validation failed.\nMessage: {ex.Message}\nParticipant: {ex.StackTrace}");
            return false;
        }

        return false;
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
