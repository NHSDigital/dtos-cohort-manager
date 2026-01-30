using Microsoft.Extensions.Hosting;

namespace ComponentTests;

public static class HostBuilder
{

    public static IHostBuilder CreateHostBuilder() =>
        Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>{});
}
