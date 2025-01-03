namespace NHS.Screening.ReceiveCaasFile;

public class RecordsProcessedTracker : IRecordsProcessedTracker
{
    private readonly HashSet<ParticipantRecord> _processedRecords;
    private readonly static object lockObj = new object();

    public RecordsProcessedTracker()
    {
        _processedRecords = new HashSet<ParticipantRecord>();
    }

    public bool RecordAlreadyProcessed(string RecordType, string NHSId)
    {
        var rec = new ParticipantRecord { RecordType = RecordType, NHSId = NHSId };
        //avoiding race conditions on access to the process records hashset
        lock (lockObj)
        {
            if (_processedRecords.Contains(rec))
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
        public override bool Equals(object? obj)
        {
            return obj is ParticipantRecord other && Equals(other);
        }

        public bool Equals(ParticipantRecord other)
        {
            return RecordType == other.RecordType && NHSId == other.NHSId;
        }

    }
}

