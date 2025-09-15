namespace NHS.CohortManager.ReconciliationService;

public interface IReconciliationProcessor
{
    /// <summary>
    /// Runs a reconciliation process to ensure all records are accounted for.
    /// Takes in a from date to reconcile records from a set date.
    /// </summary>
    /// <param name="fromDate"></param>
    /// <returns>bool if run was a success or failure</returns>
    public Task<bool> RunReconciliation(DateTime fromDate);
}
