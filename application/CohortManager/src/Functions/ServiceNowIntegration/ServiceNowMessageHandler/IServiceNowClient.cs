namespace NHS.CohortManager.ServiceNowIntegrationService;

using NHS.CohortManager.ServiceNowIntegrationService.Models;

public interface IServiceNowClient
{
    Task<HttpResponseMessage?> SendUpdate(string sysId, ServiceNowUpdateRequestBody payload);
}
