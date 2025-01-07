namespace Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class AzureQueueExtension
{
    public static IHostBuilder AddAzureQueues(this IHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureServices(_ =>
        {
            _.AddTransient<IAzureQueueStorageHelper,AzureQueueStorageHelper>();
        });
    }

}
