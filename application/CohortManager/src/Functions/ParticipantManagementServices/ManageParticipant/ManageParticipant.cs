namespace NHS.CohortManager.ParticipantManagementServices;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

public class ManageParticipant
{
    public readonly ILogger<ManageParticipant> _logger;
    public readonly ManageParticipantConfig _config;

    public ManageParticipant(ILogger<ManageParticipant> logger, IOptions<ManageParticipantConfig> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    [Function(nameof(ManageParticipant))]
    public async Task Run([ServiceBusTrigger("participant-management-queue", Connection = "ServiceBusConnectionString")] string messageBody)
    {
        _logger.LogInformation($"Received message: {messageBody}. Starting new orchestration.");

    }



}