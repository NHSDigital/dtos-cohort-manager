namespace Data.Database;

using System.Data;
using System.Net;
using System.Threading.Tasks;
using Common.Interfaces;
using Microsoft.Extensions.Logging;
using Model;
using Model.DTO;
using Model.Enums;

public class CreateCohortDistributionData : ICreateCohortDistributionData
{
    private readonly IDbConnection _dbConnection;
    private readonly string _connectionString;
    private readonly ILogger<CreateCohortDistributionData> _logger;

    public CreateCohortDistributionData(IDbConnection IdbConnection, ILogger<CreateCohortDistributionData> logger)
    {
        _dbConnection = IdbConnection;
        _logger = logger;
        _connectionString = Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString") ?? string.Empty;
    }


    public List<CohortDistributionParticipantDto> GetUnextractedCohortDistributionParticipants(int rowCount)
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
            " WHERE bcd.IS_EXTRACTED = @Extracted " +
            " AND REQUEST_ID IS NULL " +
            " ORDER BY bcd.RECORD_INSERT_DATETIME ASC ";

        var parameters = new Dictionary<string, object>
        {
            {"@RowCount", rowCount },
            {"@Extracted", 0 },
        };

        var command = CreateCommand(parameters);
        command.CommandText = SQL;

        var participantsList = GetParticipant(command);
        var requestId = Guid.NewGuid().ToString();
        if (MarkCohortDistributionParticipantsAsExtracted(participantsList, requestId))
        {
            LogRequestAudit(requestId, (int)HttpStatusCode.OK);
            return CohortDistributionParticipantDto(participantsList);
        }

        var statusCode = participantsList.Count == 0 ? (int)HttpStatusCode.NoContent : (int)HttpStatusCode.InternalServerError;
        LogRequestAudit(requestId, statusCode);

        return new List<CohortDistributionParticipantDto>();
    }

    private static List<CohortDistributionParticipantDto> CohortDistributionParticipantDto(List<CohortDistributionParticipant> listOfAllParticipants)
    {
        return listOfAllParticipants.Select(s => new CohortDistributionParticipantDto
        {
            RequestId = s.RequestId ?? string.Empty,
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

    public List<CohortDistributionParticipantDto> GetCohortDistributionParticipantsByRequestId(string requestId)
    {
        var SQL = "SELECT " +
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
        };

        var command = CreateCommand(parameters);
        command.CommandText = SQL;

        var cohortList = GetParticipant(command);
        return CohortDistributionParticipantDto(cohortList);
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
                var participant = new CohortDistributionParticipant()
                {
                    ParticipantId = DatabaseHelper.GetValue<string>(reader, "PARTICIPANT_ID"),
                    NhsNumber = DatabaseHelper.GetValue<string>(reader, "NHS_NUMBER"),
                    SupersededByNhsNumber = DatabaseHelper.GetValue<string>(reader, "SUPERSEDED_NHS_NUMBER"),
                    PrimaryCareProvider = DatabaseHelper.GetValue<string>(reader, "PRIMARY_CARE_PROVIDER"),
                    PrimaryCareProviderEffectiveFromDate = DatabaseHelper.GetValue<string>(reader, "PRIMARY_CARE_PROVIDER_FROM_DT"),
                    NamePrefix = DatabaseHelper.GetValue<string>(reader, "NAME_PREFIX"),
                    FirstName = DatabaseHelper.GetValue<string>(reader, "GIVEN_NAME"),
                    OtherGivenNames = DatabaseHelper.GetValue<string>(reader, "OTHER_GIVEN_NAME"),
                    FamilyName = DatabaseHelper.GetValue<string>(reader, "FAMILY_NAME"),
                    PreviousFamilyName = DatabaseHelper.GetValue<string>(reader, "PREVIOUS_FAMILY_NAME"),
                    DateOfBirth = DatabaseHelper.GetValue<string>(reader, "DATE_OF_BIRTH"),
                    Gender = DatabaseHelper.GetValue<Gender>(reader, "GENDER"),
                    AddressLine1 = DatabaseHelper.GetValue<string>(reader, "ADDRESS_LINE_1"),
                    AddressLine2 = DatabaseHelper.GetValue<string>(reader, "ADDRESS_LINE_2"),
                    AddressLine3 = DatabaseHelper.GetValue<string>(reader, "ADDRESS_LINE_3"),
                    AddressLine4 = DatabaseHelper.GetValue<string>(reader, "ADDRESS_LINE_4"),
                    AddressLine5 = DatabaseHelper.GetValue<string>(reader, "ADDRESS_LINE_5"),
                    Postcode = DatabaseHelper.GetValue<string>(reader, "POST_CODE"),
                    UsualAddressEffectiveFromDate = DatabaseHelper.GetValue<string>(reader, "USUAL_ADDRESS_FROM_DT"),
                    DateOfDeath = DatabaseHelper.GetValue<string>(reader, "DATE_OF_DEATH"),
                    TelephoneNumber = DatabaseHelper.GetValue<string>(reader, "TELEPHONE_NUMBER_HOME"),
                    TelephoneNumberEffectiveFromDate = DatabaseHelper.GetValue<string>(reader, "TELEPHONE_NUMBER_HOME_FROM_DT"),
                    MobileNumber = DatabaseHelper.GetValue<string>(reader, "TELEPHONE_NUMBER_MOB"),
                    MobileNumberEffectiveFromDate = DatabaseHelper.GetValue<string>(reader, "TELEPHONE_NUMBER_MOB_FROM_DT"),
                    EmailAddress = DatabaseHelper.GetValue<string>(reader, "EMAIL_ADDRESS_HOME"),
                    EmailAddressEffectiveFromDate = DatabaseHelper.GetValue<string>(reader, "EMAIL_ADDRESS_HOME_FROM_DT"),
                    PreferredLanguage = DatabaseHelper.GetValue<string>(reader, "PREFERRED_LANGUAGE"),
                    IsInterpreterRequired = DatabaseHelper.GetValue<string>(reader, "INTERPRETER_REQUIRED"),
                    ReasonForRemoval = DatabaseHelper.GetValue<string>(reader, "REASON_FOR_REMOVAL"),
                    ReasonForRemovalEffectiveFromDate = DatabaseHelper.GetValue<string>(reader, "REASON_FOR_REMOVAL_FROM_DT"),
                    RecordInsertDateTime = DatabaseHelper.GetValue<string>(reader, "RECORD_INSERT_DATETIME"),
                    RecordUpdateDateTime = DatabaseHelper.GetValue<string>(reader, "RECORD_UPDATE_DATETIME"),
                    Extracted = DatabaseHelper.GetValue<string>(reader, "IS_EXTRACTED"),
                    RequestId = DatabaseHelper.GetValue<string>(reader, "REQUEST_ID"),
                    CurrentPosting = DatabaseHelper.GetValue<string>(reader, "CURRENT_POSTING"),
                };

                participants.Add(participant);
            }
            return participants;
        });
    }

    private bool MarkCohortDistributionParticipantsAsExtracted(List<CohortDistributionParticipant> cohortParticipants, string requestId)
    {
        if (cohortParticipants == null || cohortParticipants.Count == 0) return false;

        var SQL = $@" WITH AllUnextractedParticipants AS (
        SELECT
        PARTICIPANT_ID,
        RECORD_INSERT_DATETIME,
        ROW_NUMBER() OVER (
        ORDER BY RECORD_INSERT_DATETIME ASC
        ) AS OverallRowNum
        FROM BS_COHORT_DISTRIBUTION
        WHERE IS_EXTRACTED = 0
        AND REQUEST_ID IS NULL),
        FilteredCohortDistribution AS (
        SELECT TOP (@RowCount)
        PARTICIPANT_ID,
        RECORD_INSERT_DATETIME
        FROM AllUnextractedParticipants
        ORDER BY OverallRowNum)
        UPDATE cd
        SET IS_EXTRACTED = @Extracted,
        REQUEST_ID = @RequestId
        FROM BS_COHORT_DISTRIBUTION cd
        INNER JOIN FilteredCohortDistribution fcd
        ON cd.PARTICIPANT_ID = fcd.PARTICIPANT_ID
        AND cd.RECORD_INSERT_DATETIME = fcd.RECORD_INSERT_DATETIME";

        var parameters = new Dictionary<string, object>
        {
            { "@Extracted", 1 },
            { "@RequestId", requestId },
            { "@RowCount", cohortParticipants.Count }
        };

        var sqlToExecute = new List<SQLReturnModel>
        {
            new SQLReturnModel
            {
                Parameters = parameters,
                SQL = SQL
            }
        };

        foreach (var participant in cohortParticipants)
        {
            participant.RequestId = requestId;
            participant.Extracted = "1";
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

    public CohortRequestAudit GetNextCohortRequestAudit(string requestId)
    {
        if (!Guid.TryParse(requestId, out Guid requestIdGuid))
        {
            return new CohortRequestAudit();
        }

        var sql = "SELECT TOP 1 [REQUEST_ID], [STATUS_CODE], [CREATED_DATETIME] " +
                  "FROM [dbo].[BS_SELECT_REQUEST_AUDIT] " +
                  "WHERE (CREATED_DATETIME > ( " +
                  "SELECT CREATED_DATETIME " +
                  "FROM [dbo].[BS_SELECT_REQUEST_AUDIT] " +
                  "WHERE REQUEST_ID = @RequestId) " +
                  "OR (CREATED_DATETIME = ( " +
                  "SELECT CREATED_DATETIME " +
                  "FROM [dbo].[BS_SELECT_REQUEST_AUDIT] " +
                  "WHERE REQUEST_ID = @RequestId) " +
                  "AND REQUEST_ID > @RequestId)) " +
                  "AND STATUS_CODE != @StatusCode " +
                  "ORDER BY CREATED_DATETIME ASC, REQUEST_ID ASC";

        var parameters = new Dictionary<string, object>
        {
            {"@RequestId", requestIdGuid},
            {"@StatusCode", HttpStatusCode.NoContent.ToString()}
        };

        using var command = CreateCommand(parameters);
        command.CommandText = sql;

        return ExecuteQuery(command, ReadCohortRequestAudit).FirstOrDefault();
    }

    private static string BuildCohortRequestAuditQuery(string? requestId, string? statusCode, DateTime? dateFrom)
    {
        var SQL = "SELECT" +
            " [REQUEST_ID], " +
            " [STATUS_CODE], " +
            " [CREATED_DATETIME] " +
            " FROM [dbo].[BS_SELECT_REQUEST_AUDIT] ";

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

        SQL += " ORDER BY CREATED_DATETIME DESC";

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
                RequestId = DatabaseHelper.GetValue<string>(reader, "REQUEST_ID"),
                StatusCode = DatabaseHelper.GetValue<string>(reader, "STATUS_CODE"),
                CreatedDateTime = DatabaseHelper.GetValue<string>(reader, "CREATED_DATETIME"),
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
