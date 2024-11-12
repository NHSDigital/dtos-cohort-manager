
namespace Common;

using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using Model;

public class AzureQueueStorageHelper : IAzureQueueStorageHelper
{
    private QueueClient _queueClient;

    private QueueClient _AddQueueClient;
    private readonly string storageConnectionString;

    public readonly ILogger<AzureQueueStorageHelper> _logger;

    public AzureQueueStorageHelper(ILogger<AzureQueueStorageHelper> logger)
    {
        storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        _logger = logger;
    }

    public async Task<bool> AddItemToQueueAsync<T>(T participantCsvRecord, string queueName)
    {
        _queueClient = new QueueClient(storageConnectionString, queueName);
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

    public async Task<QueueClient> CreateAddQueue()
    {
        _AddQueueClient = new QueueClient(storageConnectionString, Environment.GetEnvironmentVariable("AddQueueName"));
        await _AddQueueClient.CreateIfNotExistsAsync();

        return _AddQueueClient;
    }

    public async Task ProcessBatch(Batch batch)
    {
        //_logger.LogInformation("ProcessBatch Items {count}", batch.AddRecords.Count);
        if (batch != null && batch.AddRecords.Any())
        {
            await AddMessages(batch);
        }
    }

    private async Task AddMessages(Batch currentBatch)
    {
        var itemsToAdd = currentBatch.AddRecords;

        // List of tasks to handle messages
        List<Task> tasks = new List<Task>();
        tasks.Add(Task.Factory.StartNew(() =>
        {
            // Process messages while there are items in the queue
            while (itemsToAdd.TryDequeue(out var item))
            {
                AddMessage(item);

            }
        }));

        // Wait for all tasks to complete
        await Task.WhenAll(tasks.ToArray());

    }

    private async void AddMessage(BasicParticipantCsvRecord basicParticipantCsvRecord)
    {
        await _AddQueueClient.SendMessageAsync(ParseMessage(basicParticipantCsvRecord));
    }

    private string ParseMessage(BasicParticipantCsvRecord ParticipantCsvRecord)
    {
        var json = JsonSerializer.Serialize(ParticipantCsvRecord);
        var bytes = Encoding.UTF8.GetBytes(json);

        return Convert.ToBase64String(bytes);
    }
}
