namespace AddBatchFromQueue;
using Model;

public interface IAddBatchFromQueueHelper
{
    Task AddAllCohortRecordsToQueue(List<ParticipantCsvRecord> participantsData);
    Task<ParticipantCsvRecord?> ValidateMessageFromQueue(ParticipantCsvRecord participantCsvRecord);
    Task<ParticipantCsvRecord?> GetDemoGraphicData(string jsonFromQueue);
}