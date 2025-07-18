namespace Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class StateStoreExtension
{
    public static IHostBuilder AddStateStorage(this IHostBuilder hostBuilder)
    {
        hostBuilder.AddConfiguration<BlobStateStoreConfig>();

        return hostBuilder.ConfigureServices(_ =>
        {
            _.AddTransient<IBlobStorageHelper, BlobStorageHelper>();
            _.AddScoped<IStateStore, BlobStateStore>();
        });
    }
}
