namespace Data.Database;
using System.Data;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using Model;

public class UpdateParticipantData : IUpdateParticipantData
{
    private IDbConnection _dbConnection;
    private IDatabaseHelper _databaseHelper;
    private readonly string connectionString;
    private readonly ILogger<UpdateParticipantData> _logger;

    public UpdateParticipantData(IDbConnection IdbConnection, IDatabaseHelper databaseHelper, ILogger<UpdateParticipantData> logger)
    {
        _dbConnection = IdbConnection;
        _databaseHelper = databaseHelper;
        _logger = logger;
        connectionString = Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString");
    }

    public bool UpdateParticipantAsEligible(Participant participant, char isActive)
    {
        var allRecordsToUpdate = UpdateOldRecords(GetParticipantId(participant.NHSId), isActive);
        return UpdateRecords(allRecordsToUpdate);
    }

    public bool UpdateParticipantDetails(Participant participantData)
    {
        var cohort_id = 1;

        var date_today = DateTime.Today;
        var max_end_date = DateTime.MaxValue;

        var SQLToExecuteInOrder = new List<SQLReturnModel>();


        //end all old records ready for new ones to be created
        var oldId = GetParticipantId(participantData.NHSId);

        var oldRecordsToEnd = EndOldRecords(participantData, oldId);
        if (oldRecordsToEnd == null)
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
            " @cohort_id, " +
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
            {"@cohort_id", cohort_id},
            {"@gender", participantData.Gender},
            {"@NHSNumber", participantData.NHSId },
            {"@supersededByNhsNumber", _databaseHelper.CheckIfNumberNull(participantData.SupersededByNhsNumber) ? DBNull.Value : participantData.SupersededByNhsNumber},
            {"@dateOfBirth", _databaseHelper.parseDates(participantData.DateOfBirth)},
            { "@dateOfDeath", _databaseHelper.CheckIfDateNull(participantData.DateOfDeath) ? DBNull.Value : _databaseHelper.ParseDateToString(participantData.DateOfBirth)},
            { "@namePrefix",  _databaseHelper.ConvertNullToDbNull(participantData.NamePrefix) },
            { "@firstName", _databaseHelper.ConvertNullToDbNull(participantData.FirstName) },
            { "@surname", _databaseHelper.ConvertNullToDbNull(participantData.Surname) },
            { "@otherGivenNames", _databaseHelper.ConvertNullToDbNull(participantData.OtherGivenNames) },
            { "@gpConnect", _databaseHelper.ConvertNullToDbNull(participantData.GpConnect) },
            { "@primaryCareProvider", _databaseHelper.ConvertNullToDbNull(participantData.PrimaryCareProvider) },
            { "@reasonForRemoval", _databaseHelper.ConvertNullToDbNull(participantData.ReasonForRemoval) },
            { "@removalDate", _databaseHelper.CheckIfDateNull(participantData.ReasonForRemovalEffectiveFromDate) ? DBNull.Value : _databaseHelper.ParseDateToString(participantData.ReasonForRemovalEffectiveFromDate)},
            { "@RecordStartDate", date_today},
            { "@RecordEndDate", max_end_date},
            { "@ActiveFlag", 'Y'},
            { "@LoadDate", date_today },

        };

        //common params already contains all the parameters we need for this
        SQLToExecuteInOrder.Add(new SQLReturnModel()
        {
            commandType = CommandType.Scalar,
            SQL = insertParticipant,
            parameters = null
        });

        SQLToExecuteInOrder.Add(AddNewAddress(participantData));
        SQLToExecuteInOrder.Add(InsertContactPreference(participantData));

        return ExecuteBulkCommand(SQLToExecuteInOrder, commonParameters, participantData.NHSId);
    }

    private bool ExecuteBulkCommand(List<SQLReturnModel> sqlCommands, Dictionary<string, object> commonParams, string NHSId)
    {
        var command = CreateCommand(commonParams);
        foreach (var SqlCommand in sqlCommands)
        {
            if (SqlCommand.parameters != null)
            {
                AddParameters(SqlCommand.parameters, command);
            }
        }

        var transaction = BeginTransaction();
        command.Transaction = transaction;

        try
        {
            var newParticipantPk = -1;
            foreach (var sqlCommand in sqlCommands)
            {

                if (sqlCommand.commandType == CommandType.Scalar)
                {
                    //when the new participant ID has been created as a scalar we can get back the new participant ID
                    newParticipantPk = ExecuteCommandAndGetId(sqlCommand.SQL, command, transaction);
                    AddParameters(new Dictionary<string, object>()
                    {
                        {"@NewParticipantId", newParticipantPk }
                    }, command);
                }
                if (sqlCommand.commandType == CommandType.Command)
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

    private List<SQLReturnModel> EndOldRecords(Participant participantData, int oldId)
    {
        //We don't want to get a record that is less than 0 or 0 as records start 1
        if (oldId <= 0)
        {
            return null;
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
                commandType = CommandType.Command,
                SQL = " UPDATE [dbo].[ADDRESS] " +
                    " SET RECORD_END_DATE = @recordEndDateOldRecords, " +
                    " ACTIVE_FLAG = @IsActiveOldRecords " +
                    " WHERE PARTICIPANT_ID = @ParticipantIdOld  ",
                // we don't need to add params to all items as we don't want to duplicate them
                parameters = new Dictionary<string, object>
                {
                    {"@recordEndDateOldRecords", recordEndDate},
                    {"@ParticipantIdOld", ParticipantId},
                    {"@IsActiveOldRecords", IsActive},
                },
            },
            new SQLReturnModel()
            {
                commandType = CommandType.Command,
                SQL = " UPDATE [dbo].[PARTICIPANT] " +
                " SET RECORD_END_DATE = @recordEndDateOldRecords, " +
                " ACTIVE_FLAG = @IsActiveOldRecords " +
                " WHERE PARTICIPANT_ID = @ParticipantIdOld ",
                parameters = null,
            },
            new SQLReturnModel()
            {
                commandType = CommandType.Command,
                SQL = " UPDATE [dbo].[CONTACT_PREFERENCE] " +
                " SET RECORD_END_DATE = @recordEndDateOldRecords, " +
                " ACTIVE_FLAG = @IsActiveOldRecords " +
                " WHERE PARTICIPANT_ID = @ParticipantIdOld ",
                parameters = null
            }
        };
        return listToReturn;
    }

    public int GetParticipantId(string NHSId)
    {
        var SQL = $"SELECT PARTICIPANT_ID FROM [dbo].[PARTICIPANT] WHERE NHS_NUMBER = @NHSId AND ACTIVE_FLAG = @IsActive ";

        var parameters = new Dictionary<string, object>
        {
            {"@IsActive", 'Y' },
            {"@NHSId", NHSId }
        };

        var command = CreateCommand(parameters);
        command.CommandText = SQL;

        return ExecuteQuery(command);
    }

    private SQLReturnModel AddNewAddress(Participant participantData)
    {
        string updateAddress =
            " INSERT INTO dbo.ADDRESS " +
            " ( PARTICIPANT_ID," +
            " ADDRESS_TYPE, " +
            " ADDRESS_LINE_1,  " +
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
            commandType = CommandType.Command,
            SQL = updateAddress,
            parameters = parameters
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
            {"@isInterpreterRequired", participantData.IsInterpreterRequired.IsNullOrEmpty() ? "0" : "1"},
            {"@telephoneNumber",  _databaseHelper.CheckIfNumberNull(participantData.TelephoneNumber) ? DBNull.Value : participantData.TelephoneNumber},
            {"@mobileNumber", DBNull.Value},
            {"@emailAddress", participantData.EmailAddress},
        };

        return new SQLReturnModel()
        {
            commandType = CommandType.Command,
            SQL = insertContactPreference,
            parameters = parameters
        };
    }

    private int ExecuteQuery(IDbCommand command)
    {
        var id = 0;
        using (_dbConnection)
        {
            _dbConnection.ConnectionString = connectionString;
            _dbConnection.Open();
            using (command)
            {
                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        id = reader.GetInt32(0);
                    }
                }
                _dbConnection.Close();
            }
            return id;
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
