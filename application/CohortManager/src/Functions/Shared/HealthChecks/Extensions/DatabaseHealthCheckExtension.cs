namespace HealthChecks.Extensions;

using DataServices.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class DatabaseHealthCheckExtension
{
    public static IServiceCollection AddDatabaseHealthCheck(this IServiceCollection services, string name)
    {
        // Register the database health check
        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("HealthCheck for "+name,tags: new[] { "database", "sqlserver" });
            // Register DbContext for health check
        services.AddDbContext<DataServicesContext>(options =>
        {
            options.UseSqlServer(
                Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString"));
        });
        return services;
    }
}
