namespace Common.Interfaces;

using Model;

public interface IProcessCaasFile
{
    Task ProcessRecords(List<ParticipantsParquetMap> values, ParallelOptions options, ScreeningService screeningService, string name);
}

