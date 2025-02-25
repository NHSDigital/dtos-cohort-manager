namespace NHS.CohortManager.Tests.UnitTests.RetrieveCohortRequestAuditTests;

using System.Data;
using Data.Database;
using Microsoft.Extensions.Logging;
using Moq;
using DataServices.Client;
using Model;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using NHS.CohortManager.Tests.UnitTests.CohortDistributionTests.RetrieveCohortRequestAuditTests;

[TestClass]
public class RetrieveCohortRequestAuditTests : CohortDistributionDataBase
{
    public RetrieveCohortRequestAuditTests()
    {
        _mockDBConnection.Setup(m => m.CreateCommand()).Returns(_commandMock.Object);
        _commandMock.Setup(m => m.CreateParameter()).Returns(new Mock<IDbDataParameter>().Object);
        _commandMock.Setup(m => m.Parameters).Returns(new Mock<IDataParameterCollection>().Object);
        _commandMock.Setup(m => m.ExecuteReader()).Returns(_mockDataReader.Object);
    }

    [TestMethod]
    public async Task GetCohortRequestAudit_WithAllParameters_ReturnsValidCohortRequestAudit()
    {
        // Arrange
        string requestId = "testRequestId";
        string statusCode = "200";
        DateTime dateFrom = DateTime.Now.AddDays(-1);

        SetUpReader();

        // Act
        var result = await _createCohortDistributionDataService.GetCohortRequestAudit(requestId, statusCode, dateFrom);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("testRequestId", result[0].RequestId);
        Assert.AreEqual("200", result[0].StatusCode);
        Assert.IsInstanceOfType(result, typeof(List<CohortRequestAudit>));
        Assert.IsTrue(dateFrom <= DateTime.Parse(result[0].CreatedDateTime));
    }

    [TestMethod]
    public async Task GetCohortRequestAudit_WithNullParameters_ReturnsValidCohortRequestAudit()
    {
        // Arrange
        string? requestId = null;
        string? statusCode = null;
        DateTime? dateFrom = null;

        SetUpReader();

        // Act
        var result = await _createCohortDistributionDataService.GetCohortRequestAudit(requestId, statusCode, dateFrom);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.IsInstanceOfType(result, typeof(List<CohortRequestAudit>));
    }

    [TestMethod]
    public async Task GetCohortRequestAudit_WithFutureDateFrom_ReturnsEmptyList()
    {
        // Arrange
        string requestId = "testRequestId";
        string statusCode = "testStatusCode";
        DateTime dateFrom = DateTime.Now.AddDays(1);

        _mockDataReader.Setup(reader => reader.Read()).Returns(false);

        // Act
        var result = await _createCohortDistributionDataService.GetCohortRequestAudit(requestId, statusCode, dateFrom);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
        Assert.IsInstanceOfType(result, typeof(List<CohortRequestAudit>));
    }

    private void SetUpReader()
    {
        _mockDataReader.SetupSequence(reader => reader.Read()).Returns(true).Returns(false);
        _mockDataReader.Setup(reader => reader["REQUEST_ID"]).Returns("testRequestId");
        _mockDataReader.Setup(reader => reader["STATUS_CODE"]).Returns("200");
        _mockDataReader.Setup(reader => reader["CREATED_DATETIME"]).Returns(DateTime.Now.ToString());
    }
}

