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
        var SQL = " SELECT " +
            " [SCREENING_ID], " +
            " [SCREENING_NAME] " +
            " FROM [DBO].[SCREENING_LKP] " +
            " WHERE [SCREENING_ACRONYM] = @ScreeningAcronym ";

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
                screeningService.ScreeningId = DatabaseHelper.GetStringValue(reader, "SCREENING_ID");
                screeningService.ScreeningName = DatabaseHelper.GetStringValue(reader, "SCREENING_NAME");
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
