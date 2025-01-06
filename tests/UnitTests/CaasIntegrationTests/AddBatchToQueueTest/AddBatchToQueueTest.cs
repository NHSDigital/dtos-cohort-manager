namespace NHS.CohortManager.Tests.CaasIntegrationTests;

using Azure.Storage.Queues;
using Common;
using Microsoft.Extensions.Logging;
using Model;
using Moq;
using NHS.Screening.ReceiveCaasFile;



[TestClass]
public class AddBatchToQueueTest
{
    private readonly Mock<ILogger<AddBatchToQueue>> _loggerMock = new();
    private readonly Mock<IAzureQueueStorageHelper>  mockQueueStorageHelper = new();
    private AddBatchToQueue _addBatchToQueue;



    public AddBatchToQueueTest()
    {


        _addBatchToQueue = new AddBatchToQueue(_loggerMock.Object, mockQueueStorageHelper.Object);
    }

    [TestMethod]
    public async Task ProcessBatch_ValidRecord_ProcessSuccessfully()
    {
        //arrange
        Environment.SetEnvironmentVariable("AzureWebJobsStorage", "AzureWebJobsStorage");
        Environment.SetEnvironmentVariable("AddQueueName", "AddQueueName");

        Batch batch = new Batch();
        BasicParticipantCsvRecord basicParticipantCsvRecord = new BasicParticipantCsvRecord();
        basicParticipantCsvRecord.FileName = "TestFile";
        basicParticipantCsvRecord.Participant = new BasicParticipantData() { NhsNumber = "1234567890" };
        basicParticipantCsvRecord.participant = new Participant() { NhsNumber = "1234567890" };
        batch.AddRecords.Enqueue(basicParticipantCsvRecord);

        Environment.SetEnvironmentVariable("AddQueueName", "AddQueueName");

        // Act
        await _addBatchToQueue.ProcessBatch(batch);

        //Assert
        mockQueueStorageHelper.Verify(x => x.AddItemToQueueAsync(It.IsAny<BasicParticipantCsvRecord>(),It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task ProcessBatch_NoAddRecords_SendMessageNotCalled()
    {
        //arrange
        Environment.SetEnvironmentVariable("AzureWebJobsStorage", "AzureWebJobsStorage");
        Environment.SetEnvironmentVariable("AddQueueName", "AddQueueName");

        Batch batch = new Batch();
        Environment.SetEnvironmentVariable("AddQueueName", "AddQueueName");

        // Act
        await _addBatchToQueue.ProcessBatch(batch);

        //Assert
        mockQueueStorageHelper.Verify(x => x.AddItemToQueueAsync(It.IsAny<BasicParticipantCsvRecord>(),It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task ProcessBatch_BatchIsNull_SendMessageNotCalled()
    {
        //arrange
        Environment.SetEnvironmentVariable("AzureWebJobsStorage", "AzureWebJobsStorage");
        Environment.SetEnvironmentVariable("AddQueueName", "AddQueueName");


        Environment.SetEnvironmentVariable("AddQueueName", "AddQueueName");

        // Act
        await _addBatchToQueue.ProcessBatch(null);

        //Assert
        mockQueueStorageHelper.Verify(x => x.AddItemToQueueAsync(It.IsAny<BasicParticipantCsvRecord>(),It.IsAny<string>()), Times.Never);
    }

}
