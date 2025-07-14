namespace Common;

public interface IQueueClient
{
    Task<bool> AddAsync<T>(T message, string queueName);
}
