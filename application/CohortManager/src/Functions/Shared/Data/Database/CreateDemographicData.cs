namespace Data.Database;

using System.Data;
using Microsoft.Extensions.Logging;
using Model;
using Microsoft.Data.SqlClient;
using Model.Enums;

public class CreateDemographicData : ICreateDemographicData
{
    private readonly IDbConnection _dbConnection;
    private readonly IDatabaseHelper _databaseHelper;
    private readonly string _connectionString;
    private readonly ILogger<CreateDemographicData> _logger;

    public CreateDemographicData(IDbConnection IdbConnection, IDatabaseHelper databaseHelper, ILogger<CreateDemographicData> logger)
    {
        _dbConnection = IdbConnection;
        _databaseHelper = databaseHelper;
        _logger = logger;
        _connectionString = Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString");
    }

    public bool InsertDemographicData(Demographic demographic)
    {
        var command = new List<SQLReturnModel>()
        {
            new SQLReturnModel()
            {
                CommandType = CommandType.Command,
                SQL = "INSERT INTO [dbo].[PARTICIPANT_DEMOGRAPHIC] " +
                "(" +
                    "  [NHS_NUMBER] " +
                    ", [SUPERSEDED_BY_NHS_NUMBER] " +
                    ", [PRIMARY_CARE_PROVIDER] " +
                    ", [PRIMARY_CARE_PROVIDER_FROM_DT] " +
                    ", [CURRENT_POSTING] " +
                    ", [CURRENT_POSTING_FROM_DT] " +
                    ", [PREVIOUS_POSTING] " +
                    ", [PREV_POSTING_TO_DT] " +
                    ", [NAME_PREFIX] " +
                    ", [GIVEN_NAME] " +
                    ", [OTHER_GIVEN_NAME] " +
                    ", [FAMILY_NAME] " +
                    ", [PREVIOUS_FAMILY_NAME] " +
                    ", [DATE_OF_BIRTH] " +
                    ", [GENDER] " +
                    ", [ADDRESS_LINE_1] " +
                    ", [ADDRESS_LINE_2] " +
                    ", [ADDRESS_LINE_3] " +
                    ", [ADDRESS_LINE_4] " +
                    ", [ADDRESS_LINE_5] " +
                    ", [POST_CODE] " +
                    ", [PAF_KEY] " +
                    ", [USUAL_ADDRESS_FROM_DT] " +
                    ", [DATE_OF_DEATH] " +
                    ", [DEATH_STATUS] " +
                    ", [TELEPHONE_NUMBER_HOME] " +
                    ", [TELEPHONE_NUMBER_HOME_FROM_DT] " +
                    ", [TELEPHONE_NUMBER_MOB] " +
                    ", [TELEPHONE_NUMBER_MOB_FROM_DT] " +
                    ", [EMAIL_ADDRESS_HOME] " +
                    ", [EMAIL_ADDRESS_HOME_FROM_DT] " +
                    ", [PREFERRED_LANGUAGE] " +
                    ", [INTERPRETER_REQUIRED] " +
                    ", [INVALID_FLAG] " +
                    ", [RECORD_INSERT_DATE_TIME] " +
                    ", [RECORD_UPDATE_DATE_TIME] ) " +
                "VALUES " +
                "(" +
                    " @NHS_NUMBER, " +
                    " @SUPERSEDED_BY_NHS_NUMBER, " +
                    " @PRIMARY_CARE_PROVIDER, " +
                    " @PRIMARY_CARE_PROVIDER_FROM_DT, " +
                    " @CURRENT_POSTING, " +
                    " @CURRENT_POSTING_FROM_DT, " +
                    " @PREVIOUS_POSTING, " +
                    " @PREV_POSTING_TO_DT, " +
                    " @NAME_PREFIX, " +
                    " @GIVEN_NAME, " +
                    " @OTHER_GIVEN_NAME, " +
                    " @FAMILY_NAME," +
                    " @PREVIOUS_FAMILY_NAME, " +
                    " @DATE_OF_BIRTH, " +
                    " @GENDER, " +
                    " @ADDRESS_LINE_1, " +
                    " @ADDRESS_LINE_2, " +
                    " @ADDRESS_LINE_3, " +
                    " @ADDRESS_LINE_4, " +
                    " @ADDRESS_LINE_5, " +
                    " @POST_CODE, " +
                    " @PAF_KEY, " +
                    " @USUAL_ADDRESS_FROM_DT, " +
                    " @DATE_OF_DEATH, " +
                    " @DEATH_STATUS, " +
                    " @TELEPHONE_NUMBER_HOME, " +
                    " @TELEPHONE_NUMBER_HOME_FROM_DT, " +
                    " @TELEPHONE_NUMBER_MOB, " +
                    " @TELEPHONE_NUMBER_MOB_FROM_DT, " +
                    " @EMAIL_ADDRESS_HOME, " +
                    " @EMAIL_ADDRESS_HOME_FROM_DT," +
                    " @PREFERRED_LANGUAGE," +
                    " @INTERPRETER_REQUIRED," +
                    " @INVALID_FLAG," +
                    " @RECORD_INSERT_DATE_TIME," +
                    " @RECORD_UPDATE_DATE_TIME" +
                ")",
                Parameters = new Dictionary<string, object>
                {
                    {"@NHS_NUMBER", _databaseHelper.CheckIfNumberNull(demographic.NhsNumber) ? DBNull.Value : long.Parse(demographic.NhsNumber)},
                    {"@SUPERSEDED_BY_NHS_NUMBER", _databaseHelper.CheckIfNumberNull(demographic.SupersededByNhsNumber) ? DBNull.Value : long.Parse(demographic.SupersededByNhsNumber)},
                    {"@PRIMARY_CARE_PROVIDER", _databaseHelper.ConvertNullToDbNull(demographic.PrimaryCareProvider)},
                    {"@PRIMARY_CARE_PROVIDER_FROM_DT", string.IsNullOrEmpty(demographic.PrimaryCareProvider) ? DBNull.Value : _databaseHelper.ParseDates(demographic.PrimaryCareProviderEffectiveFromDate)},
                    {"@CURRENT_POSTING", _databaseHelper.ConvertNullToDbNull(demographic.CurrentPosting)},
                    {"@CURRENT_POSTING_FROM_DT", string.IsNullOrEmpty(demographic.CurrentPostingEffectiveFromDate) ? DBNull.Value : _databaseHelper.ParseDates(demographic.CurrentPostingEffectiveFromDate)},
                    {"@PREVIOUS_POSTING", _databaseHelper.ConvertNullToDbNull(demographic.PreviousPosting)},
                    {"@PREV_POSTING_TO_DT", string.IsNullOrEmpty(demographic.PreviousPostingEffectiveFromDate) ? DBNull.Value : _databaseHelper.ParseDates(demographic.PreviousPostingEffectiveFromDate)},
                    {"@NAME_PREFIX", _databaseHelper.ConvertNullToDbNull(demographic.NamePrefix)},
                    {"@GIVEN_NAME", _databaseHelper.ConvertNullToDbNull(demographic.FirstName)},
                    {"@OTHER_GIVEN_NAME", _databaseHelper.ConvertNullToDbNull(demographic.OtherGivenNames)},
                    {"@FAMILY_NAME", _databaseHelper.ConvertNullToDbNull(demographic.Surname)},
                    {"@PREVIOUS_FAMILY_NAME", _databaseHelper.ConvertNullToDbNull(demographic.PreviousSurname)},
                    {"@DATE_OF_BIRTH", string.IsNullOrEmpty(demographic.DateOfBirth) ? DBNull.Value : _databaseHelper.ParseDates(demographic.DateOfBirth)},
                    {"@GENDER", demographic.Gender.HasValue ? demographic.Gender : DBNull.Value},
                    {"@ADDRESS_LINE_1", _databaseHelper.ConvertNullToDbNull(demographic.AddressLine1)},
                    {"@ADDRESS_LINE_2", _databaseHelper.ConvertNullToDbNull(demographic.AddressLine2)},
                    {"@ADDRESS_LINE_3", _databaseHelper.ConvertNullToDbNull(demographic.AddressLine3)},
                    {"@ADDRESS_LINE_4", _databaseHelper.ConvertNullToDbNull(demographic.AddressLine4)},
                    {"@ADDRESS_LINE_5", _databaseHelper.ConvertNullToDbNull(demographic.AddressLine5)},
                    {"@POST_CODE", _databaseHelper.ConvertNullToDbNull(demographic.Postcode)},
                    {"@PAF_KEY", _databaseHelper.ConvertNullToDbNull(demographic.PafKey)},
                    {"@USUAL_ADDRESS_FROM_DT", string.IsNullOrEmpty(demographic.UsualAddressEffectiveFromDate) ? DBNull.Value : _databaseHelper.ParseDates(demographic.UsualAddressEffectiveFromDate)},
                    {"@DATE_OF_DEATH", string.IsNullOrEmpty(demographic.DateOfDeath) ? DBNull.Value : _databaseHelper.ParseDates(demographic.DateOfDeath)},
                    {"@DEATH_STATUS", demographic.DeathStatus.HasValue ? demographic.DeathStatus : DBNull.Value},
                    {"@TELEPHONE_NUMBER_HOME", _databaseHelper.ConvertNullToDbNull(demographic.TelephoneNumber)},
                    {"@TELEPHONE_NUMBER_HOME_FROM_DT", string.IsNullOrEmpty(demographic.TelephoneNumberEffectiveFromDate) ? DBNull.Value : _databaseHelper.ParseDates(demographic.TelephoneNumberEffectiveFromDate)},
                    {"@TELEPHONE_NUMBER_MOB", _databaseHelper.ConvertNullToDbNull(demographic.MobileNumber)},
                    {"@TELEPHONE_NUMBER_MOB_FROM_DT", string.IsNullOrEmpty(demographic.MobileNumberEffectiveFromDate) ? DBNull.Value : _databaseHelper.ParseDates(demographic.MobileNumberEffectiveFromDate)},
                    {"@EMAIL_ADDRESS_HOME", _databaseHelper.ConvertNullToDbNull(demographic.EmailAddress)},
                    {"@EMAIL_ADDRESS_HOME_FROM_DT", string.IsNullOrEmpty(demographic.EmailAddressEffectiveFromDate) ? DBNull.Value : _databaseHelper.ParseDates(demographic.EmailAddressEffectiveFromDate)},
                    {"@PREFERRED_LANGUAGE", _databaseHelper.ConvertNullToDbNull(demographic.PreferredLanguage)},
                    {"@INTERPRETER_REQUIRED", _databaseHelper.ConvertNullToDbNull(demographic.IsInterpreterRequired)},
                    {"@INVALID_FLAG", _databaseHelper.ConvertBoolStringToInt(demographic.InvalidFlag)},
                    {"@RECORD_INSERT_DATE_TIME", string.IsNullOrEmpty(demographic.RecordInsertDateTime) ? DBNull.Value : _databaseHelper.ParseDates(demographic.RecordInsertDateTime)},
                    {"@RECORD_UPDATE_DATE_TIME", string.IsNullOrEmpty(demographic.RecordUpdateDateTime) ? DBNull.Value : _databaseHelper.ParseDates(demographic.RecordUpdateDateTime)}
                },
            }
        };

        return UpdateRecords(command);
    }

    public Demographic GetDemographicData(string NhsNumber)
    {
        var SQL = @"SELECT * FROM [dbo].[PARTICIPANT_DEMOGRAPHIC] WHERE NHS_NUMBER = @NhsNumber";
        var parameters = new Dictionary<string, object>()
        {
            {"@NhsNumber",  NhsNumber },
        };

        var command = CreateCommand(parameters);
        command.CommandText = SQL;

        return GetDemographic(command);
    }

    private Demographic GetDemographic(IDbCommand command)
    {
        return ExecuteQuery<Demographic>(command, reader =>
        {
            var demographic = new Demographic();
            while (reader.Read())
            {
                demographic.ParticipantId = reader["PARTICIPANT_ID"] == DBNull.Value ? null : reader["PARTICIPANT_ID"].ToString();
                demographic.NhsNumber = reader["NHS_NUMBER"] == DBNull.Value ? null : reader["NHS_NUMBER"].ToString();
                demographic.SupersededByNhsNumber = reader["SUPERSEDED_BY_NHS_NUMBER"] == DBNull.Value ? null : reader["SUPERSEDED_BY_NHS_NUMBER"].ToString();
                demographic.PrimaryCareProvider = reader["PRIMARY_CARE_PROVIDER"] == DBNull.Value ? null : reader["PRIMARY_CARE_PROVIDER"].ToString();
                demographic.PrimaryCareProviderEffectiveFromDate = reader["PRIMARY_CARE_PROVIDER_FROM_DT"] == DBNull.Value ? null : reader["PRIMARY_CARE_PROVIDER_FROM_DT"].ToString();
                demographic.CurrentPosting = reader["CURRENT_POSTING"] == DBNull.Value ? null : reader["CURRENT_POSTING"].ToString();
                demographic.CurrentPostingEffectiveFromDate = reader["CURRENT_POSTING_FROM_DT"] == DBNull.Value ? null : reader["CURRENT_POSTING_FROM_DT"].ToString();
                demographic.PreviousPosting = reader["PREVIOUS_POSTING"] == DBNull.Value ? null : reader["PREVIOUS_POSTING"].ToString();
                demographic.PreviousPostingEffectiveFromDate = reader["PREV_POSTING_TO_DT"] == DBNull.Value ? null : reader["PREV_POSTING_TO_DT"].ToString();
                demographic.NamePrefix = reader["NAME_PREFIX"] == DBNull.Value ? null : reader["NAME_PREFIX"].ToString();
                demographic.FirstName = reader["GIVEN_NAME"] == DBNull.Value ? null : reader["GIVEN_NAME"].ToString();
                demographic.OtherGivenNames = reader["OTHER_GIVEN_NAME"] == DBNull.Value ? null : reader["OTHER_GIVEN_NAME"].ToString();
                demographic.Surname = reader["FAMILY_NAME"] == DBNull.Value ? null : reader["FAMILY_NAME"].ToString();
                demographic.PreviousSurname = reader["PREVIOUS_FAMILY_NAME"] == DBNull.Value ? null : reader["PREVIOUS_FAMILY_NAME"].ToString();
                demographic.DateOfBirth = reader["DATE_OF_BIRTH"] == DBNull.Value ? null : DateTime.Parse(reader["DATE_OF_BIRTH"].ToString()).ToString("yyyyMMdd");
                demographic.Gender = reader["GENDER"] == DBNull.Value ? null : (Gender)reader["GENDER"];
                demographic.AddressLine1 = reader["ADDRESS_LINE_1"] == DBNull.Value ? null : reader["ADDRESS_LINE_1"].ToString();
                demographic.AddressLine2 = reader["ADDRESS_LINE_2"] == DBNull.Value ? null : reader["ADDRESS_LINE_2"].ToString();
                demographic.AddressLine3 = reader["ADDRESS_LINE_3"] == DBNull.Value ? null : reader["ADDRESS_LINE_3"].ToString();
                demographic.AddressLine4 = reader["ADDRESS_LINE_4"] == DBNull.Value ? null : reader["ADDRESS_LINE_4"].ToString();
                demographic.AddressLine5 = reader["ADDRESS_LINE_5"] == DBNull.Value ? null : reader["ADDRESS_LINE_5"].ToString();
                demographic.Postcode = reader["POST_CODE"] == DBNull.Value ? null : reader["POST_CODE"].ToString();
                demographic.PafKey = reader["PAF_KEY"] == DBNull.Value ? null : reader["PAF_KEY"].ToString();
                demographic.UsualAddressEffectiveFromDate = reader["USUAL_ADDRESS_FROM_DT"] == DBNull.Value ? null : reader["USUAL_ADDRESS_FROM_DT"].ToString();
                demographic.DateOfDeath = reader["DATE_OF_DEATH"] == DBNull.Value ? null : DateTime.Parse(reader["DATE_OF_DEATH"].ToString()).ToString("yyyyMMdd");
                demographic.DeathStatus = reader["DEATH_STATUS"] == DBNull.Value ? null : (Status)reader["DEATH_STATUS"];
                demographic.TelephoneNumber = reader["TELEPHONE_NUMBER_HOME"] == DBNull.Value ? null : reader["TELEPHONE_NUMBER_HOME"].ToString();
                demographic.TelephoneNumberEffectiveFromDate = reader["TELEPHONE_NUMBER_HOME_FROM_DT"] == DBNull.Value ? null : reader["TELEPHONE_NUMBER_HOME_FROM_DT"].ToString();
                demographic.MobileNumber = reader["TELEPHONE_NUMBER_MOB"] == DBNull.Value ? null : reader["TELEPHONE_NUMBER_MOB"].ToString();
                demographic.MobileNumberEffectiveFromDate = reader["TELEPHONE_NUMBER_MOB_FROM_DT"] == DBNull.Value ? null : reader["TELEPHONE_NUMBER_MOB_FROM_DT"].ToString();
                demographic.EmailAddress = reader["EMAIL_ADDRESS_HOME"] == DBNull.Value ? null : reader["EMAIL_ADDRESS_HOME"].ToString();
                demographic.EmailAddressEffectiveFromDate = reader["EMAIL_ADDRESS_HOME_FROM_DT"] == DBNull.Value ? null : reader["EMAIL_ADDRESS_HOME_FROM_DT"].ToString();
                demographic.PreferredLanguage = reader["PREFERRED_LANGUAGE"] == DBNull.Value ? null : reader["PREFERRED_LANGUAGE"].ToString();
                demographic.IsInterpreterRequired = reader["INTERPRETER_REQUIRED"] == DBNull.Value ? null : reader["INTERPRETER_REQUIRED"].ToString();
                demographic.InvalidFlag = reader["INVALID_FLAG"] == DBNull.Value ? null : reader["INVALID_FLAG"].ToString();
                demographic.RecordInsertDateTime = reader["RECORD_INSERT_DATE_TIME"] == DBNull.Value ? null : reader["RECORD_INSERT_DATE_TIME"].ToString();
                demographic.RecordUpdateDateTime = reader["RECORD_UPDATE_DATE_TIME"] == DBNull.Value ? null : reader["RECORD_UPDATE_DATE_TIME"].ToString();
            }
            return demographic;
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
        catch (SqlException sqlEx)
        {
            if (sqlEx.Number == 2627)
            {
                _logger.LogInformation($"Sql message: {sqlEx.Message}");
                return true;
            }
            _logger.LogError($"an error happened: {sqlEx.Message}");
            return false;
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
