namespace HealthChecks.Extensions;

using DataServices.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class DatabaseHealthCheckExtension
{
    public static IServiceCollection AddDatabaseHealthCheck(this IServiceCollection services)
    {
        // Register the database health check
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>(
                "Sql-Server HealthCheck",
                tags: new[] { "database", "sqlserver" });
        // Register DbContext for health check
        services.AddDbContext<DataServicesContext>(options =>
        {
            options.UseSqlServer(
                Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString"),
                sqlServerOptions =>
                {
                    sqlServerOptions.CommandTimeout(30); // Set command timeout to 30 seconds
                    sqlServerOptions.EnableRetryOnFailure(
                        maxRetryCount: 5, // Retry up to 5 times
                        maxRetryDelay: TimeSpan.FromSeconds(30), // Maximum delay between retries
                        errorNumbersToAdd: null); // Optional: Specify SQL error numbers to retry
                });
        });
        return services;
    }
}
