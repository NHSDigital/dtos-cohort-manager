namespace NHS.CohortManager.Tests.CaasIntegrationTests;

using System.Collections.Concurrent;
using Azure.Storage.Queues;
using Common;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Model;
using Moq;
using NHS.Screening.ReceiveCaasFile;



[TestClass]
public class AddBatchToQueueTest
{
    private readonly Mock<IQueueClient> _mockQueueStorageHelper = new();
    private AddBatchToQueue _addBatchToQueue;

    public AddBatchToQueueTest()
    {
        _addBatchToQueue = new AddBatchToQueue(_mockQueueStorageHelper.Object);
    }

    [TestMethod]
    public async Task ProcessBatch_ValidRecord_ProcessSuccessfully()
    {
        //arrange
        Environment.SetEnvironmentVariable("AzureWebJobsStorage", "AzureWebJobsStorage");
        Environment.SetEnvironmentVariable("AddQueueName", "AddQueueName");
        Participant participant = new() { NhsNumber = "1234567890", Source = "TestFile" };

        var queue = new ConcurrentQueue<Participant>();

        queue.Enqueue(participant);

        Environment.SetEnvironmentVariable("AddQueueName", "AddQueueName");

        // Act
        await _addBatchToQueue.ProcessBatch(queue, "AddQueueName");

        //Assert
        _mockQueueStorageHelper.Verify(x => x.AddAsync(It.IsAny<Participant>(), It.IsAny<string>()), Times.Once());
    }

    [TestMethod]
    public async Task ProcessBatch_NoAddRecords_SendMessageNotCalled()
    {
        //arrange
        Environment.SetEnvironmentVariable("AzureWebJobsStorage", "AzureWebJobsStorage");
        Environment.SetEnvironmentVariable("AddQueueName", "AddQueueName");

        var queue = new ConcurrentQueue<Participant>();
        Environment.SetEnvironmentVariable("AddQueueName", "AddQueueName");

        // Act
        await _addBatchToQueue.ProcessBatch(queue, "AddQueueName");

        //Assert
        _mockQueueStorageHelper.Verify(x => x.AddAsync(It.IsAny<Participant>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task ProcessBatch_BatchIsNull_SendMessageNotCalled()
    {
        //arrange
        Environment.SetEnvironmentVariable("AzureWebJobsStorage", "AzureWebJobsStorage");
        Environment.SetEnvironmentVariable("AddQueueName", "AddQueueName");


        Environment.SetEnvironmentVariable("AddQueueName", "AddQueueName");

        // Act
        await _addBatchToQueue.ProcessBatch(null, "AddQueueName");

        //Assert
        _mockQueueStorageHelper.Verify(x => x.AddAsync(It.IsAny<Participant>(), It.IsAny<string>()), Times.Never);
    }
}
