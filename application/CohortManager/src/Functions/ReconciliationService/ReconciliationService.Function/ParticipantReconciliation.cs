namespace NHS.CohortManager.ReconciliationService;

using DataServices.Client;
using DataServices.Core;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;

public class ParticipantReconciliation : IReconciliationProcessor
{
    private readonly ILogger<ParticipantReconciliation> _logger;
    private readonly IDataServiceAccessor<InboundMetric> _inboundMetricDataServiceAccessor;
    private readonly IDataServiceClient<CohortDistribution> _cohortDistributionDataService;
    private readonly IDataServiceClient<ExceptionManagement> _exceptionManagementDataService;

    public ParticipantReconciliation(
        ILogger<ParticipantReconciliation> logger,
        IDataServiceAccessor<InboundMetric> inboundMetricDataServiceAccessor,
        IDataServiceClient<CohortDistribution> cohortDistributionDataService,
        IDataServiceClient<ExceptionManagement> exceptionManagementDataService
    )
    {
        _logger = logger;
        _inboundMetricDataServiceAccessor = inboundMetricDataServiceAccessor;
        _cohortDistributionDataService = cohortDistributionDataService;
        _exceptionManagementDataService = exceptionManagementDataService;

    }
    /// <summary>
    /// Runs a reconciliation where it will validate that all of the records received are processed successfully or logged to the exception table from the from date provided
    /// </summary>
    /// <param name="fromDate"></param>
    /// <returns>returns boolean if the reconciliation ran successfully (no error not a mis-match)</returns>
    public async Task<bool> RunReconciliation(DateTime fromDate)
    {
        try
        {
            var cohortDistributionRecords = await _cohortDistributionDataService.GetByFilter(x => x.RecordInsertDateTime!.Value > fromDate);
            var exceptionRecords = await _exceptionManagementDataService.GetByFilter(x => x.IsFatal!.Value.Equals(1) && x.DateCreated!.Value > fromDate);


            var metrics = await _inboundMetricDataServiceAccessor.GetRange(x => x.ReceivedDateTime > fromDate && x.ProcessName == "AuditProcess");
            var exceptionCount = exceptionRecords.Where(x => !string.IsNullOrWhiteSpace(x.NhsNumber)).DistinctBy(x => x.NhsNumber).Count();
            var recordsProcessed = exceptionCount + cohortDistributionRecords.Count();
            var recordsExpected = metrics.Sum(x => x.RecordCount);

            if (recordsExpected != recordsProcessed)
            {
                _logger.LogCritical("Expected Records {ExpectedCount} Didn't equal Records Processed {ProcessedCount}", recordsExpected, recordsProcessed);
            }
            else
            {
                _logger.LogInformation("Expected Records {ExpectedCount} equaled Records Processed {ProcessedCount}", recordsExpected, recordsProcessed);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "an exception occurred while reconciling participants from date: {FromDate}",fromDate);
            return false;
        }

    }
}
