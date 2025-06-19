namespace Common;

using System.Text.Json;
using Azure.Messaging.ServiceBus;

public class AzureServiceBusClient : IQueueClient
{

    private readonly ServiceBusClient _serviceBusClient;

    public AzureServiceBusClient(string connectionString)
    {
        _serviceBusClient = new ServiceBusClient(connectionString);
    }

    public async Task<bool> AddAsync<T>(T message, string queueName)
    {
        var sender = _serviceBusClient.CreateSender(queueName);
        try
        {
            string jsonMessage = JsonSerializer.Serialize(message);
            ServiceBusMessage serviceBusMessage = new(jsonMessage);

            await sender.SendMessageAsync(serviceBusMessage);
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            await sender.DisposeAsync();
        }
    }
    
    public async Task<bool> AddMessageBatchToQueueAsync<T>(IEnumerable<ServiceBusMessage> messages, string queueName)
    {
        var sender = _serviceBusClient.CreateSender(queueName);
        await sender.SendMessagesAsync(messages);
        return true;
    }
}