namespace Model;

using Model.Enums;

public class ParticipantCsvRecord
{
    public string FileName { get; set; }
    public Participant Participant { get; set; }
    public ReasonForAdding? ReasonForAdding { get; set; } = null;
}
