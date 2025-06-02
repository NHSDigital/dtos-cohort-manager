namespace Model;

public class ParticipantCsvRecord
{
    public string FileName { get; set; }
    public Participant Participant { get; set; }

    public long MessageFromQueueId { get; set; }
}
