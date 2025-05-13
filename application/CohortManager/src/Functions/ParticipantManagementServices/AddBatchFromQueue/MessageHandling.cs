namespace AddBatchFromQueue;

using Azure.Messaging.ServiceBus;

public class MessageHandling : IMessageHandling
{

    public async Task MessageHandler(ProcessMessageEventArgs args)
    {

        var message = new SerializableMessage()
        {
            MessageId = args.Message.MessageId,
            Body = args.Message.Body.ToString(),
            Subject = args.Message.Subject,
            EnqueuedTime = args.Message.EnqueuedTime,
            SequenceNumber = args.Message.SequenceNumber
        };

        AddBatchFromQueue.listOfAllValues.Add(message);
        await args.DeferMessageAsync(args.Message);

        if (AddBatchFromQueue.listOfAllValues.Count >= AddBatchFromQueue._expectedMessageCount)
        {
            AddBatchFromQueue._allMessagesReceived.TrySetResult(true);
        }
    }

    // handle any errors when receiving messages
    public Task ErrorHandler(ProcessErrorEventArgs args)
    {
        Console.WriteLine(args.Exception.ToString());
        return Task.CompletedTask;
    }
}