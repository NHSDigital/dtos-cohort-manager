namespace NHS.CohortManager.ServiceNowIntegrationService;

using NHS.CohortManager.ServiceNowIntegrationService.Models;

public interface IServiceNowClient
{
    /// <summary>
    /// Sends an HTTP request to update a ServiceNow case. This method automatically handles access tokens including refresh.
    /// </summary>
    /// <param name="sysId">The ServiceNow case system identifier (sys_id) used in the HTTP request path.</param>
    /// <param name="payload">The ServiceNowUpdateRequestBody that will be sent in the HTTP request body.</param>
    /// <returns>
    /// An HTTP response indicating the result of the ServiceNow update request OR null if the update request was not made because no access token was found.
    /// </returns>
    Task<HttpResponseMessage?> SendUpdate(string sysId, ServiceNowUpdateRequestBody payload);
}
