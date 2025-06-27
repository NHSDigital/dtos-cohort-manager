namespace Common;

using Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class ExceptionHandlerServiceExtension
{

    public static IHostBuilder AddExceptionHandler(this IHostBuilder hostBuilder)
    {
        hostBuilder.AddConfiguration<HttpValidationConfig>();
        return hostBuilder.ConfigureServices(_ =>
        {
            _.AddSingleton<IExceptionHandler, ExceptionHandler>();
            _.AddTransient<IHttpClientFunction, HttpClientFunction>();
            _.AddTransient<IExceptionSender, SendExceptionToHttp>();
        });
    }

    public static IHostBuilder AddExceptionHandlerWithServiceBus(this IHostBuilder hostBuilder)
    {
        hostBuilder.AddConfiguration<ServiceBusValidationConfig>(out ServiceBusValidationConfig config);
        return hostBuilder.ConfigureServices(_ =>
        {
            _.AddSingleton<IExceptionHandler, ExceptionHandler>();
            _.AddTransient<IExceptionSender, SendExceptionToServiceBus>();
            _.AddTransient<IQueueClient>(_ => new AzureServiceBusClient(config.ServiceBusConnectionString));
        });
    }




}
