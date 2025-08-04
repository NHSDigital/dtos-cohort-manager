namespace Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class HttpClientExtension
{
    public static IHostBuilder AddHttpClient(this IHostBuilder hostBuilder, bool useFakeHttpService = false)
    {
        if (useFakeHttpService)
        {
            return hostBuilder.ConfigureServices(_ =>
            {
                _.AddHttpClient();
                _.AddTransient<IHttpClientFunction, HttpClientFunctionMock>();
            });
        }

        return hostBuilder.ConfigureServices(_ =>
        {
            _.AddHttpClient();
            _.AddTransient<IHttpClientFunction, HttpClientFunction>();
        });
    }

    public static IHostBuilder AddNemsHttpClient(this IHostBuilder hostBuilder, bool useStubbedEndpoint = false)
    {
        return hostBuilder.ConfigureServices(_ =>
        {
            _.AddTransient<INemsHttpClientProvider, NemsHttpClientProvider>();
            if (useStubbedEndpoint)
            {
                _.AddTransient<INemsHttpClientFunction, StubbedNemsHttpClientFunction>();
            }
            else
            {
                _.AddTransient<INemsHttpClientFunction, NemsHttpClientFunction>();
            }

        });
    }
}
