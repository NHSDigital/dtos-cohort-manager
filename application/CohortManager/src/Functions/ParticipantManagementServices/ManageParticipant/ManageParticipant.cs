namespace NHS.CohortManager.ParticipantManagementServices;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Common;
using System.Text.Json;
using Model;

public class ManageParticipant
{
    private readonly ILogger<ManageParticipant> _logger;
    private readonly ManageParticipantConfig _config;
    private readonly IQueueClient _queueClient;

    public ManageParticipant(ILogger<ManageParticipant> logger, IOptions<ManageParticipantConfig> config, IQueueClient queueClient)
    {
        _logger = logger;
        _config = config.Value;
        _queueClient = queueClient;
    }

    [Function(nameof(ManageParticipant))]
    public async Task Run([ServiceBusTrigger("%ParticipantManagementQueueName%", Connection = "ServiceBusConnectionString")] string messageBody)
    {
        _logger.LogInformation($"Received message: {messageBody}");
        BasicParticipantCsvRecord message = JsonSerializer.Deserialize<BasicParticipantCsvRecord>(messageBody);
        _logger.LogInformation(_config.CohortQueueName);

        await _queueClient.AddAsync<BasicParticipantCsvRecord>(message, _config.CohortQueueName);
    }



}