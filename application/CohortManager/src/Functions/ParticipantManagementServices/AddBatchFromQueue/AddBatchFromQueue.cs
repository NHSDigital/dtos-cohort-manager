namespace AddBatchFromQueue;

using System;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Common;
using DataServices.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Model;

public class AddBatchFromQueue
{
    private readonly ILogger _logger;

    private readonly IDataServiceClient<ParticipantManagement> _participantManagementClient;

    private readonly ICohortDistributionHandler _cohortDistributionHandler;

    private readonly IDurableAddProcessor _durableAddProcessor;

    private readonly IExceptionHandler _handleException;

    public AddBatchFromQueue(ILoggerFactory loggerFactory, IDataServiceClient<ParticipantManagement> participantManagementClient, ICohortDistributionHandler cohortDistributionHandler, IDurableAddProcessor durableAddProcessor, IExceptionHandler exceptionHandler)
    {
        _logger = loggerFactory.CreateLogger<AddBatchFromQueue>();
        _participantManagementClient = participantManagementClient;
        _cohortDistributionHandler = cohortDistributionHandler;
        _durableAddProcessor = durableAddProcessor;
        _handleException = exceptionHandler;
    }

    [Function("AddBatchFromQueue")]
    public async Task Run([TimerTrigger("0 */1 * * * *", RunOnStartup = true)] TimerInfo myTimer)
    {
        _logger.LogInformation($"DrainServiceBusQueue started at: {DateTime.Now}");

        var client = new ServiceBusClient(Environment.GetEnvironmentVariable("QueueConnectionString"));
        var receiver = client.CreateReceiver(Environment.GetEnvironmentVariable("QueueName"), new ServiceBusReceiverOptions
        {
            ReceiveMode = ServiceBusReceiveMode.PeekLock
        });

        var batchSize = 100;
        int totalMessages = 0;


        IReadOnlyList<ServiceBusReceivedMessage> messages = await receiver.ReceiveMessagesAsync(batchSize, TimeSpan.FromSeconds(5));

        if (messages.Count == 0)
        {
            _logger.LogInformation("nothing to process");
            return;
        }

        var participants = new List<ParticipantManagement>();
        var participantsData = new List<ParticipantCsvRecord>();

        foreach (var message in messages)
        {
            try
            {
                string jsonFromQueue = message.Body.ToString();
                _logger.LogInformation($"Processing message: {jsonFromQueue}");

                var ParticipantCsvRecord = await _durableAddProcessor.ProcessAddRecord(jsonFromQueue);
                participantsData.Add(ParticipantCsvRecord!);
                participants.Add(ParticipantCsvRecord!.Participant.ToParticipantManagement());
                await receiver.CompleteMessageAsync(message);
                totalMessages++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process message");
                await receiver.AbandonMessageAsync(message);
            }
        }
        // add batch to database
        await _participantManagementClient.AddRange(participants);

        // add all items to queue
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



        _logger.LogWarning($"Drained and processed {totalMessages} messages from the queue.");
        await receiver.CloseAsync();
    }
}

