namespace NHS.CohortManager.Tests.AddAggregationDataTests;

using System.Data;
using System.Diagnostics.CodeAnalysis;
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
    private readonly Mock<IDbCommand> _commandMock = new();
    private readonly Mock<IDataReader> _mockDataReader = new();
    private readonly Mock<ILogger<CreateAggregationData>> _loggerMock = new();
    private readonly Mock<IDatabaseHelper> _databaseHelperMock = new();
    private readonly Mock<IDbDataParameter> _mockParameter = new();
    private readonly Mock<IDbTransaction> _mockTransaction = new();

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
        .Returns(_mockDataReader.Object);
        _mockDBConnection.Setup(conn => conn.Open());

        _databaseHelperMock.Setup(helper => helper.ConvertNullToDbNull(It.IsAny<string>())).Returns(DBNull.Value);
        _databaseHelperMock.Setup(helper => helper.ParseDates(It.IsAny<string>())).Returns(DateTime.Today);
    }

    [TestMethod]
    public void InsertAggregationData_Success()
    {
        // Arrange
        var createAggregationData = new CreateAggregationData(
                _mockDBConnection.Object,
                _databaseHelperMock.Object,
                _loggerMock.Object
            );

        var aggregateParticipant = new AggregateParticipant();
        _commandMock.Setup(x => x.ExecuteNonQuery()).Returns(1);

        // Act
        var result = createAggregationData.InsertAggregationData(aggregateParticipant);

        // Assert
        Assert.IsTrue(result);
        _commandMock.Verify(m => m.ExecuteNonQuery(), Times.Once);
    }

    [TestMethod]
    public void InsertAggregationData_FailureDueToExecution()
    {
        // Arrange
        var createAggregationData = new CreateAggregationData(
                _mockDBConnection.Object,
                _databaseHelperMock.Object,
                _loggerMock.Object
            );

        var aggregateParticipant = new AggregateParticipant();
        _commandMock.Setup(x => x.ExecuteNonQuery()).Returns(0);

        // Act
        var result = createAggregationData.InsertAggregationData(aggregateParticipant);

        // Assert
        Assert.IsFalse(result);
        _commandMock.Verify(m => m.ExecuteNonQuery(), Times.Once);
        _mockTransaction.Verify(t => t.Rollback(), Times.Once);
    }

    [TestMethod]
    public void GetParticipant_ReturnsListOfParticipants()
    {
        // Arrange
        var createAggregationData = new CreateAggregationData(
                _mockDBConnection.Object,
                _databaseHelperMock.Object,
                _loggerMock.Object
            );

        _mockDataReader.SetupSequence(reader => reader.Read())
        .Returns(true)
        .Returns(false);
        _commandMock.Setup(x => x.ExecuteNonQuery()).Returns(1);

        SetUpReader();

        // Act
        var res = createAggregationData.ExtractAggregateParticipants();

        // Assert
        Assert.AreEqual("1", res.FirstOrDefault().AggregateId);
        Assert.AreEqual(1, res.Count());
    }

    [TestMethod]
    public void GetParticipant_MarksParticipantsAsExtracted()
    {
        // Arrange
        var createAggregationData = new CreateAggregationData(
                _mockDBConnection.Object,
                _databaseHelperMock.Object,
                _loggerMock.Object
            );

        _mockDataReader.SetupSequence(reader => reader.Read())
        .Returns(true)
        .Returns(false);
        _commandMock.Setup(x => x.ExecuteNonQuery()).Returns(1);

        SetUpReader();

        // Act
        var res = createAggregationData.ExtractAggregateParticipants();

        // Assert
        _commandMock.Verify(x => x.ExecuteNonQuery(), Times.AtLeastOnce());
        Assert.AreEqual("1", res.FirstOrDefault().AggregateId);
    }

    [TestMethod]
    public void GetParticipant_ReturnsNull_WhenNoParticipants()
    {
        // Arrange
        var createAggregationData = new CreateAggregationData(
                _mockDBConnection.Object,
                _databaseHelperMock.Object,
                _loggerMock.Object
            );

        _mockDataReader.SetupSequence(reader => reader.Read())
        .Returns(true)
        .Returns(false);
        _commandMock.Setup(x => x.ExecuteNonQuery()).Returns(1);
        _mockDataReader.Setup(x => x.Read()).Returns(false);

        // Act
        var res = createAggregationData.ExtractAggregateParticipants();

        // Assert

        Assert.IsNull(res);
    }


    private void SetUpReader()
    {
        _mockDataReader.Setup(reader => reader["AGGREGATION_ID"]).Returns(() => "1");
        _mockDataReader.Setup(reader => reader["NHS_NUMBER"]).Returns(() => "123456");
        _mockDataReader.Setup(reader => reader["SUPERSEDED_BY_NHS_NUMBER"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["PRIMARY_CARE_PROVIDER"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["PARTICIPANT_PREFIX"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["PARTICIPANT_FIRST_NAME"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["OTHER_NAME"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["PARTICIPANT_LAST_NAME"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["PARTICIPANT_BIRTH_DATE"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["PARTICIPANT_GENDER"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["REASON_FOR_REMOVAL_CD"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["REMOVAL_DATE"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["PARTICIPANT_DEATH_DATE"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["EXTRACTED"]).Returns(0);
    }
}
