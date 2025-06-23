namespace Model;

// TODO: rename class
// TODO: remove last Participant field and refactor where it is used to use ParticipantCsvRecord instead
// TODO: review whether this class can be removed
public class BasicParticipantCsvRecord
{
    public string FileName { get; set; }
    public BasicParticipantData Participant { get; set; }
    public Participant participant { get; set; }
}
