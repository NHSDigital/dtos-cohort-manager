namespace NHS.CohortManager.ServiceNowIntegrationService;

public static class ServiceNowMessageTemplates
{
    public const string UnableToAddParticipantMessageTemplate = "Action needed {0}: the breast screening participant could not be added because either the details do not match Personal Demographics Service (PDS) records or they are not able to be added into Cohort Manager. Verify the participant's information and reason for adding and submit a new form with corrected details.";
    public const string AddRequestInProgressMessageTemplate = "The request to add a participant to the breast screening programme has been received for {0}. There may be a slight delay while we verify some information. No action is needed from you during this process.";
    public const string SuccessMessageTemplate = "The request to add a participant to the breast screening programme was successful for {0}. The participant has now been added to Cohort Manager. No further action is needed.";
}
