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
            _.AddSingleton<ICallFunction, CallFunction>();
        });
    }

}
