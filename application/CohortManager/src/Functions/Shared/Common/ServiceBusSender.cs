namespace Common;

using Azure.Messaging.ServiceBus;

public class ServiceBusSender : IQueueSender
{

    private readonly ServiceBusClient _serviceBusClient;

    public ServiceBusSender(string connectionString)
    {
        _serviceBusClient = new ServiceBusClient(connectionString);
    }

    public async Task<bool> AddMessageToQueueAsync<ServiceBusMessage>(ServiceBusMessage message, string queueName)
    {
        var sender = _serviceBusClient.CreateSender(queueName);
        ServiceBusMessage

    }
}