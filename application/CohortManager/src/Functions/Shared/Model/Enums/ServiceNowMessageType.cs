namespace Model.Enums;


public enum ServiceNowMessageType
{
    /// <summary>
    /// Does not match the participant in PDS.
    /// </summary>
    UnableToVerifyParticipant = 1,
    /// <summary>
    /// Add is in progress but an execption has been encountered.
    /// </summary>
    AddRequestInProgress = 2,
    /// <summary>
    /// The participant has been added to the cohort.
    /// </summary>
    Success = 3
}
