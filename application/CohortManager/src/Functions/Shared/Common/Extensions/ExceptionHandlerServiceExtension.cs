using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Common;

public static class ExceptionHandlerServiceExtension
{
    public static IHostBuilder addExceptionHandler(this IHostBuilder hostBuilder){
        return hostBuilder.ConfigureServices(_ => {
            _.AddSingleton<IHandleException,HandleException>();
            _.AddSingleton<ICallFunction, CallFunction>();
        });
    }

}
