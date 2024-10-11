namespace NHS.CohortManager.Tests.RetrieveCohortRequestAuditTests;

using Model;

[TestClass]
public class RetrieveCohortRequestAuditTests : CohortDistributionDataBase
{

    [TestMethod]
    public async Task GetCohortRequestAudit_WithAllParameters_ReturnsValidCohortRequestAudit()
    {
        // Arrange
        string requestId = "testRequestId";
        string statusCode = "testStatusCode";
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
    public async Task GetCohortRequestAudit_WithEmptyRequestId_ReturnsValidCohortRequestAudit()
    {
        // Arrange
        string requestId = "";
        string statusCode = "testStatusCode";
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
    public async Task GetCohortRequestAudit_WithEmptyStatusCode_ReturnsValidCohortRequestAudit()
    {
        // Arrange
        string requestId = "testRequestId";
        string statusCode = "";
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

    [TestMethod]
    public async Task GetCohortRequestAudit_WithNoMatchingRecords_ReturnsEmptyList()
    {
        // Arrange
        string requestId = "nonExistentRequestId";
        string statusCode = "nonExistentStatusCode";
        DateTime dateFrom = DateTime.Now.AddDays(-1);

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
        _mockDataReader.Setup(reader => reader["REQUEST_ID"]).Returns("testRequestId");
        _mockDataReader.Setup(reader => reader["STATUS_CODE"]).Returns("200");
        _mockDataReader.Setup(reader => reader["CREATED_DATETIME"]).Returns(DateTime.Now.ToString());
    }
}
