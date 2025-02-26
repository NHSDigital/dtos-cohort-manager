namespace NHS.CohortManager.Tests.UnitTests.AddCohortDistributionDataTests;

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
public class AddCohortDistributionTests
{
    private readonly Mock<IDbConnection> _mockDBConnection = new();
    private readonly Mock<IDbCommand> _commandMock = new();
    private readonly Mock<IDataReader> _mockDataReader = new();
    private readonly Mock<ILogger<CreateCohortDistributionData>> _loggerMock = new();
    private readonly Mock<IDatabaseHelper> _databaseHelperMock = new();
    private readonly Mock<IDbTransaction> _mockTransaction = new();
    private readonly Mock<IDataServiceClient<CohortDistribution>> _cohortDistributionMock = new();

    public AddCohortDistributionTests()
    {
        _mockDBConnection.Setup(x => x.ConnectionString).Returns("someFakeConnectionString");
        _mockDBConnection.Setup(x => x.BeginTransaction()).Returns(_mockTransaction.Object);
        _mockDBConnection.Setup(m => m.CreateCommand()).Returns(_commandMock.Object);
        _commandMock.Setup(m => m.CreateParameter()).Returns(new Mock<IDbDataParameter>().Object);
        _commandMock.Setup(m => m.Parameters).Returns(new Mock<IDataParameterCollection>().Object);
    }

    [TestMethod]
    public void ExtractCohortDistributionParticipants_ValidRequest_ReturnsListOfParticipants()
    {
        var createCohortDistributionData = new CreateCohortDistributionData(
            _mockDBConnection.Object,
            _loggerMock.Object,
            _cohortDistributionMock.Object
        );

        _mockDBConnection.Setup(m => m.CreateCommand()).Returns(_commandMock.Object);
        _commandMock.Setup(m => m.CreateParameter()).Returns(new Mock<IDbDataParameter>().Object);
        _commandMock.Setup(m => m.Parameters).Returns(new Mock<IDataParameterCollection>().Object);
        _commandMock.Setup(m => m.ExecuteNonQuery()).Returns(1);
        _commandMock.Setup(m => m.ExecuteReader()).Returns(_mockDataReader.Object);

        // ✅ Mock DataReader to return expected values
        _mockDataReader.SetupSequence(reader => reader.Read())
            .Returns(true)  // Simulating a row exists
            .Returns(false); // Simulating end of data

        _mockDataReader.Setup(reader => reader["PARTICIPANT_ID"]).Returns("12345");
        _mockDataReader.Setup(reader => reader["NHS_NUMBER"]).Returns("987654321");

        var rowCount = 1;
        var result = createCohortDistributionData.GetUnextractedCohortDistributionParticipants(rowCount);
        Assert.IsNotNull(result);
    }
}

