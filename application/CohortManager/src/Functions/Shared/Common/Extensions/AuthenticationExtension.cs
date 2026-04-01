namespace Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class AuthenticationExtension
{
    public static IHostBuilder AddAuthentication(this IHostBuilder hostBuilder)
    {

        hostBuilder.AddConfiguration<AuthConfig>();
        hostBuilder.ConfigureServices((context, services) =>
        {
            services.AddSingleton<IAuthenticationService, JWTAuthentication>();
        });
        return hostBuilder;
     }
}
