namespace NHS.Screening.ReceiveCaasFile;

using Model;

public interface IProcessRecord
{
    Task UpdateParticipant(BasicParticipantCsvRecord basicParticipantCsvRecord, string filename);
    Task RemoveParticipant(BasicParticipantCsvRecord basicParticipantCsvRecord, string filename);
}

