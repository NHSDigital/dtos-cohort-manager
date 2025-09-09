namespace NHS.CohortManager.ServiceNowIntegrationService;

public interface IServiceNowClient
{
    /// <summary>
    /// Sends an HTTP request to update a ServiceNow case. This method automatically handles access tokens including refresh.
    /// </summary>
    /// <param name="caseNumber">The ServiceNow case number used in the HTTP request path.</param>
    /// <param name="workNotes">The message to send in the request as the work_notes.</param>
    /// <param name="needsAttention">Does this update need attention in ServiceNow. If true, an assignment group will be assigned. Defaults to false.</param>
    /// <returns>
    /// An HTTP response indicating the result of the ServiceNow update request OR null if the update request was not made because no access token was found.
    /// </returns>
    Task<HttpResponseMessage?> SendUpdate(string caseNumber, string workNotes, bool needsAttention = false);

    /// <summary>
    /// Sends an HTTP request to resolve a ServiceNow case. This method automatically handles access tokens including refresh.
    /// </summary>
    /// <param name="caseNumber">The ServiceNow case number used in the HTTP request path.</param>
    /// <param name="closeNotes">The message to send in the request as the close_notes.</param>
    /// <returns>
    /// An HTTP response indicating the result of the ServiceNow resolution request OR null if the resolution request was not made because no access token was found.
    /// </returns>
    Task<HttpResponseMessage?> SendResolution(string caseNumber, string closeNotes);
}
