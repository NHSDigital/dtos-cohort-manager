namespace AddBatchFromQueue;

using Model;

public interface IAddBatchFromQueueHelper
{
    Task AddAllCohortRecordsToQueue(List<ParticipantCsvRecord> participantsData);
    Task<List<SerializableMessage>?> ValidateMessageFromQueue(List<SerializableMessage> serializableMessages);
    Task<List<SerializableMessage>?> GetDemoGraphicData(List<SerializableMessage> serializableMessages);
}