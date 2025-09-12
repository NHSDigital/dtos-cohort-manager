namespace Common;

using System.Collections.Concurrent;
using Model;

public interface IAddBatchToQueue
{
    Task ProcessBatch(ConcurrentQueue<IParticipant> batch, string queueName);
    Task AddMessage(IParticipant participant, string queueName);
}
