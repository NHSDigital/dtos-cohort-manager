namespace Data.Database;

using System.Data;
using System.Text.Json;
using Common.Interfaces;
using Microsoft.Extensions.Logging;
using Model;

public class CreateCohortDistributionData : ICreateCohortDistributionData
{

    private readonly IDbConnection _dbConnection;
    private readonly IDatabaseHelper _databaseHelper;
    private readonly string _connectionString;
    private readonly ILogger<CreateCohortDistributionData> _logger;

    public CreateCohortDistributionData(IDbConnection IdbConnection, IDatabaseHelper databaseHelper, ILogger<CreateCohortDistributionData> logger)
    {
        _dbConnection = IdbConnection;
        _databaseHelper = databaseHelper;
        _logger = logger;
        _connectionString = Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString") ?? string.Empty;
    }
    public bool InsertCohortDistributionData(CohortDistributionParticipant cohortDistributionParticipant)
    {
        var SQLToExecuteInOrder = new List<SQLReturnModel>();
        string insertParticipant = "INSERT INTO [dbo].[BS_COHORT_DISTRIBUTION] ( " +
            " PARTICIPANT_ID, " +
            " NHS_NUMBER," +
            " SUPERSEDED_NHS_NUMBER," +
            " PRIMARY_CARE_PROVIDER," +
            " PRIMARY_CARE_PROVIDER_FROM_DT," +
            " NAME_PREFIX, " +
            " GIVEN_NAME, " +
            " OTHER_GIVEN_NAME, " +
            " FAMILY_NAME, " +
            " PREVIOUS_FAMILY_NAME, " +
            " DATE_OF_BIRTH, " +
            " GENDER," +
            " ADDRESS_LINE_1," +
            " ADDRESS_LINE_2," +
            " ADDRESS_LINE_3," +
            " ADDRESS_LINE_4," +
            " ADDRESS_LINE_5," +
            " POST_CODE," +
            " USUAL_ADDRESS_FROM_DT," +
            " DATE_OF_DEATH," +
            " TELEPHONE_NUMBER_HOME," +
            " TELEPHONE_NUMBER_HOME_FROM_DT," +
            " TELEPHONE_NUMBER_MOB," +
            " TELEPHONE_NUMBER_MOB_FROM_DT," +
            " PREFERRED_LANGUAGE," +
            " INTERPRETER_REQUIRED," +
            " REASON_FOR_REMOVAL," +
            " REASON_FOR_REMOVAL_DT," +
            " RECORD_INSERT_DATETIME, " +
            " RECORD_UPDATE_DATETIME, " +
            " IS_EXTRACTED " +
            " ) VALUES( " +
            " @participantId, " +
            " @nhsNumber, " +
            " @supersededByNhsNumber, " +
            " @primaryCareProvider, " +
            " @primaryCareProviderFromDate, " +
            " @namePrefix, " +
            " @givenName, " +
            " @otherGivenNames, " +
            " @familyName," +
            " @previousFamilyName, " +
            " @dateOfBirth, " +
            " @gender, " +
            " @addressLine1, " +
            " @addressLine2, " +
            " @addressLine3, " +
            " @addressLine4, " +
            " @addressLine5, " +
            " @postCode, " +
            " @usualAddressFromDate, " +
            " @dateOfDeath, " +
            " @telephoneNumberHome, " +
            " @telephoneNumberHomeFromDate, " +
            " @telephoneNumberMob, " +
            " @telephoneNumberMobFromDate, " +
            " @preferredLanguage," +
            " @interpreterRequired," +
            " @reasonForRemoval," +
            " @reasonForRemovalFromDate," +
            " @recordInsertDateTime," +
            " @recordUpdateDateTime," +
            " @extracted" +
            " ) ";

        var parameters = new Dictionary<string, object>
        {
            {"@participantId", _databaseHelper.CheckIfNumberNull(cohortDistributionParticipant.ParticipantId) ? DBNull.Value : cohortDistributionParticipant.ParticipantId}  ,
            {"@nhsNumber", _databaseHelper.CheckIfNumberNull(cohortDistributionParticipant.NhsNumber) ? DBNull.Value : cohortDistributionParticipant.NhsNumber},
            {"@supersededByNhsNumber", _databaseHelper.CheckIfNumberNull(cohortDistributionParticipant.SupersededByNhsNumber) ? DBNull.Value : cohortDistributionParticipant.SupersededByNhsNumber},
            {"@primaryCareProvider", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.PrimaryCareProvider)},
            {"@primaryCareProviderFromDate", _databaseHelper.CheckIfDateNull(cohortDistributionParticipant.PrimaryCareProviderEffectiveFromDate) ? DBNull.Value : _databaseHelper.ParseDates(cohortDistributionParticipant.PrimaryCareProviderEffectiveFromDate) },
            {"@namePrefix",  _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.NamePrefix) },
            {"@givenName", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.FirstName) },
            {"@otherGivenNames", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.OtherGivenNames) },
            {"@familyName", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.Surname) },
            {"@previousFamilyName", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.PreviousSurname) },
            {"@dateOfBirth", _databaseHelper.CheckIfDateNull(cohortDistributionParticipant.DateOfBirth) ? DateTime.MaxValue : _databaseHelper.ParseDates(cohortDistributionParticipant.DateOfBirth)},
            {"@gender", cohortDistributionParticipant.Gender.HasValue ? cohortDistributionParticipant.Gender : DBNull.Value},
            {"@addressLine1", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.AddressLine1)},
            {"@addressLine2", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.AddressLine2)},
            {"@addressLine3", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.AddressLine3)},
            {"@addressLine4", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.AddressLine4)},
            {"@addressLine5", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.AddressLine5)},
            {"@postCode", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.Postcode)},
            {"@usualAddressFromDate", _databaseHelper.CheckIfDateNull(cohortDistributionParticipant.UsualAddressEffectiveFromDate) ? DBNull.Value : _databaseHelper.ParseDates(cohortDistributionParticipant.UsualAddressEffectiveFromDate)},
            {"@dateOfDeath", string.IsNullOrEmpty(cohortDistributionParticipant.DateOfDeath) ? DBNull.Value : _databaseHelper.ParseDates(cohortDistributionParticipant.DateOfDeath)},
            {"@telephoneNumberHome", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.TelephoneNumber)},
            {"@telephoneNumberHomeFromDate", _databaseHelper.CheckIfDateNull(cohortDistributionParticipant.TelephoneNumberEffectiveFromDate) ? DBNull.Value : _databaseHelper.ParseDates(cohortDistributionParticipant.TelephoneNumberEffectiveFromDate)},
            {"@telephoneNumberMob", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.MobileNumber)},
            {"@telephoneNumberMobFromDate", _databaseHelper.CheckIfDateNull(cohortDistributionParticipant.MobileNumberEffectiveFromDate) ? DBNull.Value : _databaseHelper.ParseDates(cohortDistributionParticipant.MobileNumberEffectiveFromDate) },
            {"@preferredLanguage", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.PreferredLanguage)},
            {"@interpreterRequired", _databaseHelper.CheckIfDateNull(cohortDistributionParticipant.IsInterpreterRequired) ? DBNull.Value : _databaseHelper.ParseDates(cohortDistributionParticipant.DateOfDeath)},
            {"@reasonForRemoval", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.ReasonForRemoval) },
            {"@reasonForRemovalFromDate", _databaseHelper.CheckIfDateNull(cohortDistributionParticipant.ReasonForRemovalEffectiveFromDate) ? DBNull.Value : _databaseHelper.ParseDateToString(cohortDistributionParticipant.ReasonForRemovalEffectiveFromDate)},
            {"@recordInsertDateTime", _databaseHelper.CheckIfDateNull(cohortDistributionParticipant.RecordInsertDateTime) ? DBNull.Value : _databaseHelper.ParseDateToString(cohortDistributionParticipant.RecordInsertDateTime)},
            {"@recordUpdateDateTime", _databaseHelper.CheckIfDateNull(cohortDistributionParticipant.RecordUpdateDateTime) ? DBNull.Value : _databaseHelper.ParseDateToString(cohortDistributionParticipant.RecordUpdateDateTime)},
            {"@extracted", _databaseHelper.CheckIfNumberNull(cohortDistributionParticipant.Extracted) ? 0 : cohortDistributionParticipant.Extracted}
        };

        SQLToExecuteInOrder.Add(new SQLReturnModel()
        {
            CommandType = CommandType.Scalar,
            SQL = insertParticipant,
            Parameters = parameters
        });

        return UpdateRecords(SQLToExecuteInOrder);
    }

    public List<CohortDistributionParticipant> ExtractCohortDistributionParticipants(int serviceProviderId, int rowCount)
    {
        var SQL = "SELECT TOP (@RowCount)" +
        " [PARTICIPANT_ID], " +
        " [NHS_NUMBER], " +
        " [SUPERSEDED_NHS_NUMBER], " +
        " [PRIMARY_CARE_PROVIDER], " +
        " [PRIMARY_CARE_PROVIDER_FROM_DT], " +
        " [NAME_PREFIX], " +
        " [GIVEN_NAME], " +
        " [OTHER_GIVEN_NAME], " +
        " [FAMILY_NAME], " +
        " [PREVIOUS_FAMILY_NAME], " +
        " [DATE_OF_BIRTH], " +
        " [GENDER], " +
        " [ADDRESS_LINE_1], " +
        " [ADDRESS_LINE_2], " +
        " [ADDRESS_LINE_3], " +
        " [ADDRESS_LINE_4], " +
        " [ADDRESS_LINE_5], " +
        " [POST_CODE], " +
        " [USUAL_ADDRESS_FROM_DT], " +
        " [DATE_OF_DEATH], " +
        " [TELEPHONE_NUMBER_HOME], " +
        " [TELEPHONE_NUMBER_HOME_FROM_DT], " +
        " [TELEPHONE_NUMBER_MOB], " +
        " [TELEPHONE_NUMBER_MOB_FROM_DT], " +
        " [EMAIL_ADDRESS_HOME], " +
        " [EMAIL_ADDRESS_HOME_FROM_DT], " +
        " [PREFERRED_LANGUAGE], " +
        " [INTERPRETER_REQUIRED], " +
        " [REASON_FOR_REMOVAL], " +
        " [REASON_FOR_REMOVAL_DT], " +
        " [RECORD_INSERT_DATETIME], " +
        " [RECORD_UPDATE_DATETIME], " +
        " [IS_EXTRACTED] " +
        " FROM [dbo].[BS_COHORT_DISTRIBUTION] bcd " +
        " JOIN [dbo].[PARTICIPANT_MANAGEMENT] pm ON bcd.PARTICIPANT_ID = pm.PARTICIPANT_ID " +
        " WHERE bcd.IS_EXTRACTED = @Extracted " +
        " AND pm.SCREENING_ID = @ServiceProviderId";

        var parameters = new Dictionary<string, object>
        {
            {"@RowCount", rowCount },
            {"@Extracted", 0 },
            {"@ServiceProviderId", serviceProviderId }
        };

        var command = CreateCommand(parameters);
        command.CommandText = SQL;

        var listOfAllParticipants = GetParticipant(command);

        if (MarkCohortDistributionParticipantsAsExtracted(listOfAllParticipants))
        {
            return listOfAllParticipants;
        }

        return new List<CohortDistributionParticipant>();
    }

    public List<CohortDistributionParticipant> GetCohortDistributionParticipantsByRequestId(string requestId)
    {

        var SQL = "SELECT" +
                " [PARTICIPANT_ID], " +
                " [NHS_NUMBER], " +
                " [SUPERSEDED_NHS_NUMBER], " +
                " [PRIMARY_CARE_PROVIDER], " +
                " [PRIMARY_CARE_PROVIDER_FROM_DT], " +
                " [NAME_PREFIX], " +
                " [GIVEN_NAME], " +
                " [OTHER_GIVEN_NAME], " +
                " [FAMILY_NAME], " +
                " [PREVIOUS_FAMILY_NAME], " +
                " [DATE_OF_BIRTH], " +
                " [GENDER], " +
                " [ADDRESS_LINE_1], " +
                " [ADDRESS_LINE_2], " +
                " [ADDRESS_LINE_3], " +
                " [ADDRESS_LINE_4], " +
                " [ADDRESS_LINE_5], " +
                " [POST_CODE], " +
                " [USUAL_ADDRESS_FROM_DT], " +
                " [DATE_OF_DEATH], " +
                " [TELEPHONE_NUMBER_HOME], " +
                " [TELEPHONE_NUMBER_HOME_FROM_DT], " +
                " [TELEPHONE_NUMBER_MOB], " +
                " [TELEPHONE_NUMBER_MOB_FROM_DT], " +
                " [EMAIL_ADDRESS_HOME], " +
                " [EMAIL_ADDRESS_HOME_FROM_DT], " +
                " [PREFERRED_LANGUAGE], " +
                " [INTERPRETER_REQUIRED], " +
                " [REASON_FOR_REMOVAL], " +
                " [REASON_FOR_REMOVAL_DT], " +
                " [RECORD_INSERT_DATETIME], " +
                " [RECORD_UPDATE_DATETIME], " +
                " [IS_EXTRACTED], " +
                " [REQUEST_ID] " +
            " FROM [dbo].[BS_COHORT_DISTRIBUTION] " +
            " WHERE REQUEST_ID = @RequestId";

        var parameters = new Dictionary<string, object>
        {
            {"@RequestId", requestId }
        };

        var command = CreateCommand(parameters);
        command.CommandText = SQL;

        return GetParticipant(command);
    }

    public List<CohortDistributionParticipant> GetCohortDistributionParticipantsMock(int serviceProviderId, int rowCount, string testDataJson)
    {
        try
        {
            var participants = JsonSerializer.Deserialize<List<CohortDistributionParticipant>>(testDataJson);

            if (participants == null || participants.Count == 0) return [];

            return participants
                .Where(p => p.ServiceProviderId == serviceProviderId)
                .Take(rowCount)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read or deserialize CohortMockData.json.");
            return [];
        }
    }

    public CohortDistributionParticipant GetLastCohortDistributionParticipant(string NhsNumber)
    {
        var SQL = "SELECT TOP (1) [PARTICIPANT_ID], " +
                " [NHS_NUMBER], " +
                " [SUPERSEDED_NHS_NUMBER], " +
                " [PRIMARY_CARE_PROVIDER], " +
                " [PRIMARY_CARE_PROVIDER_FROM_DT], " +
                " [NAME_PREFIX], " +
                " [GIVEN_NAME], " +
                " [OTHER_GIVEN_NAME], " +
                " [FAMILY_NAME], " +
                " [PREVIOUS_FAMILY_NAME], " +
                " [DATE_OF_BIRTH], " +
                " [GENDER], " +
                " [ADDRESS_LINE_1], " +
                " [ADDRESS_LINE_2], " +
                " [ADDRESS_LINE_3], " +
                " [ADDRESS_LINE_4], " +
                " [ADDRESS_LINE_5], " +
                " [POST_CODE], " +
                " [USUAL_ADDRESS_FROM_DT], " +
                " [DATE_OF_DEATH], " +
                " [TELEPHONE_NUMBER_HOME], " +
                " [TELEPHONE_NUMBER_HOME_FROM_DT], " +
                " [TELEPHONE_NUMBER_MOB], " +
                " [TELEPHONE_NUMBER_MOB_FROM_DT], " +
                " [EMAIL_ADDRESS_HOME], " +
                " [EMAIL_ADDRESS_HOME_FROM_DT], " +
                " [PREFERRED_LANGUAGE], " +
                " [INTERPRETER_REQUIRED], " +
                " [REASON_FOR_REMOVAL], " +
                " [REASON_FOR_REMOVAL_DT], " +
                " [RECORD_INSERT_DATETIME], " +
                " [RECORD_UPDATE_DATETIME], " +
                " [IS_EXTRACTED], " +
                " [REQUEST_ID] " +
                " FROM [dbo].[BS_COHORT_DISTRIBUTION] " +
                " WHERE NHS_NUMBER = @NhsNumber " +
                " ORDER BY PARTICIPANT_ID DESC ";

        var parameters = new Dictionary<string, object>
        {
            {"@NhsNumber", NhsNumber },
        };

        var command = CreateCommand(parameters);
        command.CommandText = SQL;

        return GetParticipant(command).FirstOrDefault();
    }

    public bool UpdateCohortParticipantAsInactive(string NhsNumber)
    {

        _logger.LogInformation("Updating Cohort Participant as Inactive");

        if (string.IsNullOrEmpty(NhsNumber))
        {
            _logger.LogError("No NHSID was Provided");
            return false;
        }

        var recordEndDate = DateTime.Today;

        var SQL = " UPDATE [dbo].[BS_COHORT_DISTRIBUTION] " +
            " SET REASON_FOR_REMOVAL_DT = @recordEndDate " +
            " WHERE NHS_NUMBER = @NhsNumber  ";

        var parameters = new Dictionary<string, object>
        {
            {"@NhsNumber", NhsNumber},
            {"@recordEndDate",recordEndDate}
        };

        var sqlToExecute = new List<SQLReturnModel>()
            {
                new SQLReturnModel
                {
                    Parameters = parameters,
                    SQL = SQL,
                }
            };

        return UpdateRecords(sqlToExecute);
    }

    private List<CohortDistributionParticipant> GetParticipant(IDbCommand command)
    {
        List<CohortDistributionParticipant> participants = [];

        return ExecuteQuery(command, reader =>
        {
            while (reader.Read())
            {
                var participant = new CohortDistributionParticipant
                {
                    ParticipantId = DatabaseHelper.GetStringValue(reader, "PARTICIPANT_ID"),
                    NhsNumber = DatabaseHelper.GetStringValue(reader, "NHS_NUMBER"),
                    SupersededByNhsNumber = DatabaseHelper.GetStringValue(reader, "SUPERSEDED_NHS_NUMBER"),
                    PrimaryCareProvider = DatabaseHelper.GetStringValue(reader, "PRIMARY_CARE_PROVIDER"),
                    PrimaryCareProviderEffectiveFromDate = DatabaseHelper.GetStringValue(reader, "PRIMARY_CARE_PROVIDER_FROM_DT"),
                    NamePrefix = DatabaseHelper.GetStringValue(reader, "NAME_PREFIX"),
                    FirstName = DatabaseHelper.GetStringValue(reader, "GIVEN_NAME"),
                    OtherGivenNames = DatabaseHelper.GetStringValue(reader, "OTHER_GIVEN_NAME"),
                    Surname = DatabaseHelper.GetStringValue(reader, "FAMILY_NAME"),
                    PreviousSurname = DatabaseHelper.GetStringValue(reader, "PREVIOUS_FAMILY_NAME"),
                    DateOfBirth = DatabaseHelper.GetStringValue(reader, "DATE_OF_BIRTH"),
                    Gender = DatabaseHelper.GetGenderValue(reader, "GENDER"),
                    AddressLine1 = DatabaseHelper.GetStringValue(reader, "ADDRESS_LINE_1"),
                    AddressLine2 = DatabaseHelper.GetStringValue(reader, "ADDRESS_LINE_2"),
                    AddressLine3 = DatabaseHelper.GetStringValue(reader, "ADDRESS_LINE_3"),
                    AddressLine4 = DatabaseHelper.GetStringValue(reader, "ADDRESS_LINE_4"),
                    AddressLine5 = DatabaseHelper.GetStringValue(reader, "ADDRESS_LINE_5"),
                    Postcode = DatabaseHelper.GetStringValue(reader, "POST_CODE"),
                    UsualAddressEffectiveFromDate = DatabaseHelper.GetStringValue(reader, "USUAL_ADDRESS_FROM_DT"),
                    DateOfDeath = DatabaseHelper.GetStringValue(reader, "DATE_OF_DEATH"),
                    TelephoneNumber = DatabaseHelper.GetStringValue(reader, "TELEPHONE_NUMBER_HOME"),
                    TelephoneNumberEffectiveFromDate = DatabaseHelper.GetStringValue(reader, "TELEPHONE_NUMBER_HOME_FROM_DT"),
                    MobileNumber = DatabaseHelper.GetStringValue(reader, "TELEPHONE_NUMBER_MOB"),
                    MobileNumberEffectiveFromDate = DatabaseHelper.GetStringValue(reader, "TELEPHONE_NUMBER_MOB_FROM_DT"),
                    EmailAddress = DatabaseHelper.GetStringValue(reader, "EMAIL_ADDRESS_HOME"),
                    EmailAddressEffectiveFromDate = DatabaseHelper.GetStringValue(reader, "EMAIL_ADDRESS_HOME_FROM_DT"),
                    PreferredLanguage = DatabaseHelper.GetStringValue(reader, "PREFERRED_LANGUAGE"),
                    IsInterpreterRequired = DatabaseHelper.GetStringValue(reader, "INTERPRETER_REQUIRED"),
                    ReasonForRemoval = DatabaseHelper.GetStringValue(reader, "REASON_FOR_REMOVAL"),
                    ReasonForRemovalEffectiveFromDate = DatabaseHelper.GetStringValue(reader, "REASON_FOR_REMOVAL_DT"),
                    RecordInsertDateTime = DatabaseHelper.GetStringValue(reader, "RECORD_INSERT_DATETIME"),
                    RecordUpdateDateTime = DatabaseHelper.GetStringValue(reader, "RECORD_UPDATE_DATETIME"),
                    Extracted = DatabaseHelper.GetStringValue(reader, "IS_EXTRACTED"),
                    RequestId = DatabaseHelper.GetStringValue(reader, "REQUEST_ID")
                };

                participants.Add(participant);
            }
            return participants;
        });
    }

    private bool MarkCohortDistributionParticipantsAsExtracted(List<CohortDistributionParticipant> cohortParticipants)
    {
        if (cohortParticipants == null || cohortParticipants.Count == 0) return false;

        var requestId = Guid.NewGuid();
        var sqlToExecute = new List<SQLReturnModel>();

        foreach (var participant in cohortParticipants)
        {
            var SQL = " UPDATE [dbo].[BS_COHORT_DISTRIBUTION] " +
                    " SET IS_EXTRACTED = @Extracted, REQUEST_ID = @RequestId" +
                    " WHERE PARTICIPANT_ID = @ParticipantId";

            var parameters = new Dictionary<string, object>
        {
            {"@Extracted", 1 },
            {"@RequestId", requestId },
            {"@ParticipantId", participant.ParticipantId}
        };

            sqlToExecute.Add(new SQLReturnModel
            {
                Parameters = parameters,
                SQL = SQL,
            });

            participant.Extracted = "1";
            participant.RequestId = requestId.ToString();
        }

        return UpdateRecords(sqlToExecute);
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
            _logger.LogError(ex, "An error occurred while inserting new Cohort Distribution records: {ExceptionMessage}", ex.Message);
            return false;
        }
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
}
