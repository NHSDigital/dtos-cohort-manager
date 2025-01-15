namespace NHS.Screening.ReceiveCaasFile;

using Model;

public interface IProcessRecordsManager
{
           Task ProcessRecordsWithRetry(
           List<ParticipantsParquetMap> values,
           ParallelOptions options,
           ScreeningService screeningService,
           string name);
}
