using System.Text.Json;
using Azure.Messaging.ServiceBus;

public static class ServiceBusTestHelper
{
    public static ServiceBusReceivedMessage CreateServiceBusMessage<T>(T body)
    {
        var messageBody = JsonSerializer.Serialize(body);
        return ServiceBusModelFactory.ServiceBusReceivedMessage(new BinaryData(messageBody));
    }
}
