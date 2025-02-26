using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace dtos_cohort_manager_specflow.Helpers;

public class SqlConnectionWithAuthentication
{
    private readonly string _connectionString;
    private readonly string? _managedIdentityClientId;
    private readonly bool _useManagedIdentity;
    private readonly ILogger<EndToEndFileUploadService> _logger;

    public SqlConnectionWithAuthentication(string connectionString, string? managedIdentityClientId, bool isCloudEnvironment, ILogger<EndToEndFileUploadService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;

        // Even if isCloudEnvironment is set to true if ManagedIdentityClientId is null or empty _useManagedIdentity will be false
        _useManagedIdentity = isCloudEnvironment && !string.IsNullOrEmpty(managedIdentityClientId);
        _managedIdentityClientId = _useManagedIdentity ? managedIdentityClientId : "";

        LogInputVariables();
    }

    private void LogInputVariables()
    {
        _logger.LogInformation("Connection String: {ConnectionString}", _connectionString);
        _logger.LogInformation("Managed Identity Client ID: {ManagedIdentityClientId}", _managedIdentityClientId);
        _logger.LogInformation("Use Managed Identity: {UseManagedIdentity}", _useManagedIdentity);
    }

    public async Task<SqlConnection> GetOpenConnectionAsync()
    {
        var connection = new SqlConnection(_connectionString);

        if (_useManagedIdentity)
        {
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = _managedIdentityClientId
            });

            var token = await credential.GetTokenAsync(new TokenRequestContext(new[] { "https://database.windows.net/.default" }));
            connection.AccessToken = token.Token;
        }

        await connection.OpenAsync();
        return connection;
    }
}
