namespace AddBatchFromQueue;

using System;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

public class AddBatchFromQueue
{
    private readonly ILogger _logger;

    public AddBatchFromQueue(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<AddBatchFromQueue>();
    }

    [Function("AddBatchFromQueue")]
    public async Task Run([TimerTrigger("0 */1 * * * *", RunOnStartup = true)] TimerInfo myTimer)
    {
        _logger.LogInformation($"DrainServiceBusQueue started at: {DateTime.Now}");

        var client = new ServiceBusClient(Environment.GetEnvironmentVariable("QueueConnectionString"));
        var receiver = client.CreateReceiver(Environment.GetEnvironmentVariable("QueueName"), new ServiceBusReceiverOptions
        {
            ReceiveMode = ServiceBusReceiveMode.PeekLock
        });

        var batchSize = 100;
        int totalMessages = 0;


        IReadOnlyList<ServiceBusReceivedMessage> messages = await receiver.ReceiveMessagesAsync(batchSize, TimeSpan.FromSeconds(5));

        if (messages.Count == 0)
        {
            _logger.LogInformation("nothing to process");
            return;
        }

        foreach (var message in messages)
        {
            try
            {
                string body = message.Body.ToString();
                _logger.LogInformation($"Processing message: {body}");


                await receiver.CompleteMessageAsync(message);
                totalMessages++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process message");
                await receiver.AbandonMessageAsync(message);
            }
        }


        _logger.LogInformation($"Drained and processed {totalMessages} messages from the queue.");
        await receiver.CloseAsync();
    }
}

