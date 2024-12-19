namespace Data.Database;

using System.Data;
using Microsoft.Extensions.Logging;
using Model;
using Microsoft.Data.SqlClient;
using Model.Enums;
using Microsoft.Extensions.Options;

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

    public Demographic GetDemographicData(string nhsNumber)
    {
        var SQL = @" SELECT TOP (1) [PARTICIPANT_ID]
                    ,[NHS_NUMBER]
                    ,[SUPERSEDED_BY_NHS_NUMBER]
                    ,[PRIMARY_CARE_PROVIDER]
                    ,[PRIMARY_CARE_PROVIDER_FROM_DT]
                    ,[CURRENT_POSTING]
                    ,[CURRENT_POSTING_FROM_DT]
                    ,[NAME_PREFIX]
                    ,[GIVEN_NAME]
                    ,[OTHER_GIVEN_NAME]
                    ,[FAMILY_NAME]
                    ,[PREVIOUS_FAMILY_NAME]
                    ,[DATE_OF_BIRTH]
                    ,[GENDER]
                    ,[ADDRESS_LINE_1]
                    ,[ADDRESS_LINE_2]
                    ,[ADDRESS_LINE_3]
                    ,[ADDRESS_LINE_4]
                    ,[ADDRESS_LINE_5]
                    ,[POST_CODE]
                    ,[PAF_KEY]
                    ,[USUAL_ADDRESS_FROM_DT]
                    ,[DATE_OF_DEATH]
                    ,[DEATH_STATUS]
                    ,[TELEPHONE_NUMBER_HOME]
                    ,[TELEPHONE_NUMBER_HOME_FROM_DT]
                    ,[TELEPHONE_NUMBER_MOB]
                    ,[TELEPHONE_NUMBER_MOB_FROM_DT]
                    ,[EMAIL_ADDRESS_HOME]
                    ,[EMAIL_ADDRESS_HOME_FROM_DT]
                    ,[PREFERRED_LANGUAGE]
                    ,[INTERPRETER_REQUIRED]
                    ,[INVALID_FLAG]
                    ,[RECORD_INSERT_DATETIME]
                    ,[RECORD_UPDATE_DATETIME]
                FROM [dbo].[PARTICIPANT_DEMOGRAPHIC]
                WHERE NHS_NUMBER = @NhsNumber ORDER BY PARTICIPANT_ID DESC ";
        var parameters = new Dictionary<string, object>()
        {
            {"@NhsNumber",  nhsNumber },
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
                demographic.NamePrefix = reader["NAME_PREFIX"] == DBNull.Value ? null : reader["NAME_PREFIX"].ToString();
                demographic.FirstName = reader["GIVEN_NAME"] == DBNull.Value ? null : reader["GIVEN_NAME"].ToString();
                demographic.OtherGivenNames = reader["OTHER_GIVEN_NAME"] == DBNull.Value ? null : reader["OTHER_GIVEN_NAME"].ToString();
                demographic.FamilyName = reader["FAMILY_NAME"] == DBNull.Value ? null : reader["FAMILY_NAME"].ToString();
                demographic.PreviousFamilyName = reader["PREVIOUS_FAMILY_NAME"] == DBNull.Value ? null : reader["PREVIOUS_FAMILY_NAME"].ToString();
                demographic.DateOfBirth = reader["DATE_OF_BIRTH"] == DBNull.Value ? null : reader["DATE_OF_BIRTH"].ToString();
                demographic.Gender = reader["GENDER"] == DBNull.Value ? null : (Gender)reader["GENDER"];
                demographic.AddressLine1 = reader["ADDRESS_LINE_1"] == DBNull.Value ? null : reader["ADDRESS_LINE_1"].ToString();
                demographic.AddressLine2 = reader["ADDRESS_LINE_2"] == DBNull.Value ? null : reader["ADDRESS_LINE_2"].ToString();
                demographic.AddressLine3 = reader["ADDRESS_LINE_3"] == DBNull.Value ? null : reader["ADDRESS_LINE_3"].ToString();
                demographic.AddressLine4 = reader["ADDRESS_LINE_4"] == DBNull.Value ? null : reader["ADDRESS_LINE_4"].ToString();
                demographic.AddressLine5 = reader["ADDRESS_LINE_5"] == DBNull.Value ? null : reader["ADDRESS_LINE_5"].ToString();
                demographic.Postcode = reader["POST_CODE"] == DBNull.Value ? null : reader["POST_CODE"].ToString();
                demographic.PafKey = reader["PAF_KEY"] == DBNull.Value ? null : reader["PAF_KEY"].ToString();
                demographic.UsualAddressEffectiveFromDate = reader["USUAL_ADDRESS_FROM_DT"] == DBNull.Value ? null : reader["USUAL_ADDRESS_FROM_DT"].ToString();
                demographic.DateOfDeath = reader["DATE_OF_DEATH"] == DBNull.Value ? null : reader["DATE_OF_DEATH"].ToString();
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
                demographic.RecordInsertDateTime = reader["RECORD_INSERT_DATETIME"] == DBNull.Value ? null : reader["RECORD_INSERT_DATETIME"].ToString();
                demographic.RecordUpdateDateTime = reader["RECORD_UPDATE_DATETIME"] == DBNull.Value ? null : reader["RECORD_UPDATE_DATETIME"].ToString();
            }
            return demographic;
        });
    }

    private T ExecuteQuery<T>(IDbCommand command, Func<IDataReader, T> mapFunction)
    {
        try
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

    private async Task<bool> UpdateRecords(List<SQLReturnModel> sqlToExecute)
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
                    return false;
                }
            }

            await Task.CompletedTask;
            transaction.Commit();

            return true;
        }
        catch
        {
            transaction.Rollback();
            // we need to rethrow the exception here if there is an error we need to roll back the transaction.
            throw;
        }
        finally
        {
            _dbConnection.Close();
        }
    }

    private bool Execute(IDbCommand command)
    {
        var result = command.ExecuteNonQuery();
        if (result == 0)
        {
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
