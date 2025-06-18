namespace NHS.Screening.ReceiveCaasFile;

using Model;

public interface IProcessCaasFile
{
    Task ProcessRecords(List<ParticipantsParquetMap> values, ParallelOptions options, ScreeningLkp screeningService, string name);
}

