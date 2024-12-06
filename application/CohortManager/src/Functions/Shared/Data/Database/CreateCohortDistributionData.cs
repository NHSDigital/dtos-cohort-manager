namespace Data.Database;

using System.Data;
using System.Net;
using Common;
using Common.Interfaces;
using Microsoft.Extensions.Logging;
using Model;
using Model.DTO;
using Model.Enums;

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
            " EMAIL_ADDRESS_HOME," +
            " EMAIL_ADDRESS_HOME_FROM_DT," +
            " PREFERRED_LANGUAGE," +
            " INTERPRETER_REQUIRED," +
            " REASON_FOR_REMOVAL," +
            " REASON_FOR_REMOVAL_FROM_DT," +
            " RECORD_INSERT_DATETIME, " +
            " RECORD_UPDATE_DATETIME, " +
            " IS_EXTRACTED, " +
            " CURRENT_POSTING, " +
            " CURRENT_POSTING_FROM_DT" +
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
            " @emailAddressHome," +
            " @emailAddressFromDate," +
            " @preferredLanguage," +
            " @interpreterRequired," +
            " @reasonForRemoval," +
            " @reasonForRemovalFromDate," +
            " @recordInsertDateTime," +
            " @recordUpdateDateTime," +
            " @extracted," +
            " @currentPosting," +
            " @currentPostingFromDate" +
            " ) ";

        var parameters = new Dictionary<string, object>
        {
            {"@participantId", _databaseHelper.CheckIfNumberNull(cohortDistributionParticipant.ParticipantId) ? DBNull.Value : cohortDistributionParticipant.ParticipantId}  ,
            {"@nhsNumber", _databaseHelper.CheckIfNumberNull(cohortDistributionParticipant.NhsNumber) ? DBNull.Value : cohortDistributionParticipant.NhsNumber},
            {"@supersededByNhsNumber", _databaseHelper.CheckIfNumberNull(cohortDistributionParticipant.SupersededByNhsNumber) ? DBNull.Value : cohortDistributionParticipant.SupersededByNhsNumber},
            {"@primaryCareProvider", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.PrimaryCareProvider)},
            {"@primaryCareProviderFromDate", _databaseHelper.ParseDates(cohortDistributionParticipant.PrimaryCareProviderEffectiveFromDate)},
            {"@namePrefix",  _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.NamePrefix) },
            {"@givenName", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.FirstName) },
            {"@otherGivenNames", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.OtherGivenNames) },
            {"@familyName", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.FamilyName) },
            {"@previousFamilyName", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.PreviousFamilyName) },
            {"@dateOfBirth", _databaseHelper.ParseDates(cohortDistributionParticipant.DateOfBirth)},
            {"@gender", cohortDistributionParticipant.Gender.HasValue ? cohortDistributionParticipant.Gender : DBNull.Value},
            {"@addressLine1", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.AddressLine1)},
            {"@addressLine2", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.AddressLine2)},
            {"@addressLine3", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.AddressLine3)},
            {"@addressLine4", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.AddressLine4)},
            {"@addressLine5", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.AddressLine5)},
            {"@postCode", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.Postcode)},
            {"@usualAddressFromDate", _databaseHelper.ParseDates(cohortDistributionParticipant.UsualAddressEffectiveFromDate)},
            {"@dateOfDeath", _databaseHelper.ParseDates(cohortDistributionParticipant.DateOfDeath)},
            {"@telephoneNumberHome", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.TelephoneNumber)},
            {"@telephoneNumberHomeFromDate", _databaseHelper.ParseDates(cohortDistributionParticipant.TelephoneNumberEffectiveFromDate)},
            {"@telephoneNumberMob", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.MobileNumber)},
            {"@telephoneNumberMobFromDate", _databaseHelper.ParseDates(cohortDistributionParticipant.MobileNumberEffectiveFromDate)},
            {"@emailAddressHome", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.EmailAddress) },
            {"@emailAddressFromDate", _databaseHelper.ParseDates(cohortDistributionParticipant.EmailAddressEffectiveFromDate) },
            {"@preferredLanguage", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.PreferredLanguage)},
            {"@interpreterRequired", _databaseHelper.CheckIfNumberNull(cohortDistributionParticipant.IsInterpreterRequired) ? 0 : cohortDistributionParticipant.IsInterpreterRequired},
            {"@reasonForRemoval", _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.ReasonForRemoval) },
            {"@reasonForRemovalFromDate",  _databaseHelper.ParseDates(cohortDistributionParticipant.ReasonForRemovalEffectiveFromDate)},
            {"@recordInsertDateTime", _databaseHelper.ParseDates(cohortDistributionParticipant.RecordInsertDateTime)},
            {"@recordUpdateDateTime", _databaseHelper.ParseDates(cohortDistributionParticipant.RecordUpdateDateTime)},
            {"@extracted", _databaseHelper.CheckIfNumberNull(cohortDistributionParticipant.Extracted) ? 0 : cohortDistributionParticipant.Extracted},
            {"@currentPosting",  _databaseHelper.ConvertNullToDbNull(cohortDistributionParticipant.CurrentPosting) },
            {"@currentPostingFromDate",  _databaseHelper.ParseDates(cohortDistributionParticipant.CurrentPostingEffectiveFromDate)},
        };

        SQLToExecuteInOrder.Add(new SQLReturnModel()
        {
            CommandType = CommandType.Scalar,
            SQL = insertParticipant,
            Parameters = parameters
        });

        return UpdateRecords(SQLToExecuteInOrder);
    }

    public List<CohortDistributionParticipantDto> GetUnextractedCohortDistributionParticipantsByScreeningServiceId(int screeningServiceId, int rowCount)
    {
        var SQL = "SELECT TOP (@RowCount)" +
            " bcd.[PARTICIPANT_ID], " +
            " bcd.[NHS_NUMBER], " +
            " bcd.[SUPERSEDED_NHS_NUMBER], " +
            " bcd.[PRIMARY_CARE_PROVIDER], " +
            " bcd.[PRIMARY_CARE_PROVIDER_FROM_DT], " +
            " bcd.[NAME_PREFIX], " +
            " bcd.[GIVEN_NAME], " +
            " bcd.[OTHER_GIVEN_NAME], " +
            " bcd.[FAMILY_NAME], " +
            " bcd.[PREVIOUS_FAMILY_NAME], " +
            " bcd.[DATE_OF_BIRTH], " +
            " bcd.[GENDER], " +
            " bcd.[ADDRESS_LINE_1], " +
            " bcd.[ADDRESS_LINE_2], " +
            " bcd.[ADDRESS_LINE_3], " +
            " bcd.[ADDRESS_LINE_4], " +
            " bcd.[ADDRESS_LINE_5], " +
            " bcd.[POST_CODE], " +
            " bcd.[USUAL_ADDRESS_FROM_DT], " +
            " bcd.[CURRENT_POSTING], " +
            " bcd.[CURRENT_POSTING_FROM_DT], " +
            " bcd.[DATE_OF_DEATH], " +
            " bcd.[TELEPHONE_NUMBER_HOME], " +
            " bcd.[TELEPHONE_NUMBER_HOME_FROM_DT], " +
            " bcd.[TELEPHONE_NUMBER_MOB], " +
            " bcd.[TELEPHONE_NUMBER_MOB_FROM_DT], " +
            " bcd.[EMAIL_ADDRESS_HOME], " +
            " bcd.[EMAIL_ADDRESS_HOME_FROM_DT], " +
            " bcd.[PREFERRED_LANGUAGE], " +
            " bcd.[INTERPRETER_REQUIRED], " +
            " bcd.[REASON_FOR_REMOVAL], " +
            " bcd.[REASON_FOR_REMOVAL_FROM_DT], " +
            " bcd.[RECORD_INSERT_DATETIME], " +
            " bcd.[RECORD_UPDATE_DATETIME], " +
            " bcd.[IS_EXTRACTED], " +
            " bcd.[REQUEST_ID] " +
            " FROM [dbo].[BS_COHORT_DISTRIBUTION] bcd " +
            " WHERE bcd.IS_EXTRACTED = @Extracted";

        var parameters = new Dictionary<string, object>
        {
            {"@RowCount", rowCount },
            {"@Extracted", 0 },
        };

        var command = CreateCommand(parameters);
        command.CommandText = SQL;

        var listOfAllParticipants = GetParticipant(command);
        var requestId = Guid.NewGuid().ToString();
        if (MarkCohortDistributionParticipantsAsExtracted(listOfAllParticipants, requestId, screeningServiceId))
        {
            LogRequestAudit(requestId, (int)HttpStatusCode.OK);
            return CohortDistributionParticipantDto(listOfAllParticipants, requestId);
        }

        var statusCode = listOfAllParticipants.Count == 0 ? (int)HttpStatusCode.NoContent : (int)HttpStatusCode.InternalServerError;
        LogRequestAudit(requestId, statusCode);

        return new List<CohortDistributionParticipantDto>();
    }

    private static List<CohortDistributionParticipantDto> CohortDistributionParticipantDto(List<CohortDistributionParticipant> listOfAllParticipants, string requestId)
    {
        return listOfAllParticipants.Select(s => new CohortDistributionParticipantDto
        {
            RequestId = requestId ?? string.Empty,
            NhsNumber = s.NhsNumber ?? string.Empty,
            SupersededByNhsNumber = s.SupersededByNhsNumber ?? string.Empty,
            PrimaryCareProvider = s.PrimaryCareProvider ?? string.Empty,
            PrimaryCareProviderEffectiveFromDate = DatabaseHelper.FormatDateAPI(s.PrimaryCareProviderEffectiveFromDate),
            NamePrefix = s.NamePrefix ?? string.Empty,
            FirstName = s.FirstName ?? string.Empty,
            OtherGivenNames = s.OtherGivenNames ?? string.Empty,
            FamilyName = s.FamilyName ?? string.Empty,
            PreviousFamilyName = s.PreviousFamilyName ?? string.Empty,
            DateOfBirth = DatabaseHelper.FormatDateAPI(s.DateOfBirth),
            Gender = s.Gender ?? Gender.NotKnown,
            AddressLine1 = s.AddressLine1 ?? string.Empty,
            AddressLine2 = s.AddressLine2 ?? string.Empty,
            AddressLine3 = s.AddressLine3 ?? string.Empty,
            AddressLine4 = s.AddressLine4 ?? string.Empty,
            AddressLine5 = s.AddressLine5 ?? string.Empty,
            Postcode = s.Postcode ?? string.Empty,
            UsualAddressEffectiveFromDate = DatabaseHelper.FormatDateAPI(s.UsualAddressEffectiveFromDate),
            DateOfDeath = DatabaseHelper.FormatDateAPI(s.DateOfDeath),
            TelephoneNumber = s.TelephoneNumber ?? string.Empty,
            TelephoneNumberEffectiveFromDate = DatabaseHelper.FormatDateAPI(s.TelephoneNumberEffectiveFromDate),
            MobileNumber = s.MobileNumber ?? string.Empty,
            MobileNumberEffectiveFromDate = DatabaseHelper.FormatDateAPI(s.MobileNumberEffectiveFromDate) ?? string.Empty,
            EmailAddress = s.EmailAddress ?? string.Empty,
            EmailAddressEffectiveFromDate = DatabaseHelper.FormatDateAPI(s.EmailAddressEffectiveFromDate) ?? string.Empty,
            PreferredLanguage = s.PreferredLanguage ?? string.Empty,
            IsInterpreterRequired = int.TryParse(s.IsInterpreterRequired, out var isInterpreterRequired) ? isInterpreterRequired : 0,
            ReasonForRemoval = s.ReasonForRemoval ?? string.Empty,
            ReasonForRemovalEffectiveFromDate = DatabaseHelper.FormatDateAPI(s.ReasonForRemovalEffectiveFromDate),
            ParticipantId = s.ParticipantId ?? string.Empty,
            IsExtracted = s.Extracted ?? string.Empty,
        }).ToList();
    }

    private void LogRequestAudit(string requestId, int statusCode)
    {
        var SQLToExecuteInOrder = new List<SQLReturnModel>();
        string requestAuditSql = "INSERT INTO [dbo].[BS_SELECT_REQUEST_AUDIT] ( " +
            " REQUEST_ID, " +
            " STATUS_CODE," +
            " CREATED_DATETIME" +
            " ) VALUES( " +
            " @RequestId, " +
            " @StatusCode, " +
            " @CreatedDateTime" +
            " ) ";

        var parameters = new Dictionary<string, object>
        {
            {"@RequestId", requestId}  ,
            {"@StatusCode", statusCode},
            {"@CreatedDateTime", DateTime.Now}
        };

        SQLToExecuteInOrder.Add(new SQLReturnModel()
        {
            SQL = requestAuditSql,
            Parameters = parameters
        });

        UpdateRecords(SQLToExecuteInOrder);
    }

    public List<CohortDistributionParticipantDto> GetCohortDistributionParticipantsByRequestId(string requestId, int rowCount)
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
            " [CURRENT_POSTING], " +
            " [CURRENT_POSTING_FROM_DT], " +
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
            " [REASON_FOR_REMOVAL_FROM_DT], " +
            " [RECORD_INSERT_DATETIME], " +
            " [RECORD_UPDATE_DATETIME], " +
            " [IS_EXTRACTED], " +
            " [REQUEST_ID] " +
            " FROM [dbo].[BS_COHORT_DISTRIBUTION] " +
            " WHERE REQUEST_ID = @RequestId";

        var parameters = new Dictionary<string, object>
        {
            {"@RequestId", requestId },
            {"@RowCount", rowCount },
        };

        var command = CreateCommand(parameters);
        command.CommandText = SQL;

        var cohortList = GetParticipant(command);
        return CohortDistributionParticipantDto(cohortList, requestId);
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
                " [CURRENT_POSTING], " +
                " [CURRENT_POSTING_FROM_DT], " +
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
                " [REASON_FOR_REMOVAL_FROM_DT], " +
                " [RECORD_INSERT_DATETIME], " +
                " [RECORD_UPDATE_DATETIME], " +
                " [IS_EXTRACTED], " +
                " [REQUEST_ID], " +
                " [CURRENT_POSTING] " +
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
            " SET REASON_FOR_REMOVAL_FROM_DT = @recordEndDate " +
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
                    FamilyName = DatabaseHelper.GetStringValue(reader, "FAMILY_NAME"),
                    PreviousFamilyName = DatabaseHelper.GetStringValue(reader, "PREVIOUS_FAMILY_NAME"),
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
                    ReasonForRemovalEffectiveFromDate = DatabaseHelper.GetStringValue(reader, "REASON_FOR_REMOVAL_FROM_DT"),
                    RecordInsertDateTime = DatabaseHelper.GetStringValue(reader, "RECORD_INSERT_DATETIME"),
                    RecordUpdateDateTime = DatabaseHelper.GetStringValue(reader, "RECORD_UPDATE_DATETIME"),
                    Extracted = DatabaseHelper.GetStringValue(reader, "IS_EXTRACTED"),
                    RequestId = DatabaseHelper.GetStringValue(reader, "REQUEST_ID"),
                    CurrentPosting = DatabaseHelper.GetStringValue(reader, "CURRENT_POSTING"),
                };

                participants.Add(participant);
            }
            return participants;
        });
    }

    private bool MarkCohortDistributionParticipantsAsExtracted(List<CohortDistributionParticipant> cohortParticipants, string requestId, int screeningServiceId)
    {
        if (cohortParticipants == null || cohortParticipants.Count == 0) return false;

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
            participant.RequestId = requestId;
            participant.ScreeningServiceId = screeningServiceId.ToString();
            participant.ScreeningAcronym = nameof(ServiceProvider.BSS);
            participant.ScreeningName = EnumHelper.GetDisplayName(ServiceProvider.BSS);
        }

        return UpdateRecords(sqlToExecute);
    }

    public async Task<List<CohortRequestAudit>> GetCohortRequestAudit(string? requestId, string? statusCode, DateTime? dateFrom)
    {
        var sql = BuildCohortRequestAuditQuery(requestId, statusCode, dateFrom);
        var parameters = GetCohortRequestAuditParameters(requestId, statusCode, dateFrom);

        using var command = CreateCommand(parameters);
        command.CommandText = sql;

        return await Task.FromResult(ExecuteQuery(command, ReadCohortRequestAudit));
    }

    public List<CohortRequestAudit> GetOutstandingCohortRequestAudits(string lastRequestId)
    {
        var sql = "SELECT [REQUEST_ID], [STATUS_CODE], [CREATED_DATETIME] " +
            " FROM [dbo].[BS_SELECT_REQUEST_AUDIT] " +
            " WHERE CREATED_DATETIME >= ( " +
            "SELECT CREATED_DATETIME " +
            "FROM [dbo].[BS_SELECT_REQUEST_AUDIT] " +
            "WHERE REQUEST_ID = @lastRequestId)";

        var parameters = new Dictionary<string, object>
        {
            {"@LastRequestId", lastRequestId},
        };

        using var command = CreateCommand(parameters);
        command.CommandText = sql;

        return ExecuteQuery(command, ReadCohortRequestAudit);
    }

    public List<CohortDistributionParticipantDto> GetParticipantsByRequestIds(List<string> requestIdsList, int rowCount)
    {
        if (requestIdsList.Count == 0) return GetUnextractedCohortDistributionParticipantsByScreeningServiceId((int)ServiceProvider.BSS, rowCount);

        return requestIdsList.SelectMany(GetCohortDistributionParticipantsByRequestId).ToList();
    }

    private static string BuildCohortRequestAuditQuery(string? requestId, string? statusCode, DateTime? dateFrom)
    {
        var SQL = "SELECT" +
            " [REQUEST_ID], " +
            " [STATUS_CODE], " +
            " [CREATED_DATETIME] " +
            " FROM [dbo].[BS_SELECT_REQUEST_AUDIT] " +
            " ORDER BY CREATED_DATETIME DESC";

        var conditions = new List<string>();

        if (dateFrom.HasValue)
        {
            conditions.Add("CREATED_DATETIME >= @DateFrom");
        }

        if (!string.IsNullOrEmpty(statusCode))
        {
            conditions.Add("STATUS_CODE = @StatusCode");
        }

        if (!string.IsNullOrEmpty(requestId))
        {
            conditions.Add("REQUEST_ID = @RequestId");
        }

        if (conditions.Count > 0)
        {
            SQL += " WHERE " + string.Join(" AND ", conditions);
        }

        return SQL;
    }

    private static Dictionary<string, object> GetCohortRequestAuditParameters(string? requestId, string? statusCode, DateTime? dateFrom)
    {
        var parameters = new Dictionary<string, object>();

        if (dateFrom.HasValue)
            parameters.Add("@DateFrom", dateFrom.Value);

        if (!string.IsNullOrEmpty(statusCode))
            parameters.Add("@StatusCode", statusCode);

        if (!string.IsNullOrEmpty(requestId))
            parameters.Add("@RequestId", requestId);

        return parameters;
    }

    private List<CohortRequestAudit> ReadCohortRequestAudit(IDataReader reader)
    {
        var cohortRequestAuditList = new List<CohortRequestAudit>();

        while (reader.Read())
        {
            cohortRequestAuditList.Add(new CohortRequestAudit
            {
                RequestId = DatabaseHelper.GetStringValue(reader, "REQUEST_ID"),
                StatusCode = DatabaseHelper.GetStringValue(reader, "STATUS_CODE"),
                CreatedDateTime = DatabaseHelper.GetStringValue(reader, "CREATED_DATETIME"),
            });
        }

        return cohortRequestAuditList;
    }

    private bool UpdateRecords(List<SQLReturnModel> sqlToExecute)
    {
        var transaction = BeginTransaction();
        try
        {
            foreach (var sqlCommand in sqlToExecute)
            {
                var command = CreateCommand(sqlCommand.Parameters);
                command.CommandText = sqlCommand.SQL;
                command.Transaction = transaction;

                if (!Execute(command))
                {
                    transaction.Rollback();
                    return false;
                }
            }

            transaction.Commit();
            return true;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError(ex, "An error occurred while inserting new Cohort Distribution records: {ExceptionMessage}", ex.Message);
            return false;
        }
        finally
        {
            if (_dbConnection != null)
            {
                _dbConnection.Close();
            }
        }
    }

    private T ExecuteQuery<T>(IDbCommand command, Func<IDataReader, T> mapFunction)
    {
        var result = default(T);
        try
        {
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
                }
                return result;
            }
        }
        finally
        {
            if (_dbConnection != null)
            {
                _dbConnection.Close();
            }
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

    private static IDbCommand AddParameters(Dictionary<string, object> parameters, IDbCommand dbCommand)
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
            _logger.LogInformation("ExecuteNonQuery result: {Result}", result);

            if (result == 0)
            {
                _logger.LogError("No rows affected by ExecuteNonQuery.");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in Execute method: {ExceptionMessage}", ex.Message);
            return false;
        }

        return true;
    }
}
