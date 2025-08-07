namespace Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class HttpClientExtension
{
    public static IHostBuilder AddHttpClient(this IHostBuilder hostBuilder, bool UseFakeHttpService = false)
    {
        if (UseFakeHttpService)
        {
            return hostBuilder.ConfigureServices(_ =>
            {
                _.AddHttpClient();
                _.AddTransient<IHttpClientFunction, PdsHttpClientMock>();
            });
        }

        return hostBuilder.ConfigureServices(_ =>
        {
            _.AddHttpClient();
            _.AddTransient<IHttpClientFunction, HttpClientFunction>();
        });
    }

    public static IHostBuilder AddNemsHttpClient(this IHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureServices(_ =>
        {
            _.AddTransient<INemsHttpClientProvider, NemsHttpClientProvider>();
            _.AddTransient<INemsHttpClientFunction, NemsHttpClientFunction>();
        });
    }
}
