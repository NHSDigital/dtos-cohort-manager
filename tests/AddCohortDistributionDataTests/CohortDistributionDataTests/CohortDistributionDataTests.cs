namespace NHS.CohortManager.Tests.CohortDistributionDataTests;

using Common;
using Data.Database;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Model;
using Model.Enums;
using Moq;
using System.Data;

[TestClass]
public class CohortDistributionDataTests
{
    private readonly Mock<IDbConnection> _mockDbConnection = new();
    private readonly Mock<IDatabaseHelper> _mockDatabaseHelper = new();
    private readonly Mock<ILogger<CreateCohortDistributionData>> _mockLogger = new();
    private readonly CreateCohortDistributionData _service;
    private readonly string _json;
    private const int expectedNoRowsCount = 0;

    public CohortDistributionDataTests()
    {
        _service = new CreateCohortDistributionData(
            _mockDbConnection.Object,
            _mockDatabaseHelper.Object,
            _mockLogger.Object);

        _json = FileReader.ReadJsonFileFromPath(MockTestFiles.CohortMockData1000Participants);
    }

    [TestMethod]
    public void GetCohortDistributionParticipantsMock_ShouldReturnRowCountFilteredParticipants_WhenFileExists()
    {
        // Arrange
        var serviceProviderId = (int)ServiceProvider.BsSelect;
        var rowCount = 10;

        // Act`
        var result = _service.GetCohortDistributionParticipantsMock(serviceProviderId, rowCount, _json);

        // Assert
        Assert.AreEqual(rowCount, result.Count);
    }

    [TestMethod]
    public void GetCohortDistributionParticipantsMock_ShouldReturnFilteredParticipants1000Records_WhenFileExists()
    {
        // Arrange
        var serviceProviderId = (int)ServiceProvider.BsSelect;
        var rowCount = 1000;
        var expectedRows = 900;

        // Act`
        var result = _service.GetCohortDistributionParticipantsMock(serviceProviderId, rowCount, _json);

        // Assert
        Assert.AreEqual(expectedRows, result.Count);
    }

    [TestMethod]
    public void GetCohortDistributionParticipantsMock_ShouldReturnMax1000Records_WhenFileExists()
    {
        // Arrange
        var serviceProviderId = (int)ServiceProvider.BsSelect;
        var rowCount = 2000;
        var expectedRows = 900;

        // Act`
        var result = _service.GetCohortDistributionParticipantsMock(serviceProviderId, rowCount, _json);

        // Assert
        Assert.AreEqual(expectedRows, result.Count);
    }

    [TestMethod]
    public void GetCohortDistributionParticipantsMock_ShouldReturnEmptyList_WhenFileIsEmpty()
    {
        // Arrange
        var serviceProviderId = (int)ServiceProvider.BsSelect;
        var rowCount = 5;

        // Act
        var result = _service.GetCohortDistributionParticipantsMock(serviceProviderId, rowCount, string.Empty);

        // Assert
        Assert.AreEqual(expectedNoRowsCount, result.Count);
        Assert.IsInstanceOfType(result, typeof(List<CohortDistributionParticipant>));
    }

    [TestMethod]
    public void GetCohortDistributionParticipantsMock_ShouldReturnEmptyList_WhenFileDoesNotExist()
    {
        // Arrange
        var serviceProviderId = (int)ServiceProvider.BsSelect;
        var rowCount = 5;

        // Act
        var result = _service.GetCohortDistributionParticipantsMock(serviceProviderId, rowCount, null);

        // Assert
        Assert.AreEqual(expectedNoRowsCount, result.Count);
        Assert.IsInstanceOfType(result, typeof(List<CohortDistributionParticipant>));
    }

    [TestMethod]
    public void GetCohortDistributionParticipantsMock_ShouldReturnEmptyList_WhenJsonDeserializationFails()
    {
        // Arrange
        var serviceProviderId = (int)ServiceProvider.BsSelect;
        var rowCount = 5;
        var incorrectJson = "{ invalid json }";

        // Act
        var result = _service.GetCohortDistributionParticipantsMock(serviceProviderId, rowCount, incorrectJson);

        // Assert
        Assert.AreEqual(expectedNoRowsCount, result.Count);
        Assert.IsInstanceOfType(result, typeof(List<CohortDistributionParticipant>));
    }

    [TestMethod]
    public void GetCohortDistributionParticipantsMock_ShouldReturnEmptyList_WhenNoParticipantsMatch()
    {
        // Arrange
        var serviceProviderId = 3;
        var rowCount = 1000;

        // Act
        var result = _service.GetCohortDistributionParticipantsMock(serviceProviderId, rowCount, _json);

        // Assert
        Assert.AreEqual(expectedNoRowsCount, result.Count);
        Assert.IsInstanceOfType(result, typeof(List<CohortDistributionParticipant>));
    }
}
