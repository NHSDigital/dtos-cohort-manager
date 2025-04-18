namespace NHS.Screening.ReceiveCaasFile;

using System.Collections.Concurrent;
using Model;

public class Batch
{
    public Batch()
    {
        AddRecords = new ConcurrentQueue<BasicParticipantCsvRecord>();
        UpdateRecords = new ConcurrentQueue<BasicParticipantCsvRecord>();
        DeleteRecords = new ConcurrentQueue<BasicParticipantCsvRecord>();
        DemographicData = new ConcurrentQueue<ParticipantDemographic>();
    }

    public ConcurrentQueue<BasicParticipantCsvRecord> AddRecords { get; set; }
    public ConcurrentQueue<BasicParticipantCsvRecord> UpdateRecords { get; set; }
    public ConcurrentQueue<BasicParticipantCsvRecord> DeleteRecords { get; set; }
    public ConcurrentQueue<ParticipantDemographic> DemographicData { get; set; }

}