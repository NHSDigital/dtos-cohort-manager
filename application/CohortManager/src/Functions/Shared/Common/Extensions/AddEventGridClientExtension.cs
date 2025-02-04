namespace Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Azure.Messaging.EventGrid;
using Azure.Identity;

public static class AddEventGridClientExtension
{
    public static IHostBuilder AddEventGridClient(this IHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureServices((context, services) =>
        {
            services.AddSingleton(sp =>
            {
                if (HostEnvironmentEnvExtensions.IsDevelopment(context.HostingEnvironment))
                {
                    var credentials = new Azure.AzureKeyCredential(Environment.GetEnvironmentVariable("topicKey"));
                    return new EventGridPublisherClient(new Uri(Environment.GetEnvironmentVariable("topicEndpoint")), credentials);
                }

                return new EventGridPublisherClient(new Uri(Environment.GetEnvironmentVariable("topicEndpoint")), new DefaultAzureCredential());
            });
        });
    }
}
