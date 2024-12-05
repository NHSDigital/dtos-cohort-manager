namespace Model;

using System.Collections.Concurrent;

public class Batch
{
    public Batch()
    {
        AddRecords = new ConcurrentQueue<BasicParticipantCsvRecord>();
        UpdateRecords = new ConcurrentQueue<BasicParticipantCsvRecord>();
        DeleteRecords = new ConcurrentQueue<BasicParticipantCsvRecord>();
        DemographicData = new ConcurrentQueue<Participant>();
    }

    public ConcurrentQueue<BasicParticipantCsvRecord> AddRecords { get; set; }
    public ConcurrentQueue<BasicParticipantCsvRecord> UpdateRecords { get; set; }
    public ConcurrentQueue<BasicParticipantCsvRecord> DeleteRecords { get; set; }
    public ConcurrentQueue<Participant> DemographicData { get; set; }

}