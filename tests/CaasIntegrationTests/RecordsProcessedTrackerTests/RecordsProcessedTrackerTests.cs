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
    public void RecordAlreadyProcessed_ShouldReturnTrue_True()
    {
        // Arrange
        var tracker = new RecordsProcessedTracker();

        // Act
        var result = tracker.RecordAlreadyProcessed("Type1", "12345");

        // Assert
        Assert.IsTrue(result, "A new record should return true when checked.");
    }

    [TestMethod]
    public void RecordAlreadyProcessed_ShouldReturnFalse_False()
    {
        // Arrange
        var tracker = new RecordsProcessedTracker();

        // Act
        var firstResult = tracker.RecordAlreadyProcessed("Type1", "12345");
        var secondResult = tracker.RecordAlreadyProcessed("Type1", "12345");

        // Assert
        Assert.IsTrue(firstResult, "The first check should return true.");
        Assert.IsFalse(secondResult, "A duplicate record should return false.");
    }

    [TestMethod]
    public void RecordAlreadyProcessed_ShouldDifferentiateRecordsByType_True()
    {
        // Arrange
        var tracker = new RecordsProcessedTracker();

        // Act
        var firstResult = tracker.RecordAlreadyProcessed("Type1", "12345");
        var secondResult = tracker.RecordAlreadyProcessed("Type2", "12345");

        // Assert
        Assert.IsTrue(firstResult, "The first record should return true.");
        Assert.IsTrue(secondResult, "Different record types should be treated as unique.");
    }

    [TestMethod]
    public void RecordAlreadyProcessed_ShouldDifferentiateRecordsByNHSId_True()
    {
        // Arrange
        var tracker = new RecordsProcessedTracker();

        // Act
        var firstResult = tracker.RecordAlreadyProcessed("Type1", "12345");
        var secondResult = tracker.RecordAlreadyProcessed("Type1", "67890");

        // Assert
        Assert.IsTrue(firstResult, "The first record should return true.");
        Assert.IsTrue(secondResult, "Different NHSIds should be treated as unique.");
    }

}
