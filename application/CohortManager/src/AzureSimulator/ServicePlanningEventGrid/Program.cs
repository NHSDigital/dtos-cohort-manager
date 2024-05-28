using System;
using Azure.Messaging.EventGrid;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        string connectionString = "";
        string topicEndpoint = "";

        var options = new EventGridPublisherClientOptions()
        {
            RetryOptions = new ExponentialRetry(TimeSpan.FromSeconds(1), maxAttempts: 5),
            LoggingOptions = new ConsoleLoggingProvider()
        };

        var publisher = new EventGridPublisherClient(topicEndpoint, new AzureKeyCredential(connectionString), options);
        var eventGridEvent = new EventGridEvent("CustomTopic", "CustomEventType", "Payload");
        await publisher.SendEventsAsync(new[] { eventGridEvent });

        Console.WriteLine("Published event successfully...");
    }
}
