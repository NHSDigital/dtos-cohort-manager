namespace NHS.CohortManager.Tests.UnitTests.RemoveCohortDistributionDataTests;

using System.Data;
using Data.Database;
using Microsoft.Extensions.Logging;
using Moq;
using DataServices.Client;
using Model;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;


[TestClass]
public class RemoveCohortDistributionTests
{
    private readonly Mock<IDbConnection> _mockDBConnection = new();
    private readonly Mock<IDbCommand> _commandMock = new();
    private readonly Mock<IDataReader> _mockDataReader = new();
    private readonly Mock<ILogger<CreateCohortDistributionData>> _loggerMock = new();
    private readonly Mock<IDatabaseHelper> _databaseHelperMock = new();
    private readonly Mock<IDbTransaction> _mockTransaction = new();
    private readonly Mock<IDataServiceClient<CohortDistribution>> _cohortDistributionMock = new();

    public RemoveCohortDistributionTests()
    {
        _mockDBConnection.Setup(x => x.ConnectionString).Returns("someFakeConnectionString");
        _mockDBConnection.Setup(x => x.BeginTransaction()).Returns(_mockTransaction.Object);
        _mockDBConnection.Setup(m => m.CreateCommand()).Returns(_commandMock.Object);
        _commandMock.Setup(m => m.CreateParameter()).Returns(new Mock<IDbDataParameter>().Object);
        _commandMock.Setup(m => m.ExecuteNonQuery()).Returns(1);
        _commandMock.Setup(m => m.CommandText).Returns("UPDATE Cohort SET Status='Inactive' WHERE NHSID=@NHSID");
    }

    [TestMethod]
    public void UpdateCohortDistributionParticipantAsInactive_Success()
    {
        // Arrange
        var updateCohortDistributionData = new CreateCohortDistributionData(
            _mockDBConnection.Object,
            _loggerMock.Object,
            _cohortDistributionMock.Object
        );

        string NHSID = "123456";

        _commandMock.Setup(m => m.ExecuteNonQuery()).Returns(1);
        _commandMock.Setup(m => m.CommandText).Returns("UPDATE Cohort SET Status='Inactive' WHERE NHSID=@NHSID");
        _commandMock.Setup(m => m.Parameters).Returns(new Mock<IDataParameterCollection>().Object);

        // Act
        var result = updateCohortDistributionData.UpdateCohortParticipantAsInactive(NHSID);

        // Assert
        Assert.IsTrue(result);
        _commandMock.Verify(m => m.ExecuteNonQuery(), Times.Once);
    }
}
