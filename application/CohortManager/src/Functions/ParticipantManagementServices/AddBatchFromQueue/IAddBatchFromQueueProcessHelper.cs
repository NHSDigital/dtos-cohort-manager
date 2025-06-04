namespace AddBatchFromQueue;

using Azure.Messaging.ServiceBus;
using Model;

public interface IAddBatchFromQueueProcessHelper
{
    Task processItem(ServiceBusReceiver receiver, List<SerializableMessage> messages, List<ParticipantCsvRecord> participantsData, List<ParticipantManagement> participants, ParallelOptions options, int totalMessages);
    Task AddAllCohortRecordsToQueue(List<ParticipantCsvRecord> participantsData);
}