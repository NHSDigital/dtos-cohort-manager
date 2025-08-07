namespace Model;

// TODO: rename class
// TODO: remove last Participant field and refactor where it is used to use ParticipantCsvRecord instead
// TODO: review whether this class can be removed
public class BasicParticipantCsvRecord
{
    /// <value>
    /// A bool representing whether this participant came from ServiceNow. Defaults to false.
    /// </value>
    public bool FromServiceNow { get; set; } = false;
    /// <value>
    /// A string representing either the CaaS file name or the ServiceNow Case Number from where this participant originated.
    /// </value>
    public string FileName { get; set; }
    public BasicParticipantData BasicParticipantData { get; set; }
    public Participant Participant { get; set; }
}
