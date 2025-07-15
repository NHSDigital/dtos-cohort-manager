namespace NHS.CohortManager.Tests.UnitTests.RetrieveCohortRequestAuditTests;

using System.Linq.Expressions;
using Data.Database;
using DataServices.Client;
using Model;
using Moq;
[TestClass]
public class RetrieveCohortRequestAuditTests
{

    private readonly CreateCohortDistributionData _createCohortDistributionData;
    private readonly Mock<IDataServiceClient<CohortDistribution>> _cohortDistributionDataServiceClient = new();
    private readonly Mock<IDataServiceClient<BsSelectRequestAudit>> _bsSelectRequestAuditDataServiceClient = new();

    public RetrieveCohortRequestAuditTests()
    {
        _createCohortDistributionData = new CreateCohortDistributionData(_cohortDistributionDataServiceClient.Object, _bsSelectRequestAuditDataServiceClient.Object);
    }


    [TestMethod]
    public async Task GetCohortRequestAudit_WithAllParameters_ReturnsValidCohortRequestAudit()
    {
        // Arrange
        Guid requestId = new Guid();
        string statusCode = "testStatusCode";
        DateTime dateFrom = DateTime.UtcNow.AddDays(-1);

        _bsSelectRequestAuditDataServiceClient.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<BsSelectRequestAudit, bool>>>())).ReturnsAsync(new List<BsSelectRequestAudit>()
        {
            new BsSelectRequestAudit ()
            {
                RequestId = requestId,
                StatusCode = statusCode,
                CreatedDateTime = dateFrom.AddDays(1)
            }
        });
        // Act
        var result = await _createCohortDistributionData.GetCohortRequestAudit(requestId.ToString(), statusCode, dateFrom);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(requestId.ToString(), result[0].RequestId.ToString());
        Assert.AreEqual("testStatusCode", result[0].StatusCode);
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

        _bsSelectRequestAuditDataServiceClient.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<BsSelectRequestAudit, bool>>>())).ReturnsAsync(new List<BsSelectRequestAudit>()
        {
            new BsSelectRequestAudit ()
            {
                RequestId = new Guid(),
                StatusCode = "",
                CreatedDateTime = DateTime.UtcNow.Date
            }
        });

        // Act

        var result = await _createCohortDistributionData.GetCohortRequestAudit(requestId, statusCode, dateFrom);

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
        DateTime dateFrom = DateTime.UtcNow.AddDays(-1);

        _bsSelectRequestAuditDataServiceClient.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<BsSelectRequestAudit, bool>>>())).ReturnsAsync(new List<BsSelectRequestAudit>()
        {
            new BsSelectRequestAudit ()
            {
                RequestId = new Guid(),
                StatusCode = "200",
                CreatedDateTime = DateTime.UtcNow.Date.AddDays(1)
            }
        });

        // Act
        var result = await _createCohortDistributionData.GetCohortRequestAudit(requestId, statusCode, dateFrom);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(new Guid().ToString(), result[0].RequestId.ToString());
        Assert.AreEqual("200", result[0].StatusCode);
        Assert.IsInstanceOfType(result, typeof(List<CohortRequestAudit>));
        Assert.IsTrue(dateFrom <= DateTime.Parse(result[0].CreatedDateTime));
    }

    [TestMethod]
    public async Task GetCohortRequestAudit_WithEmptyStatusCode_ReturnsValidCohortRequestAudit()
    {
        // Arrange
        string requestId = new Guid().ToString();
        string statusCode = "";
        DateTime dateFrom = DateTime.UtcNow.AddDays(-1);

        _bsSelectRequestAuditDataServiceClient.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<BsSelectRequestAudit, bool>>>())).ReturnsAsync(new List<BsSelectRequestAudit>()
        {
            new BsSelectRequestAudit ()
            {
                RequestId = new Guid(),
                StatusCode = "200",
                CreatedDateTime = DateTime.UtcNow.Date.AddDays(1)
            }
        });

        // Act
        var result = await _createCohortDistributionData.GetCohortRequestAudit(requestId, statusCode, dateFrom);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(new Guid().ToString(), result[0].RequestId.ToString());
        Assert.AreEqual("200", result[0].StatusCode);
        Assert.IsInstanceOfType(result, typeof(List<CohortRequestAudit>));
        Assert.IsTrue(dateFrom <= DateTime.Parse(result[0].CreatedDateTime));
    }

    [TestMethod]
    public async Task GetCohortRequestAudit_WithFutureDateFrom_ReturnsEmptyList()
    {
        // Arrange
        string requestId = new Guid().ToString();
        string statusCode = "testStatusCode";
        DateTime dateFrom = DateTime.UtcNow.AddDays(1);

        _bsSelectRequestAuditDataServiceClient.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<BsSelectRequestAudit, bool>>>())).ReturnsAsync(new List<BsSelectRequestAudit>());

        // Act
        var result = await _createCohortDistributionData.GetCohortRequestAudit(requestId, statusCode, dateFrom);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
        Assert.IsInstanceOfType(result, typeof(List<CohortRequestAudit>));
    }

    [TestMethod]
    public async Task GetCohortRequestAudit_WithNoMatchingRecords_ReturnsEmptyList()
    {
        // Arrange
        string requestId = new Guid().ToString();
        string statusCode = "nonExistentStatusCode";
        DateTime dateFrom = DateTime.UtcNow.AddDays(-1);

        _bsSelectRequestAuditDataServiceClient.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<BsSelectRequestAudit, bool>>>())).ReturnsAsync(new List<BsSelectRequestAudit>());

        // Act
        var result = await _createCohortDistributionData.GetCohortRequestAudit(requestId, statusCode, dateFrom);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
        Assert.IsInstanceOfType(result, typeof(List<CohortRequestAudit>));
    }
}
