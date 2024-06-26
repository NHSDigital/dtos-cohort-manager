namespace Data.Database;


using System.Data;
using Common;
using Data.Database;
using Microsoft.Extensions.Logging;
using Model;

public class CreateAggregationData : ICreateAggregationData
{

    private readonly IDbConnection _dbConnection;
    private readonly IDatabaseHelper _databaseHelper;
    private readonly string _connectionString;
    private readonly ILogger<CreateAggregationData> _logger;

    public CreateAggregationData(IDbConnection IdbConnection, IDatabaseHelper databaseHelper, ILogger<CreateAggregationData> logger)
    {
        _dbConnection = IdbConnection;
        _databaseHelper = databaseHelper;
        _logger = logger;
        _connectionString = Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString") ?? string.Empty;
    }


    public bool InsertAggregationData(AggregateParticipant aggregateParticipant)
    {

        var cohortId = 1;

        var dateToday = DateTime.Today;
        var maxEndDate = DateTime.MaxValue;

        var SQLToExecuteInOrder = new List<SQLReturnModel>();
        string insertParticipant = "INSERT INTO [dbo].[AGGREGATION_DATA] ( " +
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

        var parameters = new Dictionary<string, object>
        {
            {"@cohortId", cohortId},
            {"@gender", aggregateParticipant.Gender},
            {"@NHSNumber", aggregateParticipant.NhsNumber },
            {"@supersededByNhsNumber", _databaseHelper.CheckIfNumberNull(aggregateParticipant.SupersededByNhsNumber) ? DBNull.Value : aggregateParticipant.SupersededByNhsNumber},
            {"@dateOfBirth", _databaseHelper.CheckIfDateNull(aggregateParticipant.DateOfBirth) ? DateTime.MaxValue : _databaseHelper.ParseDates(aggregateParticipant.DateOfBirth)},
            { "@dateOfDeath", _databaseHelper.CheckIfDateNull(aggregateParticipant.DateOfDeath) ? DBNull.Value : _databaseHelper.ParseDates(aggregateParticipant.DateOfDeath)},
            { "@namePrefix",  _databaseHelper.ConvertNullToDbNull(aggregateParticipant.NamePrefix) },
            { "@firstName", _databaseHelper.ConvertNullToDbNull(aggregateParticipant.FirstName) },
            { "@surname", _databaseHelper.ConvertNullToDbNull(aggregateParticipant.Surname) },
            { "@otherGivenNames", _databaseHelper.ConvertNullToDbNull(aggregateParticipant.OtherGivenNames) },
            { "@gpConnect", _databaseHelper.ConvertNullToDbNull(aggregateParticipant.PrimaryCareProvider) },
            { "@primaryCareProvider", _databaseHelper.ConvertNullToDbNull(aggregateParticipant.PrimaryCareProvider) },
            { "@reasonForRemoval", _databaseHelper.ConvertNullToDbNull(aggregateParticipant.ReasonForRemoval) },
            { "@removalDate", _databaseHelper.CheckIfDateNull(aggregateParticipant.ReasonForRemovalEffectiveFromDate) ? DBNull.Value : _databaseHelper.ParseDateToString(aggregateParticipant.ReasonForRemovalEffectiveFromDate)},
            { "@RecordStartDate", dateToday},
            { "@RecordEndDate", maxEndDate},
            { "@ActiveFlag", 'Y'},
            { "@LoadDate", dateToday },
        };

        SQLToExecuteInOrder.Add(new SQLReturnModel()
        {
            CommandType = Data.Database.CommandType.Scalar,
            SQL = insertParticipant,
            Parameters = parameters
        });

        return UpdateRecords(SQLToExecuteInOrder);
    }

    public List<AggregateParticipant> GetParticipant(string NHSId)
    {
        var SQL = "SELECT TOP (1000) * " +
                " FROM [dbo].[AGGREGATION_DATA] " +
                " WHERE EXTRACTED != @Extracted ";

        var parameters = new Dictionary<string, object>
        {
            {"@Extracted", '0' },
        };

        var command = CreateCommand(parameters);
        command.CommandText = SQL;

        return GetParticipant(command);
    }

    public List<AggregateParticipant> GetParticipant(IDbCommand command)
    {
        List<AggregateParticipant> participants = new List<AggregateParticipant>();
        return ExecuteQuery(command, reader =>
        {
            var participant = new AggregateParticipant();
            while (reader.Read())
            {
                participant.ParticipantId = reader["PARTICIPANT_ID"] == DBNull.Value ? "-1" : reader["PARTICIPANT_ID"].ToString();
                participant.NHSId = reader["NHS_NUMBER"] == DBNull.Value ? null : reader["NHS_NUMBER"].ToString();
                participant.SupersededByNhsNumber = reader["SUPERSEDED_BY_NHS_NUMBER"] == DBNull.Value ? null : reader["SUPERSEDED_BY_NHS_NUMBER"].ToString();
                participant.PrimaryCareProvider = reader["PRIMARY_CARE_PROVIDER"] == DBNull.Value ? null : reader["PRIMARY_CARE_PROVIDER"].ToString();
                participant.NamePrefix = reader["PARTICIPANT_PREFIX"] == DBNull.Value ? null : reader["PARTICIPANT_PREFIX"].ToString();
                participant.FirstName = reader["PARTICIPANT_FIRST_NAME"] == DBNull.Value ? null : reader["PARTICIPANT_FIRST_NAME"].ToString();
                participant.OtherGivenNames = reader["OTHER_NAME"] == DBNull.Value ? null : reader["OTHER_NAME"].ToString();
                participant.Surname = reader["PARTICIPANT_LAST_NAME"] == DBNull.Value ? null : reader["PARTICIPANT_LAST_NAME"].ToString();
                participant.DateOfBirth = reader["PARTICIPANT_BIRTH_DATE"] == DBNull.Value ? null : reader["PARTICIPANT_BIRTH_DATE"].ToString();
                participant.Gender = reader["PARTICIPANT_GENDER"] == DBNull.Value ? Model.Enums.Gender.NotKnown : (Model.Enums.Gender)(int)reader["PARTICIPANT_GENDER"];
                participant.ReasonForRemoval = reader["REASON_FOR_REMOVAL_CD"] == DBNull.Value ? null : reader["REASON_FOR_REMOVAL_CD"].ToString();
                participant.ReasonForRemovalEffectiveFromDate = reader["REMOVAL_DATE"] == DBNull.Value ? null : reader["REMOVAL_DATE"].ToString();
                participant.DateOfDeath = reader["PARTICIPANT_DEATH_DATE"] == DBNull.Value ? null : reader["PARTICIPANT_DEATH_DATE"].ToString();
                participant.AddressLine1 = reader["ADDRESS_LINE_1"] == DBNull.Value ? null : reader["ADDRESS_LINE_1"].ToString();
                participant.AddressLine2 = reader["ADDRESS_LINE_2"] == DBNull.Value ? null : reader["ADDRESS_LINE_2"].ToString();
                participant.AddressLine3 = reader["CITY"] == DBNull.Value ? null : reader["CITY"].ToString();
                participant.AddressLine4 = reader["COUNTY"] == DBNull.Value ? null : reader["COUNTY"].ToString();
                participant.AddressLine5 = reader["POST_CODE"] == DBNull.Value ? null : reader["POST_CODE"].ToString();

                participants.Add(participant);
            }
            return participants;

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
            _logger.LogError($"An error occurred while inserting new aggregation records: {ex.Message}");
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
