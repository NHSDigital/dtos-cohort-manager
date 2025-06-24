namespace Common;

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Model;

public class AddBatchToQueue : IAddBatchToQueue
{
    public readonly ILogger<AddBatchToQueue> _logger;
    private readonly IAzureQueueStorageHelper _queueHelper;

    public AddBatchToQueue(
        ILogger<AddBatchToQueue> logger,
        IAzureQueueStorageHelper queueHelper)
    {
        _logger = logger;
        _queueHelper = queueHelper;
    }

    public async Task ProcessBatch(ConcurrentQueue<BasicParticipantCsvRecord> batch, string queueName)
    {
        if (batch != null && batch.Any())
        {
            await AddMessagesAsync(batch, queueName);
        }
    }

    private async Task AddMessagesAsync(ConcurrentQueue<BasicParticipantCsvRecord> currentBatch, string queueName)
    {
        var itemsToAdd = currentBatch;

        // List of tasks to handle messages
        List<Task> tasks =
        [
            Task.Factory.StartNew(async () =>
            {
                // Process messages while there are items in the queue
                while (itemsToAdd.TryDequeue(out var item))
                {
                    await AddMessage(item, queueName);
                }
            }),
        ];

        // Wait for all tasks to complete
        await Task.WhenAll(tasks.ToArray());

    }

    private async Task AddMessage(BasicParticipantCsvRecord basicParticipantCsvRecord, string queueName)
    {
        await _queueHelper.AddItemToQueueAsync(basicParticipantCsvRecord, queueName);
    }
}
