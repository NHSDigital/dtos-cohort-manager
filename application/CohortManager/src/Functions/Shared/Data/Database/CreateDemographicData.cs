namespace Data.Database;

using System.Data;
using Microsoft.Extensions.Logging;
using Model;
using Microsoft.Data.SqlClient;

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

    public bool InsertDemographicData(Participant participant)
    {
        var command = new List<SQLReturnModel>()
        {
            new SQLReturnModel()
            {
                CommandType = CommandType.Command,
                SQL = "INSERT INTO [dbo].[DEMOGRAPHIC_DATA] " +
                "(" +
                    " [resource_id] " +
                    ", [nhs_number] " +
                    ", [prefix] " +
                    ", [given_name] " +
                    ", [family_name] " +
                    ", [gender] " +
                    ", [birth_date] " +
                    ", [deceased_datetime] " +
                    ", [general_practitioner_code] " +
                    ", [managing_organization_code] " +
                    ", [communication_language] " +
                    ", [interpreter_required] " +
                    ", [preferred_communication_format] " +
                    ", [preferred_contact_method] " +
                    ", [preferred_contact_time] " +
                    ", [birth_place_city] " +
                    ", [birth_place_district] " +
                    ", [birth_place_country] " +
                    ", [removal_reason_code] " +
                    ", [removal_effective_start] " +
                    " ,[removal_effective_end] " +
                    " ,[home_address_line1] " +
                    " ,[home_address_line2] " +
                    " ,[home_address_line3] " +
                    " ,[home_address_city] " +
                    " ,[home_address_postcode] " +
                    " ,[home_phone_number] " +
                    " ,[home_email_address] " +
                    " ,[home_phone_textphone] " +
                    " ,[emergency_contact_phone_number] ) " +
                "VALUES " +
                "(" +
                    " @resource_id, " +
                    " @nhs_number, " +
                    " @prefix, " +
                    " @given_name, " +
                    " @family_name, " +
                    " @gender, " +
                    " @birth_date, " +
                    " @deceased_datetime, " +
                    " @general_practitioner_code, " +
                    " @managing_organization_code, " +
                    " @communication_language, " +
                    " @interpreter_required, " +
                    " @preferred_communication_format," +
                    " @preferred_contact_method, " +
                    " @preferred_contact_time, " +
                    " @birth_place_city, " +
                    " @birth_place_district, " +
                    " @birth_place_country, " +
                    " @removal_reason_code, " +
                    " @removal_effective_start, " +
                    " @removal_effective_end, " +
                    " @home_address_line1, " +
                    " @home_address_line2, " +
                    " @home_address_line3, " +
                    " @home_address_city, " +
                    " @home_address_postcode, " +
                    " @home_phone_number, " +
                    " @home_email_address, " +
                    " @home_phone_textphone, " +
                    " @emergency_contact_phone_number" +
                ")",
                Parameters = new Dictionary<string, object>
                {
                    {"@resource_id", participant.RecordIdentifier},
                    {"@nhs_number", participant.NHSId},
                    {"@prefix", _databaseHelper.ConvertNullToDbNull(participant.NamePrefix)},
                    {"@given_name", _databaseHelper.ConvertNullToDbNull(participant.FirstName)},
                    {"@family_name", _databaseHelper.ConvertNullToDbNull(participant.Surname)},
                    {"@gender", participant.Gender.ToString()},
                    {"@birth_date", string.IsNullOrEmpty(participant.DateOfBirth) ? DBNull.Value : _databaseHelper.ParseDates(participant.DateOfBirth)},
                    {"@deceased_datetime", _databaseHelper.CheckIfDateNull(participant.DateOfDeath) ? DBNull.Value : _databaseHelper.ParseDates(participant.DateOfDeath)},
                    {"@general_practitioner_code", _databaseHelper.ConvertNullToDbNull(participant.PrimaryCareProvider)},
                    {"@managing_organization_code", DBNull.Value},
                    {"@communication_language", _databaseHelper.ConvertNullToDbNull(participant.PreferredLanguage)},
                    {"@interpreter_required", _databaseHelper.ConvertNullToDbNull(participant.IsInterpreterRequired)},
                    {"@preferred_communication_format", DBNull.Value},
                    {"@preferred_contact_method", DBNull.Value},
                    {"@preferred_contact_time", DBNull.Value},
                    {"@birth_place_city", DBNull.Value},
                    {"@birth_place_district", DBNull.Value},
                    {"@birth_place_country", DBNull.Value},
                    {"@removal_reason_code", _databaseHelper.ConvertNullToDbNull(participant.ReasonForRemoval)},
                    {"@removal_effective_start", _databaseHelper.CheckIfDateNull(participant.ReasonForRemovalEffectiveFromDate) ? DBNull.Value : _databaseHelper.ParseDateToString(participant.ReasonForRemovalEffectiveFromDate)},
                    {"@removal_effective_end", DBNull.Value},
                    {"@home_address_line1", _databaseHelper.ConvertNullToDbNull(participant.AddressLine1)},
                    {"@home_address_line2", _databaseHelper.ConvertNullToDbNull(participant.AddressLine2)},
                    {"@home_address_line3", _databaseHelper.ConvertNullToDbNull(participant.AddressLine3)},
                    {"@home_address_city", _databaseHelper.ConvertNullToDbNull(participant.AddressLine4)},
                    {"@home_address_postcode", _databaseHelper.ConvertNullToDbNull(participant.Postcode)},
                    {"@home_phone_number", _databaseHelper.ConvertNullToDbNull(participant.TelephoneNumber)},
                    {"@home_email_address", _databaseHelper.ConvertNullToDbNull(participant.EmailAddress)},
                    {"@home_phone_textphone", DBNull.Value},
                    {"@emergency_contact_phone_number", _databaseHelper.ConvertNullToDbNull(participant.MobileNumber)}
                },

            }
        };

        return UpdateRecords(command);
    }

    public Demographic GetDemographicData(string NHSId)
    {
        var SQL = @"SELECT * FROM [dbo].[DEMOGRAPHIC_DATA] WHERE nhs_number = @NHSId";
        var parameters = new Dictionary<string, object>()
        {
            {"@NHSId",  NHSId },
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
                demographic.ResourceId = reader["resource_id"] == DBNull.Value ? null : reader["resource_id"].ToString();
                demographic.NhsNumber = reader["nhs_number"] == DBNull.Value ? null : reader["nhs_number"].ToString();
                demographic.Prefix = reader["prefix"] == DBNull.Value ? null : reader["prefix"].ToString();
                demographic.GivenName = reader["given_name"] == DBNull.Value ? null : reader["given_name"].ToString();
                demographic.FamilyName = reader["family_name"] == DBNull.Value ? null : reader["family_name"].ToString();
                demographic.Gender = reader["gender"] == DBNull.Value ? null : reader["gender"].ToString();
                demographic.BirthDate = reader["birth_date"] == DBNull.Value ? null : reader["birth_date"].ToString();
                demographic.DeceasedDatetime = reader["deceased_datetime"] == DBNull.Value ? null : reader["deceased_datetime"].ToString();
                demographic.GeneralPractitionerCode = reader["general_practitioner_code"] == DBNull.Value ? null : reader["general_practitioner_code"].ToString();
                demographic.ManagingOrganizationCode = reader["managing_organization_code"] == DBNull.Value ? null : reader["managing_organization_code"].ToString();
                demographic.CommunicationLanguage = reader["communication_language"] == DBNull.Value ? null : reader["communication_language"].ToString();
                demographic.InterpreterRequired = reader["interpreter_required"] == DBNull.Value ? null : reader["interpreter_required"].ToString();
                demographic.PreferredCommunicationFormat = reader["preferred_communication_format"] == DBNull.Value ? null : reader["preferred_communication_format"].ToString();
                demographic.PreferredContactMethod = reader["preferred_contact_method"] == DBNull.Value ? null : reader["preferred_contact_method"].ToString();
                demographic.PreferredContactTime = reader["preferred_contact_time"] == DBNull.Value ? null : reader["preferred_contact_time"].ToString();
                demographic.BirthPlaceCity = reader["birth_place_city"] == DBNull.Value ? null : reader["birth_place_city"].ToString();
                demographic.BirthPlaceDistrict = reader["birth_place_district"] == DBNull.Value ? null : reader["birth_place_district"].ToString();
                demographic.BirthPlaceCountry = reader["birth_place_country"] == DBNull.Value ? null : reader["birth_place_country"].ToString();
                demographic.RemovalReasonCode = reader["removal_reason_code"] == DBNull.Value ? null : reader["removal_reason_code"].ToString();
                demographic.RemovalEffectiveStart = reader["removal_effective_start"] == DBNull.Value ? null : reader["removal_effective_start"].ToString();
                demographic.RemovalEffectiveEnd = reader["removal_effective_end"] == DBNull.Value ? null : reader["removal_effective_end"].ToString();
                demographic.HomeAddressLine1 = reader["home_address_line1"] == DBNull.Value ? null : reader["home_address_line1"].ToString();
                demographic.HomeAddressLine2 = reader["home_address_line2"] == DBNull.Value ? null : reader["home_address_line2"].ToString();
                demographic.HomeAddressLine3 = reader["home_address_line3"] == DBNull.Value ? null : reader["home_address_line3"].ToString();
                demographic.HomeAddressCity = reader["home_address_city"] == DBNull.Value ? null : reader["home_address_city"].ToString();
                demographic.HomeAddressPostcode = reader["home_address_postcode"] == DBNull.Value ? null : reader["home_address_postcode"].ToString();
                demographic.HomePhoneNumber = reader["home_phone_number"] == DBNull.Value ? null : reader["home_phone_number"].ToString();
                demographic.HomeEmailAddress = reader["home_email_address"] == DBNull.Value ? null : reader["home_email_address"].ToString();
                demographic.HomePhoneTextphone = reader["home_phone_textphone"] == DBNull.Value ? null : reader["home_phone_textphone"].ToString();
                demographic.EmergencyContactPhoneNumber = reader["emergency_contact_phone_number"] == DBNull.Value ? null : reader["emergency_contact_phone_number"].ToString();
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
