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
            " LOAD_DATE, " +
            " EXTRACTED " +
            "  " +
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
            " @LoadDate, " +
            " @Extracted" +
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
            { "@Extracted", '0' },
        };

        SQLToExecuteInOrder.Add(new SQLReturnModel()
        {
            CommandType = Data.Database.CommandType.Scalar,
            SQL = insertParticipant,
            Parameters = parameters
        });

        return UpdateRecords(SQLToExecuteInOrder);
    }

    public List<AggregateParticipant> ExtractAggregateParticipants()
    {
        var SQL = "SELECT TOP (1000) * " +
                " FROM [dbo].[AGGREGATION_DATA] " +
                " WHERE EXTRACTED = @Extracted ";

        var parameters = new Dictionary<string, object>
        {
            {"@Extracted", '0' },
        };

        var command = CreateCommand(parameters);
        command.CommandText = SQL;

        var listOfAllAggregates = GetParticipant(command);

        if (MarkAggregateParticipantsAsExtracted(listOfAllAggregates))
        {
            return listOfAllAggregates;
        }

        return null;
    }

    private List<AggregateParticipant> GetParticipant(IDbCommand command)
    {
        List<AggregateParticipant> participants = new List<AggregateParticipant>();

        return ExecuteQuery(command, reader =>
        {
            while (reader.Read())
            {
                var participant = new AggregateParticipant
                {
                    AggregateId = reader["AGGREGATION_ID"] == DBNull.Value ? "-1" : reader["AGGREGATION_ID"].ToString(),
                    NhsNumber = reader["NHS_NUMBER"] == DBNull.Value ? null : reader["NHS_NUMBER"].ToString(),
                    SupersededByNhsNumber = reader["SUPERSEDED_BY_NHS_NUMBER"] == DBNull.Value ? null : reader["SUPERSEDED_BY_NHS_NUMBER"].ToString(),
                    PrimaryCareProvider = reader["PRIMARY_CARE_PROVIDER"] == DBNull.Value ? null : reader["PRIMARY_CARE_PROVIDER"].ToString(),
                    NamePrefix = reader["PARTICIPANT_PREFIX"] == DBNull.Value ? null : reader["PARTICIPANT_PREFIX"].ToString(),
                    FirstName = reader["PARTICIPANT_FIRST_NAME"] == DBNull.Value ? null : reader["PARTICIPANT_FIRST_NAME"].ToString(),
                    OtherGivenNames = reader["OTHER_NAME"] == DBNull.Value ? null : reader["OTHER_NAME"].ToString(),
                    Surname = reader["PARTICIPANT_LAST_NAME"] == DBNull.Value ? null : reader["PARTICIPANT_LAST_NAME"].ToString(),
                    DateOfBirth = reader["PARTICIPANT_BIRTH_DATE"] == DBNull.Value ? null : reader["PARTICIPANT_BIRTH_DATE"].ToString(),
                    Gender = reader["PARTICIPANT_GENDER"] == DBNull.Value ? Model.Enums.Gender.NotKnown : (Model.Enums.Gender)int.Parse(reader["PARTICIPANT_GENDER"].ToString()),
                    ReasonForRemoval = reader["REASON_FOR_REMOVAL_CD"] == DBNull.Value ? null : reader["REASON_FOR_REMOVAL_CD"].ToString(),
                    ReasonForRemovalEffectiveFromDate = reader["REMOVAL_DATE"] == DBNull.Value ? null : reader["REMOVAL_DATE"].ToString(),
                    DateOfDeath = reader["PARTICIPANT_DEATH_DATE"] == DBNull.Value ? null : reader["PARTICIPANT_DEATH_DATE"].ToString(),
                    Extracted = reader["EXTRACTED"] == DBNull.Value ? null : reader["EXTRACTED"].ToString()
                };

                participants.Add(participant);
            }
            return participants;

        });
    }

    private bool MarkAggregateParticipantsAsExtracted(List<AggregateParticipant> aggregateParticipants)
    {
        if (aggregateParticipants.Count == 0)
        {
            return false;
        }
        var SQL = " UPDATE [dbo].[AGGREGATION_DATA] " +
                " SET EXTRACTED = @Extracted " +
                " WHERE AGGREGATION_ID >= @FirstId and AGGREGATION_ID <= @LastId";

        var parameters = new Dictionary<string, object>
        {
            {"@FirstId", aggregateParticipants.FirstOrDefault().AggregateId },
            {"@LastId", aggregateParticipants.LastOrDefault().AggregateId },
            {"@Extracted", '1' },
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
