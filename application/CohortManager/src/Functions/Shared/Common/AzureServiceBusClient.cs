namespace Common;

using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

public class AzureServiceBusClient : IQueueClient
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ILogger<AzureServiceBusClient> _logger;

    public AzureServiceBusClient(string connectionString)
    {
        _serviceBusClient = new ServiceBusClient(connectionString);
        var factory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });
        _logger = factory.CreateLogger<AzureServiceBusClient>();
    }


    /// <summary>
    /// sends a message to a topic of the given name or name or sends a message to service buss queue when the topic name is not provided 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="message"></param>
    /// <param name="queueName"></param>
    /// <param name="topicName"></param>
    /// <returns></returns>
    public async Task<bool> AddAsync<T>(T message, string queueName, string? topicName = null)
    {
        ServiceBusSender sender = null!;
        if (!string.IsNullOrEmpty(queueName))
        {
            sender = _serviceBusClient.CreateSender(queueName);
        }
        if (!string.IsNullOrEmpty(topicName))
        {
            sender = _serviceBusClient.CreateSender(topicName);
        }

        try
        {
            string jsonMessage = JsonSerializer.Serialize(message);
            ServiceBusMessage serviceBusMessage = new(jsonMessage);

            _logger.LogInformation("sending message to service bus queue");

            await sender.SendMessageAsync(serviceBusMessage);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "There was an error sending message to service bus queue {QueueName} {ErrorMessage}", queueName, ex.Message);
            return false;
        }
        finally
        {
            await sender.DisposeAsync();
        }
    }
}