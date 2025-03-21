using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace NHS.CohortManager.SmokeTests.Helpers;

public class SqlConnectionWithAuthentication
{
    private readonly string _connectionString;
    private readonly string? _managedIdentityClientId;
    private readonly bool _useManagedIdentity;

    public SqlConnectionWithAuthentication(string connectionString, string? managedIdentityClientId, bool isCloudEnvironment)
    {
        _connectionString = connectionString;

        // Even if isCloudEnvironment is set to true if ManagedIdentityClientId is null or empty _useManagedIdentity will be false
        _useManagedIdentity = isCloudEnvironment && !string.IsNullOrEmpty(managedIdentityClientId);
        _managedIdentityClientId = _useManagedIdentity ? managedIdentityClientId : "";
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
