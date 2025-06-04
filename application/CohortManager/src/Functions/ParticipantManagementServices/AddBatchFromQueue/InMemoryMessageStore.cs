
namespace AddBatchFromQueue;

using System.Collections.Generic;
using System.Threading.Tasks;

public class InMemoryMessageStore : IMessageStore
{
    public long ExpectedMessageCount { get; set; } = 0;
    public List<SerializableMessage> ListOfAllValues { get; set; }
    public TaskCompletionSource<bool> AllMessagesReceived { get; set; }
    public bool processingCurrentBatch { get; set; } = false;

}