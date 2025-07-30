namespace Common;

using Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Client;

public static class ExceptionHandlerServiceExtension
{

    public static IHostBuilder AddExceptionHandler(this IHostBuilder hostBuilder)
    {
        bool useServiceBus = false;
        if (!bool.TryParse(Environment.GetEnvironmentVariable("UseServiceBus"), out useServiceBus))
        {
            useServiceBus = false;
        }

        if (useServiceBus)
        {
            hostBuilder.AddConfiguration<ExceptionServiceBusConfig>(out ExceptionServiceBusConfig config);
            hostBuilder.AddKeyedAzureQueues(config.UseServiceBus, config.ServiceBusConnectionString, "Exception");
            return hostBuilder.ConfigureServices(_ =>
            {
                _.AddSingleton<IExceptionHandler, ExceptionHandler>();
                _.AddTransient<IExceptionSender, SendExceptionToServiceBus>();
            });
        }

        hostBuilder.AddConfiguration<HttpValidationConfig>();
        return hostBuilder.ConfigureServices(_ =>
        {
            _.AddSingleton<IExceptionHandler, ExceptionHandler>();
            _.AddTransient<IHttpClientFunction, HttpClientFunction>();
            _.AddTransient<IExceptionSender, SendExceptionToHttp>();
        });
    }
}
