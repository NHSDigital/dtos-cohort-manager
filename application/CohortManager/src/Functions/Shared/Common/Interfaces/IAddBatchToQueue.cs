namespace Common;

using System.Collections.Concurrent;
using Model;

public interface IAddBatchToQueue
{
    Task ProcessBatch(ConcurrentQueue<BasicParticipantCsvRecord> batch, string queueName);
    Task AddMessage(BasicParticipantCsvRecord basicParticipantCsvRecord, string queueName);
}
