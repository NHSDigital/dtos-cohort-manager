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
