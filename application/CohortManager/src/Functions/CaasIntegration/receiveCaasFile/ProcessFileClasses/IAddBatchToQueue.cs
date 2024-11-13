namespace NHS.Screening.ReceiveCaasFile;
using Model;

public interface IAddBatchToQueue
{
    Task ProcessBatch(Batch batch);
}