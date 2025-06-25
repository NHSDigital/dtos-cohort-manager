namespace Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class ExceptionHandlerServiceExtension
{
    public static IHostBuilder AddExceptionHandler(this IHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureServices(_ =>
        {
            _.AddSingleton<IExceptionHandler, ExceptionHandler>();
            _.AddTransient<IHttpClientFunction, HttpClientFunction>();
        });
    }

    public static IHostBuilder AddExceptionHandlerWithServiceBus(this IHostBuilder hostBuilder, string serviceBusConnectionString)
    {
        return hostBuilder.ConfigureServices(_ =>
      {
          _.AddSingleton<IExceptionHandler, ExceptionHandler>();
          _.AddTransient<IQueueClient>(_ => new AzureServiceBusClient(serviceBusConnectionString));
      });
    }




}
