public interface IServiceBusTopicHandler
{
    Task<bool> SendMessageToTopic(string TopicName, string MessageBody);

}
