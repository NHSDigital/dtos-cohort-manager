namespace NHS.Screening.ReceiveCaasFile;

using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Azure.Storage.Queues;
using Common;
using Microsoft.Extensions.Logging;
using Model;

public class AddBatchToQueue : IAddBatchToQueue
{

    public readonly ILogger<AddBatchToQueue> _logger;


    private readonly IAzureQueueStorageHelper _queueHelper;

    public AddBatchToQueue(ILogger<AddBatchToQueue> logger, IAzureQueueStorageHelper queueHelper)
    {
        _logger = logger;
        _queueHelper = queueHelper;
    }

    public async Task ProcessBatch(ConcurrentQueue<BasicParticipantCsvRecord> batch)
    {
        if (batch != null && batch.Any())
        {
            await AddMessagesAsync(batch);
        }
    }

    private async Task AddMessagesAsync(ConcurrentQueue<BasicParticipantCsvRecord> currentBatch)
    {
        var itemsToAdd = currentBatch;

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
        await _queueHelper.AddItemToQueueAsync<BasicParticipantCsvRecord>(basicParticipantCsvRecord,Environment.GetEnvironmentVariable("AddQueueName"));
    }

    private static string ParseMessage(BasicParticipantCsvRecord ParticipantCsvRecord)
    {
        var json = JsonSerializer.Serialize(ParticipantCsvRecord);
        var bytes = Encoding.UTF8.GetBytes(json);

        return Convert.ToBase64String(bytes);
    }
}
