namespace Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class AuthenticationExtension
{
    public static IHostBuilder AddAuthentication(this IHostBuilder hostBuilder)
    {

        hostBuilder.AddConfiguration<AuthConfig>();
        hostBuilder.ConfigureFunctionsWorkerDefaults(workerOptions =>
        {
            workerOptions.UseMiddleware<Cis2AuthMiddleware>();
        });
        hostBuilder.ConfigureServices((context, services) =>
        {
            services.AddSingleton<IAuthenticationService, JwtAuthentication>();
            services.AddSingleton<ICis2UserService,Cis2UserService>();
        });
        return hostBuilder;
     }
}
