using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

public class QueueDrainerFunction
{
    private readonly string connectionString = Environment.GetEnvironmentVariable("ServiceBusConnection");
    private readonly string queueName = "your-queue-name";

    [FunctionName("DrainServiceBusQueue")]
    public async Task RunAsync(
        [TimerTrigger("*/1 * * * *")] TimerInfo myTimer, // Every 5 minutes
        ILogger log)
    {
        log.LogInformation($"DrainServiceBusQueue started at: {DateTime.Now}");

        var client = new ServiceBusClient(connectionString);
        var receiver = client.CreateReceiver(queueName, new ServiceBusReceiverOptions
        {
            ReceiveMode = ServiceBusReceiveMode.PeekLock
        });

        var batchSize = 100;
        int totalMessages = 0;

        while (true)
        {
            IReadOnlyList<ServiceBusReceivedMessage> messages = await receiver.ReceiveMessagesAsync(batchSize, TimeSpan.FromSeconds(2));

            if (messages.Count == 0)
            {
                break;
            }

            foreach (var message in messages)
            {
                try
                {
                    string body = message.Body.ToString();
                    log.LogInformation($"Processing message: {body}");


                    await receiver.CompleteMessageAsync(message);
                    totalMessages++;
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Failed to process message");
                    await receiver.AbandonMessageAsync(message);
                }
            }
        }

        log.LogInformation($"Drained and processed {totalMessages} messages from the queue.");
        await receiver.CloseAsync();
    }
}
