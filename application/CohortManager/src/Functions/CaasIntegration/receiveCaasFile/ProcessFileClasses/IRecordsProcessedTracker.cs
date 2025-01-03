namespace NHS.Screening.ReceiveCaasFile;

public interface IRecordsProcessedTracker
{
    bool RecordAlreadyProcessed(string RecordType, string NHSId);
}
