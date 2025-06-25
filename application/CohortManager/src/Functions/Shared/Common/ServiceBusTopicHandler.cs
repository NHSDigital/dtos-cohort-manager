namespace Common;


using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Common.Interfaces;

public class ServiceTopicBusHandler : IServiceBusTopicHandler
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ILogger<AzureServiceBusClient> _logger;

    public ServiceTopicBusHandler()
    {
        _serviceBusClient = new ServiceBusClient(Environment.GetEnvironmentVariable("ServiceBusConnectionString"));
        var factory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });
        _logger = factory.CreateLogger<AzureServiceBusClient>();
    }

    public async Task<bool> SendMessageToTopic(string TopicName, string MessageBody)
    {
        var _serviceBusSender = _serviceBusClient.CreateSender(TopicName);
        try
        {
            // Use the producer client to send the batch of messages to the Service Bus topic
            await _serviceBusSender.SendMessageAsync(new ServiceBusMessage(MessageBody));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "there has been a problem adding a message to thew service buss topic {topicName}, {errorMessage}", TopicName, ex.Message);
            return false;
        }
        finally
        {
            await _serviceBusSender.DisposeAsync();
        }
    }
}