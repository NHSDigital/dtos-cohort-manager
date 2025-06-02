namespace Common.Interfaces;
using Model;
using NHS.Screening.ReceiveCaasFile;

public interface IReceiveCaasFileHelper
{
    Participant MapParticipant(ParticipantsParquetMap rec, string screeningId, string ScreeningName, string name);
    string GetUrlFromEnvironment(string key);
}
