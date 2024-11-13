using System.Text;
using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using Model;

public class AddBatchToQueue : IAddBatchToQueue
{
    private QueueClient _AddQueueClient;

    private readonly string storageConnectionString;

    public readonly ILogger<AddBatchToQueue> _logger;

    public AddBatchToQueue(ILogger<AddBatchToQueue> logger)
    {
        storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "";
        _AddQueueClient = new QueueClient(storageConnectionString, Environment.GetEnvironmentVariable("AddQueueName"));

        _AddQueueClient.CreateIfNotExists();
        _logger = logger;
    }



    public async Task ProcessBatch(Batch batch)
    {
        if (batch != null && batch.AddRecords.Any())
        {
            await AddMessagesAsync(batch);
        }
    }

    private async Task AddMessagesAsync(Batch currentBatch)
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

    private async Task AddMessage(BasicParticipantCsvRecord basicParticipantCsvRecord)
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