namespace Data.Database;

using System.Data;
using Microsoft.Extensions.Logging;
using Model;
using Common;
using System.Text.Json;
using System.Net;
using Model.Enums;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.IdentityModel.Tokens;

public class UpdateAggregateData : IUpdateAggregateData
{
    private readonly IDbConnection _dbConnection;

    private readonly string _connectionString;
    private readonly ILogger<UpdateAggregateData> _logger;

    public UpdateAggregateData(IDbConnection IdbConnection, ILogger<UpdateAggregateData> logger)
    {
        _connectionString = Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString") ?? string.Empty;
        _dbConnection = IdbConnection;
        _dbConnection.ConnectionString =_connectionString;

        _logger = logger;

    }

    public bool UpdateAggregateParticipantAsInactive(string NHSID)
    {

        _logger.LogInformation("Updating Aggregate Participant as Inactive");

        if(NHSID.IsNullOrEmpty())
        {
            _logger.LogError("No NHSID was Provided");
            return false;
        }

        try{
            var recordEndDate = DateTime.Today;

            var SQL = " UPDATE [dbo].[AGGREGATION_DATA] " +
                " SET RECORD_END_DATE = @recordEndDate, " +
                " ACTIVE_FLAG = 'N' " +
                " WHERE NHS_NUMBER = @NHSID  ";
            var Parameters = new Dictionary<string, object>
            {
                {"@NHSID", NHSID},
                {"@recordEndDate",recordEndDate}
            };



            var command = CreateCommand(Parameters) ;
            command.CommandText = SQL;

            var transaction = BeginTransaction();
            command.Transaction = transaction;
            if(!Execute(command)){
                transaction.Rollback();
                return false;
            }

            transaction.Commit();

            return true;
        }
        catch(Exception ex){
            _logger.LogError(ex,"An error occurred while updating records: {ex}",ex);
            return false;
        }
        finally
        {
            _dbConnection.Close();
        }

    }

#region PrivateMethods
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

    private IDbTransaction BeginTransaction()
    {
        _dbConnection.ConnectionString = _connectionString;
        _dbConnection.Open();
        return _dbConnection.BeginTransaction();
    }


#endregion








}
