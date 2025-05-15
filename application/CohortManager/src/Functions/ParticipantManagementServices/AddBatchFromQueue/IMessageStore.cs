
namespace AddBatchFromQueue;


public interface IMessageStore
{
    long ExpectedMessageCount { get; set; }
    List<SerializableMessage> ListOfAllValues { get; set; }
    TaskCompletionSource<bool> AllMessagesReceived { get; set; }
}