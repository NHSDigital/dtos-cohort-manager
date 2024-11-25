namespace NHS.Screening.ReceiveCaasFile;

using System.Collections.Concurrent;
using Model;

public interface IAddBatchToQueue
{
    Task ProcessBatch(ConcurrentQueue<BasicParticipantCsvRecord> batch);
}
