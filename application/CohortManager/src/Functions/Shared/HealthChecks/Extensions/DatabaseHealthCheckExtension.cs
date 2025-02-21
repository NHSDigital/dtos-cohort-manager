namespace HealthChecks.Extension;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public static class DatabaseHealthCheckExtension
{
    public static IServiceCollection AddDatabaseHealthCheck<TDbContext>(
        this IServiceCollection services)
        where TDbContext : DbContext
    {
        // Register the generic health check
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck<TDbContext>>(
                name: typeof(TDbContext).Name + "HealthCheck",
                tags: new[] { "database", "sqlserver" });

        return services;
    }
}
