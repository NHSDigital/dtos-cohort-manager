namespace Common;

using Azure.Messaging.ServiceBus;

public class ServiceBusSender : IQueueSender
{

    private readonly ServiceBusClient _serviceBusClient;

    public ServiceBusSender(string connectionString)
    {
        _serviceBusClient = new ServiceBusClient(connectionString);
    }

    public async Task<bool> AddMessageToQueueAsync<T>(ServiceBusMessage message, string queueName)
    {
        var sender = _serviceBusClient.CreateSender(queueName);

        await sender.SendMessageAsync(message);
        sender.
    }
    
    public async Task<bool> AddMessageBatchToQueueAsync<T>(IEnumerable<ServiceBusMessage> messages, string queueName)
    {
        var sender = _serviceBusClient.CreateSender(queueName);
        await sender.SendMessagesAsync(messages);
        return true;
    }
}