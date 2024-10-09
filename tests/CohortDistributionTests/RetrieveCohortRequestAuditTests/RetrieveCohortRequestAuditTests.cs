namespace NHS.CohortManager.Tests.RetrieveCohortRequestAuditTests;

using Model;

[TestClass]
public class RetrieveCohortRequestAuditTests : CohortDistributionDataBase
{

    [TestMethod]
    public void GetCohortRequestAudit_WithValidParameters_ReturnsExpectedResult()
    {
        // Arrange
        string requestId = "testRequestId";
        string statusCode = "testStatusCode";
        DateTime dateFrom = DateTime.Now.AddDays(-1);

        _mockDataReader.Setup(reader => reader.Read()).Returns(true);
        SetUpReader();

        // Act
        var result = _createCohortDistributionDataService.GetCohortRequestAudit(requestId, statusCode, dateFrom);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("testRequestId", result[0].RequestId);
        Assert.AreEqual("testStatusCode", result[0].StatusCode);
        Assert.IsInstanceOfType(result, typeof(List<CohortRequestAudit>));
    }

    [TestMethod]
    public void GetCohortRequestAudit_WithNullParameters_ReturnsExpectedResult()
    {
        // Arrange
        string? requestId = null;
        string? statusCode = null;
        DateTime? dateFrom = null;

        _mockDataReader.Setup(reader => reader.Read()).Returns(false);

        // Act
        var result = _createCohortDistributionDataService.GetCohortRequestAudit(requestId, statusCode, dateFrom);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
        Assert.IsInstanceOfType(result, typeof(List<CohortRequestAudit>));
    }
    private void SetUpReader()
    {
        _mockDataReader.Setup(reader => reader.Read()).Returns(true);
        _mockDataReader.Setup(reader => reader["REQUEST_ID"]).Returns("testRequestId");
        _mockDataReader.Setup(reader => reader["STATUS_CODE"]).Returns("200");
        _mockDataReader.Setup(reader => reader["CREATED_DATETIME"]).Returns(DateTime.Now.ToString());
    }
}
