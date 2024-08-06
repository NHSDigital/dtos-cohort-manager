namespace Data.Database;

using System.Data;
using Model;
using Microsoft.Extensions.Logging;

public class ScreeningServiceData : IScreeningServiceData
{
    private readonly IDbConnection _dbConnection;
    private readonly string _connectionString;
    private readonly ILogger<ScreeningServiceData> _logger;

    public ScreeningServiceData(IDbConnection dbConnection, ILogger<ScreeningServiceData> logger)
    {
        _dbConnection = dbConnection;
        _logger = logger;
        _connectionString = Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString") ?? string.Empty;
    }

    public ScreeningService GetScreeningServiceByAcronym(string screeningAcronym)
    {
        var SQL = "SELECT * " +
        " FROM [SCREENING_LKP] " +
        " WHERE [SCREENING_LKP].[SCREENING_ACRONYM] = @ScreeningAcronym";

        var parameters = new Dictionary<string, object>
        {
            {"@ScreeningAcronym", screeningAcronym }
        };

        var command = CreateCommand(parameters);
        command.CommandText = SQL;

        return GetScreeningService(command);
    }

    private ScreeningService GetScreeningService(IDbCommand command)
    {
        return ExecuteQuery(command, reader =>
        {
            var screeningService = new ScreeningService();
            while (reader.Read())
            {
                screeningService.ScreeningId = reader["SCREENING_ID"] == DBNull.Value ? null : reader["SCREENING_ID"].ToString();
                screeningService.ScreeningName = reader["SCREENING_NAME"] == DBNull.Value ? null : reader["SCREENING_NAME"].ToString();
                screeningService.ScreeningType = reader["SCREENING_TYPE"] == DBNull.Value ? null : reader["SCREENING_TYPE"].ToString();
                screeningService.ScreeningAcronym = reader["SCREENING_ACRONYM"] == DBNull.Value ? null : reader["SCREENING_ACRONYM"].ToString();
            }
            return screeningService;
        });
    }

    private IDbCommand CreateCommand(Dictionary<string, object> parameters)
    {
        var dbCommand = _dbConnection.CreateCommand();
        return AddParameters(parameters, dbCommand);
    }

    private IDbCommand AddParameters(Dictionary<string, object> parameters, IDbCommand dbCommand)
    {
        if (parameters == null) return dbCommand;

        foreach (var param in parameters)
        {
            var parameter = dbCommand.CreateParameter();

            parameter.ParameterName = param.Key;
            parameter.Value = param.Value;

            dbCommand.Parameters.Add(parameter);
        }

        return dbCommand;
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
}
