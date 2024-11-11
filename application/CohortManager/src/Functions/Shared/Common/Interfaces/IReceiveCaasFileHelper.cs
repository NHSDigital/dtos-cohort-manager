namespace Common.Interfaces;
using Model;

public interface IReceiveCaasFileHelper
{
    Task<bool> InitialChecks(Stream blobStream, string name);
    Task<Participant?> MapParticipant(ParticipantsParquetMap rec, string screeningId, string ScreeningName, string name);
    Task InsertValidationErrorIntoDatabase(string fileName, string errorRecord);
    string GetUrlFromEnvironment(string key);

    bool validateDateTimes(Participant participant);
}
