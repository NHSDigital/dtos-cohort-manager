namespace NHS.CohortManager.ServiceNowIntegrationService;

using NHS.CohortManager.ServiceNowIntegrationService.Models;

public interface IServiceNowClient
{
    /// <summary>
    /// Sends an HTTP request update a ServiceNow case.
    /// </summary>
    /// <param name="sysId">The ServiceNow case system identifier (sys_id) used in the HTTP request path.</param>
    /// <param name="payload">The ServiceNowUpdateRequestBody that will be sent in the HTTP request body.</param>
    /// <returns>
    /// An HTTP response indicating the result of the operation
    /// </returns>
    Task<HttpResponseMessage?> SendUpdate(string sysId, ServiceNowUpdateRequestBody payload);
}
