namespace Common;

public interface ICohortDistributionHandler
{
    Task<bool> SendToCohortDistributionService(string nhsNumber, string screeningService, string recordType, string fileName, string errorRecord);
}
