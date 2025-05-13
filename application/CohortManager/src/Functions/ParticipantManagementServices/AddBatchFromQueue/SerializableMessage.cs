namespace AddBatchFromQueue;

public class SerializableMessage
{
    public string MessageId { get; set; }
    public string Body { get; set; }
    public string Subject { get; set; }
    public long SequenceNumber { get; set; }
    public DateTimeOffset EnqueuedTime { get; set; }
}