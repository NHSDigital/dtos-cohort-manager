
namespace Common;

using System.Text;
using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using Model;

public class AzureStorageQueueClient : IQueueClient
{
    public readonly ILogger<AzureStorageQueueClient> _logger;
    public readonly IQueueClientFactory _queueClientFactory;

    public AzureStorageQueueClient(ILogger<AzureStorageQueueClient> logger, IQueueClientFactory queueClientFactory)
    {
        _logger = logger;
        _queueClientFactory = queueClientFactory;
    }

    public async Task<bool> AddAsync<T>(T message, string queueName)
    {
        var _queueClient = _queueClientFactory.CreateClient(queueName);
        await _queueClient.CreateIfNotExistsAsync();
        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        try
        {
            await _queueClient.SendMessageAsync(Convert.ToBase64String(bytes));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "There was an error while putting item on queue for queue: {QueueName}", queueName);
            return false;
        }
    }
}
