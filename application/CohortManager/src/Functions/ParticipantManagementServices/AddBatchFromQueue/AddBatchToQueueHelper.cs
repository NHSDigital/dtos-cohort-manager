namespace AddBatchFromQueue;

using Azure.Messaging.ServiceBus;
using Common;
using Microsoft.Extensions.Logging;
using Model;

public class AddBatchFromQueueProcessHelper : IAddBatchFromQueueProcessHelper
{
    IDurableAddProcessor _durableAddProcessor;
    ILogger<IAddBatchFromQueueProcessHelper> _logger;
    ICohortDistributionHandler _cohortDistributionHandler;
    IExceptionHandler _handleException;

    public AddBatchFromQueueProcessHelper(ILogger<IAddBatchFromQueueProcessHelper> logger, IDurableAddProcessor durableAddProcessor, ICohortDistributionHandler cohortDistributionHandler, IExceptionHandler handleException)
    {
        _logger = logger;
        _durableAddProcessor = durableAddProcessor;
        _cohortDistributionHandler = cohortDistributionHandler;
        _handleException = handleException;
    }

    public async Task processItem(ServiceBusReceiver receiver, List<SerializableMessage> messages, List<ParticipantCsvRecord> participantsData, List<ParticipantManagement> participants, ParallelOptions options, int totalMessages)
    {
        foreach (var message in messages)
        {
            var fullMessage = await receiver.ReceiveDeferredMessageAsync(message.SequenceNumber);
            try
            {
                string jsonFromQueue = message.Body;
                _logger.LogInformation($"Processing message: {jsonFromQueue}");

                var ParticipantCsvRecord = await _durableAddProcessor.ProcessAddRecord(jsonFromQueue);
                if (ParticipantCsvRecord == null)
                {
                    _logger.LogError("The result of processing a record was null, see errors in database for more details. Will still process anymore records");
                    // this sends a record to the dead letter queue on error
                    await receiver.DeadLetterMessageAsync(fullMessage);
                }
                // we only want to add non null items to the database and log error records to the database this handled by validation 
                else
                {
                    participantsData.Add(ParticipantCsvRecord!);
                    await receiver.CompleteMessageAsync(fullMessage);

                    participants.Add(ParticipantCsvRecord!.Participant.ToParticipantManagement());
                    totalMessages++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process message");
                // send error messages to the dead letter queue
                await receiver.DeadLetterMessageAsync(fullMessage);
            }
        }

    }

    public async Task AddAllCohortRecordsToQueue(List<ParticipantCsvRecord> participantsData)
    {
        foreach (var ParticipantCsvRecord in participantsData)
        {
            var cohortDistResponse = await _cohortDistributionHandler.SendToCohortDistributionService(ParticipantCsvRecord.Participant.NhsNumber!, ParticipantCsvRecord.Participant.ScreeningId!, ParticipantCsvRecord.Participant.RecordType!, ParticipantCsvRecord.FileName, ParticipantCsvRecord.Participant);
            if (!cohortDistResponse)
            {
                _logger.LogError("Participant failed to send to Cohort Distribution Service");
                await _handleException.CreateSystemExceptionLog(new Exception("participant failed to send to Cohort Distribution Service"), ParticipantCsvRecord.Participant, ParticipantCsvRecord.FileName);
                return;
            }
        }
    }
}