namespace NHS.Screening.ReceiveCaasFile;

using Model;

public interface IProcessRecordsManager
{
    Task ProcessRecordsWithRetry(
    List<ParticipantsParquetMap> participants,
    string name);
}
