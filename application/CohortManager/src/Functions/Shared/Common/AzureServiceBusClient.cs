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
    /// will send a message to a queue/ topic
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="message"></param>
    /// <param name="queueName"></param>
    /// <param name="topicName"></param>
    /// <returns></returns>
    public async Task<bool> AddAsync<T>(T message, string queueTopicName)
    {
        var sender = _serviceBusClient.CreateSender(queueTopicName);

        try
        {
            string jsonMessage = JsonSerializer.Serialize(message);
            ServiceBusMessage serviceBusMessage = new(jsonMessage);

            _logger.LogInformation("sending message to service bus queue or topic");

            await sender.SendMessageAsync(serviceBusMessage);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "There was an error sending message to service bus queue {QueueName} {ErrorMessage}", queueTopicName, ex.Message);
            return false;
        }
        finally
        {
            await sender.DisposeAsync();
        }
    }
}