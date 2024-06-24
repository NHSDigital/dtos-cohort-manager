namespace AggregationDataTests;

using System.Data;
using System.Net;
using Common;
using Data.Database;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;
using Moq;


[TestClass]
public class AddAggregationTests
{

    private readonly Mock<IDbConnection> _mockDBConnection = new();
    private readonly ParticipantCsvRecord _participantCsvRecord;
    private readonly Mock<IDbCommand> _commandMock = new();
    private readonly Mock<IDataReader> _moqDataReader = new();
    private readonly Mock<ILogger<CreateAggregationData>> _loggerMock = new();
    private readonly Mock<IDatabaseHelper> _databaseHelperMock = new();
    private readonly Mock<IDbDataParameter> _mockParameter = new();
    private readonly Mock<IDbTransaction> _mockTransaction = new();
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly Mock<HttpWebResponse> _webResponse = new();

    public AddAggregationTests()
    {
        Environment.SetEnvironmentVariable("DtOsDatabaseConnectionString", "DtOsDatabaseConnectionString");
        Environment.SetEnvironmentVariable("LookupValidationURL", "LookupValidationURL");

        _mockDBConnection.Setup(x => x.ConnectionString).Returns("someFakeCOnnectionString");
        _mockDBConnection.Setup(x => x.BeginTransaction()).Returns(_mockTransaction.Object);

        _commandMock.Setup(c => c.Dispose());
        _commandMock.SetupSequence(m => m.Parameters.Add(It.IsAny<IDbDataParameter>()));
        _commandMock.Setup(m => m.Parameters.Clear()).Verifiable();
        _commandMock.SetupProperty<System.Data.CommandType>(c => c.CommandType);
        _commandMock.SetupProperty<string>(c => c.CommandText);
        _commandMock.Setup(x => x.CreateParameter()).Returns(_mockParameter.Object);

        _mockDBConnection.Setup(m => m.CreateCommand()).Returns(_commandMock.Object);
        _commandMock.Setup(m => m.Parameters.Add(It.IsAny<IDbDataParameter>())).Verifiable();
        _commandMock.Setup(m => m.ExecuteReader())
        .Returns(_moqDataReader.Object);
        _mockDBConnection.Setup(conn => conn.Open());

        _databaseHelperMock.Setup(helper => helper.ConvertNullToDbNull(It.IsAny<string>())).Returns(DBNull.Value);
        _databaseHelperMock.Setup(helper => helper.ParseDates(It.IsAny<string>())).Returns(DateTime.Today);
    }

    [TestMethod]
    public void InsertAggregationData_Success()
    {
        var createAggregationData = new CreateAggregationData(
                _mockDBConnection.Object,
                _databaseHelperMock.Object,
                _loggerMock.Object,
                _callFunction.Object
            );
        // Arrange
        var aggregateParticipant = new AggregateParticipant();
        _commandMock.Setup(x => x.ExecuteNonQuery()).Returns(1);

        var result = createAggregationData.InsertAggregationData(aggregateParticipant);

        Assert.IsTrue(result);
        _commandMock.Verify(m => m.ExecuteNonQuery(), Times.Once);
    }

    [TestMethod]
    public void InsertAggregationData_FailureDueToExecution()
    {
        var createAggregationData = new CreateAggregationData(
                _mockDBConnection.Object,
                _databaseHelperMock.Object,
                _loggerMock.Object,
                _callFunction.Object
            );
        // Arrange
        var aggregateParticipant = new AggregateParticipant();
        _commandMock.Setup(x => x.ExecuteNonQuery()).Returns(0);

        var result = createAggregationData.InsertAggregationData(aggregateParticipant);

        Assert.IsFalse(result);
        _commandMock.Verify(m => m.ExecuteNonQuery(), Times.Once);
        _mockTransaction.Verify(t => t.Rollback(), Times.Once);
    }
}
