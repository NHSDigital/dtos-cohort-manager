namespace NHS.Screening.ReceiveCaasFile;

public interface IRecordsProcessedTracker
{
    bool RecordNotAlreadyProcessed(string RecordType, string NHSId);
}
