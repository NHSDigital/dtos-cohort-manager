namespace Common.Interfaces;

using System.Collections.Concurrent;
using Azure.Storage.Queues;
using Model;

public interface IProcessCaasFile
{
    Task AddBatchToQueue(Batch currentBatch, string name);

    Task<QueueClient> CreateAddQueueCLient();




}

