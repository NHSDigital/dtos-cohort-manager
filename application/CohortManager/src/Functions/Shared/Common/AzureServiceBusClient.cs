namespace Common;

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

public class AzureServiceBusClient : IQueueClient
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ILogger<AzureServiceBusClient> _logger;
    private readonly ConcurrentDictionary<string, ServiceBusSender> _senders = new();

    public AzureServiceBusClient(ILogger<AzureServiceBusClient> logger, ServiceBusClient serviceBusClient)
    {
        _logger = logger;
        _serviceBusClient = serviceBusClient;
    }


    /// <summary>
    /// will send a message to a queue/ topic
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="message"></param>
    /// <param name="queueName"></param>
    /// <param name="topicName"></param>
    /// <returns></returns>
    public async Task<bool> AddAsync<T>(T message, string queueName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

        var sender = _senders.GetOrAdd(queueName, _serviceBusClient.CreateSender);

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
            _logger.LogError(ex, "There was an error sending message to service bus queue {queueName} {errorMessage}", queueName, ex.Message);
            return false;
        }
    }
}
