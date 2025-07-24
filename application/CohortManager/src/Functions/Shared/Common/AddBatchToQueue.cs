namespace Common;

using System.Collections.Concurrent;
using Model;

public class AddBatchToQueue : IAddBatchToQueue
{

    private readonly IQueueClient _queueClient;

    public AddBatchToQueue(IQueueClient queueClient)
    {
        _queueClient = queueClient;
    }

    public async Task ProcessBatch(ConcurrentQueue<BasicParticipantCsvRecord> batch, string queueName)
    {
        if (batch != null && !batch.IsEmpty)
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

    public async Task AddMessage(BasicParticipantCsvRecord basicParticipantCsvRecord, string queueName)
    {
        await _queueClient.AddAsync<BasicParticipantCsvRecord>(basicParticipantCsvRecord, queueName);
    }
}
