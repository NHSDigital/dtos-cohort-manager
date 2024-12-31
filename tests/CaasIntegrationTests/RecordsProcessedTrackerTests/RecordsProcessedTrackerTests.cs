namespace NHS.CohortManager.Tests.CaasIntegrationTests;

using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using Model;
using Moq;
using NHS.Screening.ReceiveCaasFile;
using receiveCaasFile;

[TestClass]
public class RecordsProcessedTrackerTests
{

    [TestMethod]
    public void RecordNotAlreadyProcessed_ShouldReturnTrue_ForNewRecord()
    {
        // Arrange
        var tracker = new RecordsProcessedTracker();

        // Act
        var result = tracker.RecordNotAlreadyProcessed("Type1", "12345");

        // Assert
        Assert.IsTrue(result, "A new record should return true when checked.");
    }

    [TestMethod]
    public void RecordNotAlreadyProcessed_ShouldReturnFalse_ForDuplicateRecord()
    {
        // Arrange
        var tracker = new RecordsProcessedTracker();

        // Act
        var firstResult = tracker.RecordNotAlreadyProcessed("Type1", "12345");
        var secondResult = tracker.RecordNotAlreadyProcessed("Type1", "12345");

        // Assert
        Assert.IsTrue(firstResult, "The first check should return true.");
        Assert.IsFalse(secondResult, "A duplicate record should return false.");
    }

    [TestMethod]
    public void RecordNotAlreadyProcessed_ShouldDifferentiateRecordsByType()
    {
        // Arrange
        var tracker = new RecordsProcessedTracker();

        // Act
        var firstResult = tracker.RecordNotAlreadyProcessed("Type1", "12345");
        var secondResult = tracker.RecordNotAlreadyProcessed("Type2", "12345");

        // Assert
        Assert.IsTrue(firstResult, "The first record should return true.");
        Assert.IsTrue(secondResult, "Different record types should be treated as unique.");
    }

    [TestMethod]
    public void RecordNotAlreadyProcessed_ShouldDifferentiateRecordsByNHSId()
    {
        // Arrange
        var tracker = new RecordsProcessedTracker();

        // Act
        var firstResult = tracker.RecordNotAlreadyProcessed("Type1", "12345");
        var secondResult = tracker.RecordNotAlreadyProcessed("Type1", "67890");

        // Assert
        Assert.IsTrue(firstResult, "The first record should return true.");
        Assert.IsTrue(secondResult, "Different NHSIds should be treated as unique.");
    }

}
