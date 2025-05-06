
namespace Common;

using System.Text;
using System.Text.Json;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Logging;
using Model;

public class AzureQueueStorageHelper : IAzureQueueStorageHelper
{
    public readonly ILogger<AzureQueueStorageHelper> _logger;
    public readonly IQueueClientFactory _queueClientFactory;

    public AzureQueueStorageHelper(ILogger<AzureQueueStorageHelper> logger, IQueueClientFactory queueClientFactory)
    {
        _logger = logger;
        _queueClientFactory = queueClientFactory;
    }

    public async Task<bool> AddItemToQueueAsync<T>(T participantCsvRecord, string queueName)
    {
        var _queueClient = _queueClientFactory.CreateClient(queueName);
        await _queueClient.CreateIfNotExistsAsync();
        var json = JsonSerializer.Serialize(participantCsvRecord);
        var bytes = Encoding.UTF8.GetBytes(json);
        try
        {
            await _queueClient.SendMessageAsync(Convert.ToBase64String(bytes));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "There was an error while putting item on queue for queue: {queueName}", queueName);
            return false;
        }
    }

    public async Task<int> GetNumberOfItemsInQueue(string queueName)
    {
        var _queueClient = _queueClientFactory.CreateClient(queueName);
        await _queueClient.CreateIfNotExistsAsync();

        QueueProperties properties = await _queueClient.GetPropertiesAsync();
        return properties.ApproximateMessagesCount;
    }
}
