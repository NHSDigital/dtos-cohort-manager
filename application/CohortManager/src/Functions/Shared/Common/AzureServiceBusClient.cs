namespace Common;

using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.WebUtilities;
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

    public async Task<bool> AddAsync<T>(T message, string queueName)
    {
        var sender = _serviceBusClient.CreateSender(queueName);
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
            _logger.LogError(ex, "There was an error sending message to service bus queue {queueName} {errorMessage}", queueName, ex.Message);
            return false;
        }
        finally
        {
            await sender.DisposeAsync();
        }
    }
}