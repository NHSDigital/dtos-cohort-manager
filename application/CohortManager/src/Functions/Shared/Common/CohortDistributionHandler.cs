namespace Common;

using Microsoft.Extensions.Logging;
using Model;

public class CohortDistributionHandler : ICohortDistributionHandler
{

    private readonly ILogger<CohortDistributionHandler> _logger;
    private readonly IQueueClient _azureQueueStorageHelper;

    public CohortDistributionHandler(ILogger<CohortDistributionHandler> logger, IQueueClient azureQueueStorageHelper)
    {
        _logger = logger;
        _azureQueueStorageHelper = azureQueueStorageHelper;
    }

    public async Task<bool> SendToCohortDistributionService(string nhsNumber, string screeningService, string recordType, string fileName, Participant errorRecord)
    {
        CreateCohortDistributionRequestBody requestBody = new CreateCohortDistributionRequestBody
        {
            NhsNumber = nhsNumber,
            ScreeningService = screeningService,
            FileName = fileName,
            RecordType = recordType,
            ErrorRecord = errorRecord
        };

        var CohortQueueName = Environment.GetEnvironmentVariable("CohortQueueName");
        if (string.IsNullOrEmpty(CohortQueueName))
        {
            throw new NullReferenceException("The cohort queue name was null");
        }
        await _azureQueueStorageHelper.AddAsync<CreateCohortDistributionRequestBody>(requestBody, CohortQueueName);

        _logger.LogInformation($"Participant sent to Cohort Distribution Service");
        return true;
    }
}
