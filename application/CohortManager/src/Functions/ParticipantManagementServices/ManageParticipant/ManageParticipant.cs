namespace NHS.CohortManager.ParticipantManagementServices;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
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
    public async Task Run([ServiceBusTrigger(topicName: "%ParticipantManagementTopic%", subscriptionName: "%ManageParticipantSubscription%", Connection = "ServiceBusConnectionString_internal")] string message)
    {
        var participantRecord = JsonSerializer.Deserialize<BasicParticipantCsvRecord>(message)!;
        Participant participant = participantRecord.Participant;
        try
        {
            _logger.LogInformation("Received manage participant request");
            bool nhsNumberValid = ValidationHelper.ValidateNHSNumber(participant.NhsNumber);
            if (!nhsNumberValid)
            {
                await HandleException(new ArgumentException("NHS Number invalid"), participant, participantRecord.FileName);
                return;
            }

            long nhsNumber = long.Parse(participant.NhsNumber);
            long screeningId = long.Parse(participant.ScreeningId);

            var databaseParticipant = await _participantManagementClient.GetSingleByFilter(x => x.NHSNumber == nhsNumber && x.ScreeningId == screeningId);

            bool dataServiceResponse;
            if (databaseParticipant is null)
            {
                _logger.LogInformation("Participant not in participant management table, adding new record");
                var participantManagement = participant.ToParticipantManagement();
                participantManagement.RecordInsertDateTime = DateTime.UtcNow;

                dataServiceResponse = await _participantManagementClient.Add(participantManagement);
            }
            else if (databaseParticipant.BlockedFlag == 1)
            {
                await HandleException(new InvalidOperationException("Participant is blocked"), participant, participantRecord.FileName, "0");
                return;
            }
            else
            {
                _logger.LogInformation("Existing participant management record found, updating record {ParticipantId}", databaseParticipant.ParticipantId);
                var participantManagement = participant.ToParticipantManagement(databaseParticipant);
                participantManagement.RecordUpdateDateTime = DateTime.UtcNow;

                dataServiceResponse = await _participantManagementClient.Update(participantManagement);
            }

            if (!dataServiceResponse)
            {
                await HandleException(new InvalidOperationException("Participant Management Data Service request failed"), participant, participantRecord.FileName);
                return;
            }

            await _queueClient.AddAsync(participantRecord, _config.CohortDistributionTopic);
        }
        catch (Exception ex)
        {
            await HandleException(ex, participant, participantRecord.FileName);
        }
    }

    private async Task HandleException(Exception ex, Participant participant, string fileName, string category = "")
    {
        _logger.LogError(ex, "Manage Exception failed");
        await _handleException.CreateSystemExceptionLog(ex, participant, fileName, category);
    }
}
