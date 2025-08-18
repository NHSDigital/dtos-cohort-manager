namespace Common;

using System.Collections.Concurrent;
using Model;

public interface IAddBatchToQueue
{
    Task ProcessBatch(ConcurrentQueue<Participant> batch, string queueName);
    Task AddMessage(Participant participant, string queueName);
}
