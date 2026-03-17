namespace Model;

using Enums;

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

    /// <summary>
    /// The reason for adding the participant. When populated, indicates this participant originated from ServiceNow.
    /// </summary>
    public ReasonForAdding? ReasonForAdding { get; set; }

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
            RecordType = Actions.New,
        };
        Participant = new Participant
        {
            ReferralFlag = "1",
            PrimaryCareProvider = serviceNowParticipant.RequiredGpCode,
            PrimaryCareProviderEffectiveFromDate = string.IsNullOrEmpty(serviceNowParticipant.RequiredGpCode) ? null : DateTime.UtcNow.ToString("yyyy-MM-dd"),
            ScreeningAcronym = "BSS" // TODO: Remove hardcoding when adding support for additional screening programs
        };
        ReasonForAdding = MapReasonForAdding(serviceNowParticipant.ReasonForAdding);
    }

    private ReasonForAdding? MapReasonForAdding(string reasonForAdding)
    {
        return reasonForAdding switch
        {
            Constants.ServiceNowReasonsForAdding.VeryHighRisk => Enums.ReasonForAdding.VeryHighRisk,
            Constants.ServiceNowReasonsForAdding.RequiresCeasing => Enums.ReasonForAdding.RequiresCeasing,
            Constants.ServiceNowReasonsForAdding.RoutineScreening => Enums.ReasonForAdding.RoutineScreening,
            Constants.ServiceNowReasonsForAdding.OverAgeSelfReferral => Enums.ReasonForAdding.OverAgeSelfReferral,
            _ => null
        };
    }
}
