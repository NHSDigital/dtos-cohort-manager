namespace NHS.CohortManager.ReconciliationService;

public interface IReconciliationProcessor
{
    public Task<bool> RunReconciliation(DateTime fromDate);
}
