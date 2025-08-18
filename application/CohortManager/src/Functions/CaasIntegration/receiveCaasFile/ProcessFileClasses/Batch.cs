namespace NHS.Screening.ReceiveCaasFile;

using System.Collections.Concurrent;
using Model;

public class Batch
{
    public Batch()
    {
        AddRecords = new ConcurrentQueue<Participant>();
        UpdateRecords = new ConcurrentQueue<Participant>();
        DeleteRecords = new ConcurrentQueue<Participant>();
        DemographicData = new ConcurrentQueue<ParticipantDemographic>();
    }

    public ConcurrentQueue<Participant> AddRecords { get; set; }
    public ConcurrentQueue<Participant> UpdateRecords { get; set; }
    public ConcurrentQueue<Participant> DeleteRecords { get; set; }
    public ConcurrentQueue<ParticipantDemographic> DemographicData { get; set; }

}