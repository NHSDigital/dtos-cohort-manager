namespace Data.Database;

using System.Data;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

using Model;

public class CreateParticipantData : ICreateParticipantData
{
    private IDbConnection _dbConnection;
    private IDatabaseHelper _databaseHelper;
    private string connectionString;
    private readonly ILogger<CreateParticipantData> _logger;

    public CreateParticipantData(IDbConnection dbConnection, IDatabaseHelper databaseHelper, ILogger<CreateParticipantData> logger)
    {
        _dbConnection = dbConnection;
        _databaseHelper = databaseHelper;
        _logger = logger;
        connectionString = Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString") ?? string.Empty;
    }

    public bool CreateParticipantEntry(ParticipantCsvRecord participantCsvRecord)
    {
        string cohort_id = "1";
        string active = "Y";
        DateTime date_today = DateTime.Today;
        DateTime max_end_date = DateTime.MaxValue;
        var sqlToExecuteInOrder = new List<SQLReturnModel>();
        var participantData = participantCsvRecord.Participant;

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
            { "@dateOfDeath", _databaseHelper.CheckIfDateNull(participantData.DateOfDeath) ? DBNull.Value : _databaseHelper.parseDates(participantData.DateOfDeath)},
            { "@namePrefix",  _databaseHelper.ConvertNullToDbNull(participantData.NamePrefix) },
            { "@firstName", _databaseHelper.ConvertNullToDbNull(participantData.FirstName) },
            { "@surname", _databaseHelper.ConvertNullToDbNull(participantData.Surname) },
            { "@otherGivenNames", _databaseHelper.ConvertNullToDbNull(participantData.OtherGivenNames) },
            { "@gpConnect", _databaseHelper.ConvertNullToDbNull(participantData.PrimaryCareProvider) },
            { "@primaryCareProvider", _databaseHelper.ConvertNullToDbNull(participantData.PrimaryCareProvider) },
            { "@reasonForRemoval", _databaseHelper.ConvertNullToDbNull(participantData.ReasonForRemoval) },
            { "@removalDate", _databaseHelper.CheckIfDateNull(participantData.ReasonForRemovalEffectiveFromDate) ? DBNull.Value : _databaseHelper.parseDates(participantData.ReasonForRemovalEffectiveFromDate)},
            { "@RecordStartDate", date_today},
            { "@RecordEndDate", max_end_date},
            { "@ActiveFlag", 'Y'},
            { "@LoadDate", date_today },

        };

        sqlToExecuteInOrder.Add(new SQLReturnModel()
        {
            commandType = CommandType.Scalar,
            SQL = insertParticipant,
            parameters = null
        });

        sqlToExecuteInOrder.Add(AddNewAddress(participantData));
        sqlToExecuteInOrder.Add(InsertContactPreference(participantData));

        return ExecuteBulkCommand(sqlToExecuteInOrder, commonParameters, participantData.NHSId);
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
            {"@emailAddress", _databaseHelper.ConvertNullToDbNull(participantData.EmailAddress)},
        };

        return new SQLReturnModel()
        {
            commandType = CommandType.Command,
            SQL = insertContactPreference,
            parameters = parameters
        };
    }
}
