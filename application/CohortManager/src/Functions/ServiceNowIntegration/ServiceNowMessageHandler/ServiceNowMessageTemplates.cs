namespace NHS.CohortManager.ServiceNowIntegrationService;

public static class ServiceNowMessageTemplates
{
    public const string UnableToAddParticipantMessageTemplate = "Action needed for {0}: We could not add this participant. This may be because the details entered do not match NHS records. Please check the participant's information and the reason for adding, then submit a new request if appropriate.";
    public const string AddRequestInProgressMessageTemplate = "Update on {0}: Thank you for your request to add a participant to the breast screening programme. There may be a slight delay while the information continues to be validated. Please allow time for this process to complete before following up on your request.";
    public const string SuccessMessageTemplate = "Update on {0}: Validation checks are complete and your request has been sent to BS Select for processing. Please note that it may take up to 24 hours before it’s visible in BS Select.";
}
