namespace Data.Database;

using System.Data;
using Microsoft.Extensions.Logging;
using Model;
using Common;
using System.Text.Json;
using System.Net;
using Model.Enums;

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

        var allRecordsToUpdate = UpdateOldRecords(int.Parse(oldParticipant.ParticipantId), isActive);
        return UpdateRecords(allRecordsToUpdate);
    }

    public async Task<bool> UpdateParticipantDetails(ParticipantCsvRecord participantCsvRecord)
    {
        var participantData = participantCsvRecord.Participant;

        var cohortId = 1;

        var dateToday = DateTime.Today;
        var maxEndDate = DateTime.MaxValue;

        var SQLToExecuteInOrder = new List<SQLReturnModel>();

        var oldParticipant = GetParticipant(participantData.NhsNumber);
        if (!await ValidateData(oldParticipant, participantData, participantCsvRecord.FileName))
        {
            return false;
        }

        // End all old records ready for new ones to be created
        var oldRecordsToEnd = EndOldRecords(int.Parse(oldParticipant.ParticipantId));
        if (oldRecordsToEnd.Count == 0)
        {
            return false;
        }

        foreach (var oldRecordsSQL in oldRecordsToEnd)
        {
            SQLToExecuteInOrder.Add(oldRecordsSQL);
        }

        string insertParticipant = "INSERT INTO [dbo].[PARTICIPANT] ( " +
            " COHORT_ID, " +
            " GENDER_CD," +
            " NHS_NUMBER," +
            " SUPERSEDED_BY_NHS_NUMBER," +
            " PARTICIPANT_BIRTH_DATE," +
            " PARTICIPANT_DEATH_DATE," +
            " PARTICIPANT_PREFIX," +
            " PARTICIPANT_FIRST_NAME," +
            " PARTICIPANT_LAST_NAME," +
            " OTHER_NAME," +
            " GP_CONNECT," +
            " PRIMARY_CARE_PROVIDER," +
            " REASON_FOR_REMOVAL_CD," +
            " REMOVAL_DATE," +
            " RECORD_START_DATE," +
            " RECORD_END_DATE," +
            " ACTIVE_FLAG, " +
            " LOAD_DATE " +
            " ) VALUES( " +
            " @cohortId, " +
            " @gender, " +
            " @NHSNumber, " +
            " @supersededByNhsNumber, " +
            " @dateOfBirth, " +
            " @dateOfDeath, " +
            " @namePrefix, " +
            " @firstName, " +
            " @surname, " +
            " @otherGivenNames, " +
            " @gpConnect, " +
            " @primaryCareProvider, " +
            " @reasonForRemoval, " +
            " @RemovalDate, " +
            " @RecordStartDate, " +
            " @RecordEndDate, " +
            " @ActiveFlag, " +
            " @LoadDate " +
            " ) ";

        var commonParameters = new Dictionary<string, object>
        {
            {"@cohortId", cohortId},
            {"@gender", participantData.Gender},
            {"@NHSNumber", participantData.NhsNumber },
            {"@supersededByNhsNumber", _databaseHelper.CheckIfNumberNull(participantData.SupersededByNhsNumber) ? DBNull.Value : participantData.SupersededByNhsNumber},
            {"@dateOfBirth", _databaseHelper.CheckIfDateNull(participantData.DateOfBirth) ? DateTime.MaxValue : _databaseHelper.ParseDates(participantData.DateOfBirth)},
            { "@dateOfDeath", _databaseHelper.CheckIfDateNull(participantData.DateOfDeath) ? DBNull.Value : _databaseHelper.ParseDates(participantData.DateOfDeath)},
            { "@namePrefix",  _databaseHelper.ConvertNullToDbNull(participantData.NamePrefix) },
            { "@firstName", _databaseHelper.ConvertNullToDbNull(participantData.FirstName) },
            { "@surname", _databaseHelper.ConvertNullToDbNull(participantData.Surname) },
            { "@otherGivenNames", _databaseHelper.ConvertNullToDbNull(participantData.OtherGivenNames) },
            { "@gpConnect", _databaseHelper.ConvertNullToDbNull(participantData.PrimaryCareProvider) },
            { "@primaryCareProvider", _databaseHelper.ConvertNullToDbNull(participantData.PrimaryCareProvider) },
            { "@reasonForRemoval", _databaseHelper.ConvertNullToDbNull(participantData.ReasonForRemoval) },
            { "@removalDate", _databaseHelper.CheckIfDateNull(participantData.ReasonForRemovalEffectiveFromDate) ? DBNull.Value : _databaseHelper.ParseDateToString(participantData.ReasonForRemovalEffectiveFromDate)},
            { "@RecordStartDate", dateToday},
            { "@RecordEndDate", maxEndDate},
            { "@ActiveFlag", 'Y'},
            { "@LoadDate", dateToday },
        };

        // Common params already contains all the parameters we need for this
        SQLToExecuteInOrder.Add(new SQLReturnModel()
        {
            CommandType = CommandType.Scalar,
            SQL = insertParticipant,
            Parameters = null
        });

        SQLToExecuteInOrder.Add(AddNewAddress(participantData));
        SQLToExecuteInOrder.Add(InsertContactPreference(participantData));

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
            var newParticipantPk = -1;
            foreach (var sqlCommand in sqlCommands)
            {

                if (sqlCommand.CommandType == CommandType.Scalar)
                {
                    // When the new participant ID has been created as a scalar we can get back the new participant ID
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
        // We don't want to get a record that is less than 0 or 0 as records start 1
        if (oldId <= 0)
        {
            return new List<SQLReturnModel>();
        }

        return UpdateOldRecords(oldId, 'N');
    }

    private List<SQLReturnModel> UpdateOldRecords(int ParticipantId, char IsActive)
    {
        var recordEndDate = DateTime.Today;
        if (IsActive == 'Y')
        {
            recordEndDate = DateTime.MaxValue;
        }

        var listToReturn = new List<SQLReturnModel>()
        {
            new SQLReturnModel()
            {
                CommandType = CommandType.Command,
                SQL = " UPDATE [dbo].[ADDRESS] " +
                    " SET RECORD_END_DATE = @recordEndDateOldRecords, " +
                    " ACTIVE_FLAG = @IsActiveOldRecords " +
                    " WHERE PARTICIPANT_ID = @ParticipantIdOld  ",
                // We don't need to add params to all items as we don't want to duplicate them
                Parameters = new Dictionary<string, object>
                {
                    {"@recordEndDateOldRecords", recordEndDate},
                    {"@ParticipantIdOld", ParticipantId},
                    {"@IsActiveOldRecords", IsActive},
                },
            },
            new SQLReturnModel()
            {
                CommandType = CommandType.Command,
                SQL = " UPDATE [dbo].[PARTICIPANT] " +
                " SET RECORD_END_DATE = @recordEndDateOldRecords, " +
                " ACTIVE_FLAG = @IsActiveOldRecords " +
                " WHERE PARTICIPANT_ID = @ParticipantIdOld ",
                Parameters = null,
            },
            new SQLReturnModel()
            {
                CommandType = CommandType.Command,
                SQL = " UPDATE [dbo].[CONTACT_PREFERENCE] " +
                " SET RECORD_END_DATE = @recordEndDateOldRecords, " +
                " ACTIVE_FLAG = @IsActiveOldRecords " +
                " WHERE PARTICIPANT_ID = @ParticipantIdOld ",
                Parameters = null
            }
        };
        return listToReturn;
    }

    public Participant GetParticipant(string NhsNumber)
    {
        var SQL = "SELECT " +
            "[PARTICIPANT].[PARTICIPANT_ID], " +
            "[PARTICIPANT].[NHS_NUMBER], " +
            "[PARTICIPANT].[SUPERSEDED_BY_NHS_NUMBER], " +
            "[PARTICIPANT].[PRIMARY_CARE_PROVIDER], " +
            "[PARTICIPANT].[GP_CONNECT], " +
            "[PARTICIPANT].[PARTICIPANT_PREFIX], " +
            "[PARTICIPANT].[PARTICIPANT_FIRST_NAME], " +
            "[PARTICIPANT].[OTHER_NAME], " +
            "[PARTICIPANT].[PARTICIPANT_LAST_NAME], " +
            "[PARTICIPANT].[PARTICIPANT_BIRTH_DATE], " +
            "[PARTICIPANT].[PARTICIPANT_GENDER], " +
            "[PARTICIPANT].[REASON_FOR_REMOVAL_CD], " +
            "[PARTICIPANT].[REMOVAL_DATE], " +
            "[PARTICIPANT].[PARTICIPANT_DEATH_DATE], " +
            "[ADDRESS].[ADDRESS_LINE_1], " +
            "[ADDRESS].[ADDRESS_LINE_2], " +
            "[ADDRESS].[CITY], " +
            "[ADDRESS].[COUNTY], " +
            "[ADDRESS].[POST_CODE] " +
        "FROM [dbo].[PARTICIPANT] " +
        "INNER JOIN [dbo].[ADDRESS] ON [PARTICIPANT].[PARTICIPANT_ID]=[ADDRESS].[PARTICIPANT_ID] " +
        "WHERE [PARTICIPANT].[NHS_NUMBER] = @NhsNumber AND [PARTICIPANT].[ACTIVE_FLAG] = @IsActive AND [ADDRESS].[ACTIVE_FLAG] = @IsActive";

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
                participant.DateOfDeath = reader["PARTICIPANT_DEATH_DATE"] == DBNull.Value ? null : reader["PARTICIPANT_DEATH_DATE"].ToString();
                participant.AddressLine1 = reader["ADDRESS_LINE_1"] == DBNull.Value ? null : reader["ADDRESS_LINE_1"].ToString();
                participant.AddressLine2 = reader["ADDRESS_LINE_2"] == DBNull.Value ? null : reader["ADDRESS_LINE_2"].ToString();
                participant.AddressLine3 = reader["CITY"] == DBNull.Value ? null : reader["CITY"].ToString();
                participant.AddressLine4 = reader["COUNTY"] == DBNull.Value ? null : reader["COUNTY"].ToString();
                participant.AddressLine5 = reader["POST_CODE"] == DBNull.Value ? null : reader["POST_CODE"].ToString();
            }
            return participant;
        });
    }

    private SQLReturnModel AddNewAddress(Participant participantData)
    {
        string updateAddress =
        " INSERT INTO dbo.ADDRESS " +
        " ( PARTICIPANT_ID," +
        " ADDRESS_TYPE, " +
        " ADDRESS_LINE_1, " +
        " ADDRESS_LINE_2, " +
        " CITY, " +
        " COUNTY,  " +
        " POST_CODE,  " +
        " LSOA,  " +
        " RECORD_START_DATE,  " +
        " RECORD_END_DATE, " +
        " ACTIVE_FLAG,  " +
        " LOAD_DATE)  " +
        " VALUES  " +
        " ( @NewParticipantId, " +
        " null, " +
        " @addressLine1, " +
        " @addressLine2, " +
        " null, " +
        " null, " +
        " null, " +
        " null, " +
        " @RecordStartDate,  " +
        " @RecordEndDate, " +
        " @ActiveFlag, " +
        " @LoadDate)";

        var parameters = new Dictionary<string, object>()
        {
            { "@addressLine1", participantData.AddressLine1 },
            { "@addressLine2", participantData.AddressLine2 },
        };

        return new SQLReturnModel()
        {
            CommandType = CommandType.Command,
            SQL = updateAddress,
            Parameters = parameters
        };
    }

    private SQLReturnModel InsertContactPreference(Participant participantData)
    {
        string insertContactPreference = "INSERT INTO CONTACT_PREFERENCE (PARTICIPANT_ID, CONTACT_METHOD, PREFERRED_LANGUAGE, IS_INTERPRETER_REQUIRED, TELEPHONE_NUMBER, MOBILE_NUMBER, EMAIL_ADDRESS, RECORD_START_DATE, RECORD_END_DATE, ACTIVE_FLAG, LOAD_DATE)" +
        "VALUES (@NewParticipantId, @contactMethod, @preferredLanguage, @isInterpreterRequired, @telephoneNumber, @mobileNumber, @emailAddress, @RecordStartDate, @RecordEndDate, @ActiveFlag, @LoadDate)";

        var parameters = new Dictionary<string, object>
        {
            {"@contactMethod", DBNull.Value},
            {"@preferredLanguage", participantData.PreferredLanguage},
            {"@isInterpreterRequired", string.IsNullOrEmpty(participantData.IsInterpreterRequired) ? "0" : "1"},
            {"@telephoneNumber",  _databaseHelper.CheckIfNumberNull(participantData.TelephoneNumber) ? DBNull.Value : participantData.TelephoneNumber},
            {"@mobileNumber", DBNull.Value},
            {"@emailAddress", _databaseHelper.ConvertNullToDbNull(participantData.EmailAddress)},
        };

        return new SQLReturnModel()
        {
            CommandType = CommandType.Command,
            SQL = insertContactPreference,
            Parameters = parameters
        };
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

    private int ExecuteCommandAndGetId(string SQL, IDbCommand command, IDbTransaction transaction)
    {
        command.Transaction = transaction;
        var newParticipantPk = -1;

        try
        {
            command.CommandText = SQL;
            _logger.LogInformation($"{SQL}");

            command.ExecuteNonQuery();
            var SQLGet = $"SELECT PARTICIPANT_ID FROM [dbo].[PARTICIPANT] WHERE NHS_NUMBER = @NHSNumber AND ACTIVE_FLAG = @ActiveFlag ";

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

    private async Task<bool> ValidateData(Participant existingParticipant, Participant newParticipant, string fileName)
    {
        var json = JsonSerializer.Serialize(new
        {
            ExistingParticipant = existingParticipant,
            NewParticipant = newParticipant,
            Workflow = "UpdateParticipant",
            FileName = fileName
        });

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
