namespace Common;

using Model;

public interface ICohortDistributionHandler
{
    Task<bool> SendToCohortDistributionService(string nhsNumber, string screeningService, string recordType, string fileName, Participant errorRecord);
}
