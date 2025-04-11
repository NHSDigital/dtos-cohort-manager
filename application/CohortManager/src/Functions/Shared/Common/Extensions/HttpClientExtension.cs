namespace Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class HttpClientExtension
{
    public static IHostBuilder AddHttpClient(this IHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureServices(_ =>
        {
            _.AddHttpClient();
            _.AddSingleton<IHttpClientFunction, HttpClientFunction>();
        });
    }

}
