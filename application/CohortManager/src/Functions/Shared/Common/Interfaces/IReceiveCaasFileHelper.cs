namespace Common.Interfaces;
using Model;

public interface IReceiveCaasFileHelper
{
    Task<bool> InitialChecks(Stream blobStream, string name);
    Task<Participant?> MapParticipant(ParticipantsParquetMap rec, Participant participant, string name, int rowNumber);
    Task SerializeParquetFile(List<Cohort> chunks, Cohort cohort, string filename, int rowNumber);
    Task InsertValidationErrorIntoDatabase(string fileName, string errorRecord);
    string GetUrlFromEnvironment(string key);
}
