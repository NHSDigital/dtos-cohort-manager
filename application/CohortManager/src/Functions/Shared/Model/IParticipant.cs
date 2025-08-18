namespace Model;

/// <summary>
/// Interface representing participant screening data
/// </summary>
public interface IParticipant
{
    /// <summary>
    /// The NHS number for the participant
    /// </summary>
    public string NhsNumber { get; set; }
    /// <summary>
    /// The screening ID of the screening service that
    /// the participant belongs to <see cref="ScreeningService"/>
    /// </summary>
    public string ScreeningId { get; set; }
    /// <summary>
    /// The name of the screening service that
    /// the participant belongs to <see cref="ScreeningService"/>
    /// </summary>
    public string? ScreeningName { get; set; }
    /// <summary>
    /// The record type of the participant
    /// <see cref="Actions"/>
    /// </summary>
    public string? RecordType { get; set; }
    /// <summary>
    /// The reason for the participants removal from a GP practice
    /// </summary>
    public string? ReasonForRemoval { get; set; }
    /// <summary>
    /// The date when the participant is removed
    /// from a GP practice
    /// </summary>
    public string? ReasonForRemovalEffectiveFromDate { get; set; }
    /// <summary>
    /// Flag representing whether or not the participant
    /// is eligible for screening
    /// </summary>
    public string? EligibilityFlag { get; set; }
    /// <summary>
    /// Flag representing whether or not the participant
    /// was referred (true) or came in through the routine 
    /// pathway (false) 
    /// </summary>
    public bool ReferralFlag { get; set; }
    /// <summary>
    /// The source of the participant, for routine
    /// this field is the filename of the file that the
    /// participant originated from
    /// </summary>
    public string? Source { get; set; }
}