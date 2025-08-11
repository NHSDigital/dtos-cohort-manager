namespace Model;

// TODO: rename class
// TODO: remove last Participant field and refactor where it is used to use ParticipantCsvRecord instead
// TODO: review whether this class can be removed
public class BasicParticipantCsvRecord
{
    /// <value>
    /// A string representing either the CaaS file name or the ServiceNow Case Number from where this participant originated.
    /// </value>
    public string FileName { get; set; }
    public BasicParticipantData BasicParticipantData { get; set; }
    public Participant Participant { get; set; }

    public BasicParticipantCsvRecord()
    {

    }

    public BasicParticipantCsvRecord(ServiceNowParticipant serviceNowParticipant, ParticipantManagement? participantManagement)
    {
        FileName = serviceNowParticipant.ServiceNowCaseNumber;
        BasicParticipantData = new BasicParticipantData
        {
            ScreeningId = serviceNowParticipant.ScreeningId.ToString(),
            NhsNumber = serviceNowParticipant.NhsNumber.ToString(),
            RecordType = participantManagement is null ? Actions.New : Actions.Amended,
        };
        Participant = new Participant
        {
            ReferralFlag = "1",
            Postcode = serviceNowParticipant.RequiredGpCode,
            ScreeningAcronym = "BSS" // TODO: Remove hardcoding when adding support for additional screening programs
        };
    }
}
