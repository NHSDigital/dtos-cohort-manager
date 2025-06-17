namespace Common;

using Azure.Storage.Queues;
using Model;

public interface IQueueSender
{
    Task<bool> AddMessageToQueueAsync<T>(T message, string queueName);
}
