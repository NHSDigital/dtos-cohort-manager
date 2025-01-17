namespace NHS.Screening.ReceiveCaasFile;

using Model;

public interface IStateStore
{

    public List<ParticipantsParquetMap> GetListOfAllValues();
    Task<int?> GetLastProcessedRecordIndex(string fileName);
    Task ClearProcessingState(string fileName);

}