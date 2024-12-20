namespace NHS.Screening.ReceiveCaasFile;
using Model;

public interface IRecordsProcessedTracker
{
    bool RecordNotAlreadyProcessed(string RecordType, string NHSId);
}
