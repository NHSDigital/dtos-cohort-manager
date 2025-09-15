namespace ReconciliationService;

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

    public async Task<bool> RunReconciliation(DateTime fromDate)
    {
        try
        {
            short isFatal = 1;
            var cohortDistributionRecords = await _cohortDistributionDataService.GetByFilter(x => x.RecordInsertDateTime!.Value > fromDate);
            var exceptionRecords = await _exceptionManagementDataService.GetByFilter(x => x.IsFatal.Value.Equals(1) && x.DateCreated!.Value > fromDate);


            var metrics = await _inboundMetricDataServiceAccessor.GetRange(x => x.ReceivedDateTime > fromDate && x.ProcessName == "AuditProcess");

            var recordsProcessed = exceptionRecords.DistinctBy(x=> x.NhsNumber).Count() + cohortDistributionRecords.Count();
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
            _logger.LogError(ex, "an exception occurred while reconciling participants");
            return false;
        }

    }
}
