namespace NHS.Screening.ReceiveCaasFile;

using Model;

public interface ICopyFailedBatchToBlob
{
    Task<bool> writeBatchToBlob(string jsonFromBatch, InvalidOperationException invalidOperationException, List<ParticipantsParquetMap> parquetValuesForRetry, string fileName = "");
}
