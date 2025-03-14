
namespace Common;

using System.Text;
using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using Model;

public class AzureQueueStorageHelper : IAzureQueueStorageHelper
{

    private readonly string storageConnectionString;

    public readonly ILogger<AzureQueueStorageHelper> _logger;

    public AzureQueueStorageHelper(ILogger<AzureQueueStorageHelper> logger)
    {
        storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        _logger = logger;
    }

    public async Task<bool> AddItemToQueueAsync<T>(T participantCsvRecord, string queueName)
    {
        var _queueClient = new QueueClient(storageConnectionString, queueName);
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
}
