using Azure.Storage.Queues;
using Common;
using Microsoft.Extensions.Logging;
using Model;
using Moq;

namespace AzureQueueStorageHelperTests;

[TestClass]
public class AzureQueueStorageHelperTests
{

    AzureQueueStorageHelper _queueHelper;

    public AzureQueueStorageHelperTests()
    {
        Mock<QueueClient> mockQueueClient = new("UseDevelopmentStorage=true", "testqueue");

        var mockQueueClientFactory = new Mock<IQueueClientFactory>();

        mockQueueClientFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(mockQueueClient.Object);

        _queueHelper = new AzureQueueStorageHelper(new Mock<ILogger<AzureQueueStorageHelper>>().Object, mockQueueClientFactory.Object);

    }

    [TestMethod]
    public async Task AddItemToQueueAsync_AddsRecordToQueue_True()
    {
        var res = await _queueHelper.AddItemToQueueAsync<ParticipantCsvRecord>(new ParticipantCsvRecord(), "testqueue");

        Assert.IsTrue(res);
    }

    [TestMethod]
    public async Task AddItemToQueueAsync_AddsRecordToQueue_False()
    {
        var mockQueueClientFactory = new Mock<IQueueClientFactory>();
        Mock<QueueClient> mockQueueClient = new("UseDevelopmentStorage=true", "Some_Bad_Queue_Name");

        mockQueueClient.Setup(x => x.SendMessageAsync(It.IsAny<string>())).Throws(new Exception("some new error"));
        mockQueueClientFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(mockQueueClient.Object);

        var _queueHelper = new AzureQueueStorageHelper(new Mock<ILogger<AzureQueueStorageHelper>>().Object, mockQueueClientFactory.Object);

        var res = await _queueHelper.AddItemToQueueAsync<ParticipantCsvRecord>(new ParticipantCsvRecord(), "Some_Bad_Queue_Name");

        Assert.IsFalse(res);
    }

}
