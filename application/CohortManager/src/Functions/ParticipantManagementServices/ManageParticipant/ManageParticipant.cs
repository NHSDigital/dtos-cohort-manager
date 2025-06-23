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
using DataServices.Client;

public class ManageParticipant
{
    private readonly ILogger<ManageParticipant> _logger;
    private readonly ManageParticipantConfig _config;
    private readonly IQueueClient _queueClient;
    private readonly IDataServiceClient<ParticipantManagement> _participantManagementClient;
    private readonly IExceptionHandler _handleException;

    public ManageParticipant(ILogger<ManageParticipant> logger,
                            IOptions<ManageParticipantConfig> config,
                            IQueueClient queueClient,
                            IDataServiceClient<ParticipantManagement> participantManagementClient,
                            IExceptionHandler handleException)
    {
        _logger = logger;
        _config = config.Value;
        _queueClient = queueClient;
        _participantManagementClient = participantManagementClient;
        _handleException = handleException;
    }

    /// <summary>
    /// Reads messages from the participant management queue, adds/ updates the record in participant management,
    /// and sends the record to cohort distribution
    /// </summary>
    /// <param name="message">json string containing the participant record</param>
    [Function(nameof(ManageParticipant))]
    public async Task Run([ServiceBusTrigger("%ParticipantManagementQueueName%", Connection = "ServiceBusConnectionString")] string message)
    {
        var participantRecord = JsonSerializer.Deserialize<BasicParticipantCsvRecord>(message)!;
        Participant participant = participantRecord.participant;
        try
        {
            _logger.LogInformation("Recieved manage participant request");
            bool nhsNumberValid = ValidationHelper.ValidateNHSNumber(participant.NhsNumber);
            if (!nhsNumberValid)
            {
                await HandleException(new ArgumentException("NHS Number invalid"), participant, participantRecord.FileName);
                return;
            }

            long nhsNumber = long.Parse(participant.NhsNumber);
            short screeningId = short.Parse(participant.ScreeningId);

            var databaseParticipant = await _participantManagementClient.GetSingleByFilter(x => x.NHSNumber == nhsNumber && x.ScreeningId == screeningId);

            bool dataServiceResponse;
            if (databaseParticipant is null)
            {
                dataServiceResponse = await _participantManagementClient.Add(participant.ToParticipantManagement());
            }
            else if (databaseParticipant.BlockedFlag == 1)
            {
                await HandleException(new InvalidOperationException("Participant is blocked"), participant, participantRecord.FileName);
                return;
            }
            else
            {
                dataServiceResponse = await _participantManagementClient.Update(participant.ToParticipantManagement());
            }

            if (!dataServiceResponse)
            {
                await HandleException(new InvalidOperationException("Participant Management Data Service request failed"), participant, participantRecord.FileName);
                return;
            }

            await _queueClient.AddAsync(participantRecord, _config.CohortQueueName);
        }
        catch (Exception ex)
        {
            await HandleException(ex, participant, participantRecord.FileName);
        }
    }

    private async Task HandleException(Exception ex, Participant participant, string fileName)
    {
        _logger.LogError(ex, "Manage Exception failed");
        await _handleException.CreateSystemExceptionLog(ex, participant, fileName);
    }
}