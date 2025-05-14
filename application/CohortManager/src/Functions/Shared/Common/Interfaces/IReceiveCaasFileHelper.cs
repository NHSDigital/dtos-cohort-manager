namespace Common.Interfaces;
using Model;
using NHS.Screening.ReceiveCaasFile;

public interface IReceiveCaasFileHelper
{
    Task<Participant?> MapParticipant(ParticipantsParquetMap rec, string screeningId, string ScreeningName, string name);
    Task InsertValidationErrorIntoDatabase(string fileName, string errorRecord);
    string GetUrlFromEnvironment(string key);
    Task<bool> CheckFileName(string name, FileNameParser fileNameParser, string errorMessage);
}
