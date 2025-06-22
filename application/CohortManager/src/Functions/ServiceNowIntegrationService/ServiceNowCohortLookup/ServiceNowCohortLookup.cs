namespace NHS.CohortManager.ServiceNowIntegrationService;

using Microsoft.Azure.Functions.Worker;
using Common;
using Microsoft.Extensions.Logging;
using Model;
using DataServices.Client;
using Microsoft.Extensions.Options;

public class ServiceNowCohortLookup
{
    private readonly ILogger<ServiceNowCohortLookup> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ServiceNowCohortLookupConfig _config;
    private readonly IDataServiceClient<CohortDistribution> _cohortDistributionClient;
    private readonly IDataServiceClient<ServicenowCases> _serviceNowCasesClient;

    public ServiceNowCohortLookup(
        ILogger<ServiceNowCohortLookup> logger,
        IOptions<ServiceNowCohortLookupConfig> config,
        IDataServiceClient<CohortDistribution> cohortDistributionClient,
        IDataServiceClient<ServicenowCases> serviceNowCasesClient)
    {
        _logger = logger;
        _config = config.Value;
        _cohortDistributionClient = cohortDistributionClient;
        _serviceNowCasesClient = serviceNowCasesClient;
    }

    /// <summary>
    /// Azure Timer Function to check status of participants that have been received via serviceNow and updates participant statuses on a daily schedule.
    /// </summary>
    /// <remarks>
    /// This timer-triggered function:
    /// 1. Gets the NHS Numbers from the ServiceNow_Cases table where status = NEW
    /// 2. Lookup the NHS Numbers in Cohort Distribution
    /// 3. If the NHS Number is in Cohort Distribution, change the status in the ServiceNow_Cases table to COMPLETE
    /// 4. This function should run once every 24 hours
    /// 5. Logs processing metrics and schedule information
    /// </remarks>
    /// <param name="myTimer">The TimerInfo object containing schedule information</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    [Function("ServiceNowCohortLookup")]
    public async Task Run([TimerTrigger("0 0 0 * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation($"ServiceNowCohortLookup function started at: {DateTime.Now}");

        try
        {
            _logger.LogInformation("Starting ServiceNow cohort lookup processing");
            var processingResult = await ProcessNewServiceNowCasesAsync();

            _logger.LogInformation(
                "Completed processing. Status updates: {UpdatedCount}/{TotalCases} cases marked as {CompleteStatus}",
                processingResult.ProcessedCount,
                processingResult.TotalCases,
                ServiceNowStatus.Complete);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process ServiceNow cohort lookup timer function.");
        }

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {scheduleStatus}", myTimer.ScheduleStatus.Next);
        }
    }

    /// <summary>
    /// Processes all new ServiceNow cases and updates their status if participants are found
    /// </summary>
    /// <remarks>
    /// This method:
    /// 1. Retrieves all records/cases from the ServiceNow_Cases table where status = NEW
    /// 2. Processes each record/case through <see cref="ProcessSingleCaseAsync"/>
    /// 3. Tracks success/failure counts
    /// </remarks>
    /// <returns>
    /// <list>
    ///   <item><term>ProcessedCount</term><description>Number of successfully updated servicenow cases</description></item>
    ///   <item><term>TotalCases</term><description>Total number of servicenow cases found</description></item>
    /// </list>
    /// </returns>
    private async Task<(int ProcessedCount, int TotalCases)> ProcessNewServiceNowCasesAsync()
    {
        var serviceNowCases = (await _serviceNowCasesClient.GetByFilter(c => c.Status == ServiceNowStatus.New)).ToList();
        _logger.LogInformation("Found {CaseCount} servicenow cases with status {NewStatus}.", serviceNowCases.Count, ServiceNowStatus.New);

        var processedCount = 0;
        foreach (var caseItem in serviceNowCases)
        {
            try
            {
                if (await ProcessSingleCaseAsync(caseItem))
                {
                    processedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process servicenow case {ServicenowId}", caseItem.ServicenowId);
            }
        }
        return (processedCount, serviceNowCases.Count);
    }

    /// <summary>
    /// Processes an individual ServiceNow case by verifying participant and updating status
    /// </summary>
    /// <remarks>
    /// This method:
    /// 1. Lookup the NHS Numbers in Cohort Distribution from the servicenow case
    /// 2. Updates servicenow case status to 'COMPLETE' if participant exists in Cohort Distribution table.
    /// 3. Returns whether the case was successfully processed
    /// </remarks>
    /// <param name="caseItem">The ServiceNow case to process</param>
    /// <returns>
    /// True if case was successfully updated, false if participant was not found
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the case status cannot be updated in ServiceNow
    /// </exception>
    private async Task<bool> ProcessSingleCaseAsync(ServicenowCases caseItem)
    {
        var participant = await _cohortDistributionClient.GetSingleByFilter(p => caseItem.NhsNumber.HasValue && p.NHSNumber == caseItem.NhsNumber.Value);
        if (participant == null)
        {
            _logger.LogInformation("No participant found for Servicenow Id: {ServicenowId}", caseItem.ServicenowId);
            return false;
        }

        caseItem.Status = ServiceNowStatus.Complete;
        caseItem.RecordUpdateDatetime = DateTime.Now;
        var updateSuccess = await _serviceNowCasesClient.Update(caseItem);

        if (!updateSuccess)
        {
            throw new InvalidOperationException($"Failed to update status for servicenow case {caseItem.ServicenowId}.");
        }
        _logger.LogInformation("Updated servicenow case {ServicenowId} to status {CompleteStatus}", caseItem.ServicenowId, ServiceNowStatus.Complete);
        return true;
    }
}
