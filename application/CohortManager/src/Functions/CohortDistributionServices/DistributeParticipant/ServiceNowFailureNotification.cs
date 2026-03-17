namespace NHS.CohortManager.CohortDistributionServices;

using Model.Enums;

/// <summary>
/// DTO used to pass failure notification data to the SendServiceNowFailureMessage activity.
/// </summary>
public class ServiceNowFailureNotification
{
    public required string ServiceNowCaseNumber { get; set; }
    public required ServiceNowMessageType MessageType { get; set; }
}
