namespace HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;

public static class BasicHealthCheckExtension
{
    public static IServiceCollection AddBasicHealthCheck(this IServiceCollection services, string name)
    {
        // Register blob storage health checks
        services.AddHealthChecks()
            .AddCheck<BasicHealthCheck>(
                "HealthCheck for " + name,
                tags: new[] { "Basic", "Ping API end point." });
        
        return services;
    }
}
