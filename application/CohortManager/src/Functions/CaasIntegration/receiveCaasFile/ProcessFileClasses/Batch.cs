namespace NHS.Screening.ReceiveCaasFile;

using System.Collections.Concurrent;
using Model;

public class Batch
{
    public Batch()
    {
        AddRecords = new ConcurrentQueue<IParticipant>();
        UpdateRecords = new ConcurrentQueue<IParticipant>();
        DeleteRecords = new ConcurrentQueue<IParticipant>();
        DemographicData = new ConcurrentQueue<ParticipantDemographic>();
    }

    public ConcurrentQueue<IParticipant> AddRecords { get; set; }
    public ConcurrentQueue<IParticipant> UpdateRecords { get; set; }
    public ConcurrentQueue<IParticipant> DeleteRecords { get; set; }
    public ConcurrentQueue<ParticipantDemographic> DemographicData { get; set; }

}