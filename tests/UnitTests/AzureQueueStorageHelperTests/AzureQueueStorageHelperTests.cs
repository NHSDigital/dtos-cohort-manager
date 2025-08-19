using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Xml.Xsl;
using Azure.Storage.Queues;
using Common;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Model;
using Moq;

namespace AzureQueueStorageHelperTests;

[TestClass]
public class AzureQueueStorageHelperTests
{

    AzureStorageQueueClient _queueHelper;

    Mock<IQueueClientFactory> mockQueueClientFactory;

    Mock<QueueClient> _mockQueueClient;

    Mock<ILogger<AzureStorageQueueClient>> _loggerMock;

    public AzureQueueStorageHelperTests()
    {
        mockQueueClientFactory = new Mock<IQueueClientFactory>();
        _loggerMock = new Mock<ILogger<AzureStorageQueueClient>>();
        _mockQueueClient = new("UseDevelopmentStorage=true", "testqueue");

        mockQueueClientFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(_mockQueueClient.Object);

        _queueHelper = new AzureStorageQueueClient(_loggerMock.Object, mockQueueClientFactory.Object);

    }

    [TestMethod]
    public async Task AddItemToQueueAsync_AddsRecordToQueue_True()
    {
        var testRecord = new Participant
        {
            Source = "test file name",

        };

        // Act
        var result = await _queueHelper.AddAsync(testRecord, "testqueue");

        // Assert
        Assert.IsTrue(result);

        var expectedJson = JsonSerializer.Serialize(testRecord);
        var expectedBase64Message = Convert.ToBase64String(Encoding.UTF8.GetBytes(expectedJson));

        _mockQueueClient.Verify(x => x.SendMessageAsync(It.Is<string>(msg => msg == expectedBase64Message)), Times.Once);

        _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
                     It.IsAny<EventId>(),
                     It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"There was an error while putting item on queue for queue: testqueue")),
                     It.IsAny<Exception>(),
                     It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                 Times.Never);
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

        var _queueHelper = new AzureStorageQueueClient(new Mock<ILogger<AzureStorageQueueClient>>().Object, mockQueueClientFactory.Object);

        var res = await _queueHelper.AddAsync<Participant>(new Participant(), "Some_Bad_Queue_Name");

        Assert.IsFalse(res);
    }

}
