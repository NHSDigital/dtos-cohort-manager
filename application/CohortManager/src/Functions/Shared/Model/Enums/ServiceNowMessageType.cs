namespace Model.Enums;


public enum ServiceNowMessageType
{
    /// <summary>
    /// Participant could not be added because either the details do not match PDS or
    /// they cannot be added to the cohort because they are blocked.
    /// </summary>
    UnableToAddParticipant = 1,
    /// <summary>
    /// Add is in progress but an execption has been encountered.
    /// </summary>
    AddRequestInProgress = 2,
    /// <summary>
    /// The participant has been added to the cohort.
    /// </summary>
    Success = 3
}
