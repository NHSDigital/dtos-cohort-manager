namespace NHS.Screening.ReceiveCaasFile;

using Model;

public interface IProcessCaasFile
{
    Task ProcessRecord(ParticipantsParquetMap record, ScreeningLkp screeningService, string name);
}

