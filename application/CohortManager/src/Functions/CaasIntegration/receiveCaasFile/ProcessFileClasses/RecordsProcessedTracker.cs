namespace receiveCaasFile;
public class RecordsProcessedTracker
{
    private readonly Guid _instanceId;
    private readonly HashSet<ParticipantRecord> _processedRecords;

    private static object lockObj = new object();

    public RecordsProcessedTracker()
    {
        _instanceId = Guid.NewGuid();
        Console.WriteLine(_instanceId);
        _processedRecords = new HashSet<ParticipantRecord>();
    }

    public string getInstanceId()
    {
        return _instanceId.ToString();
    }

    public bool RecordNotAlreadyProcessed(string RecordType, string NHSId)
    {
        var rec = new ParticipantRecord{ RecordType = RecordType, NHSId = NHSId};
        //avoiding race conditions on access to the process records hashset
        lock(lockObj)
        {
            if(_processedRecords.Contains(rec))
            {
                return false;
            }
            _processedRecords.Add(rec);
        }
        return true;
    }

    private struct ParticipantRecord
    {
        public string RecordType;
        public string NHSId;
        public override int GetHashCode()
        {
            return HashCode.Combine(RecordType, NHSId);
        }
    }
}

