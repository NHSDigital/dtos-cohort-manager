namespace Common;

using Azure.Storage.Queues;
using Model;

public interface IQueueClient
{
    Task<bool> AddAsync<T>(T message, string queueName);
}
