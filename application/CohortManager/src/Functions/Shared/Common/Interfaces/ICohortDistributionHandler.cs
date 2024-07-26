namespace Common;

public interface ICohortDistributionHandler{
    Task<bool> SendToCohortDistributionService(string nhsNumber, string screeningService);
}
