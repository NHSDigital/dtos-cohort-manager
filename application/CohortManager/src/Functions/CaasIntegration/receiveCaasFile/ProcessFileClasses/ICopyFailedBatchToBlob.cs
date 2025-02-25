namespace NHS.Screening.ReceiveCaasFile;

public interface ICopyFailedBatchToBlob
{
    Task<bool> writeBatchToBlob(string jsonFromBatch, InvalidOperationException invalidOperationException);
}
