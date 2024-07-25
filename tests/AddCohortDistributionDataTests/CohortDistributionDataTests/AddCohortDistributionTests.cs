namespace NHS.CohortManager.Tests.AddCohortDistributionDataTests;

using System.Data;
using Data.Database;
using Microsoft.Extensions.Logging;
using Model;
using Moq;


[TestClass]
public class AddCohortDistributionTests
{
    private readonly Mock<IDbConnection> _mockDBConnection = new();
    private readonly Mock<IDbCommand> _commandMock = new();
    private readonly Mock<IDataReader> _mockDataReader = new();
    private readonly Mock<ILogger<CreateCohortDistributionData>> _loggerMock = new();
    private readonly Mock<IDatabaseHelper> _databaseHelperMock = new();
    private readonly Mock<IDbDataParameter> _mockParameter = new();
    private readonly Mock<IDbTransaction> _mockTransaction = new();

    public AddCohortDistributionTests()
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
    public void InsertCohortDistributionData_Success()
    {
        // Arrange
        var createCohortDistributionData = new CreateCohortDistributionData(
                _mockDBConnection.Object,
                _databaseHelperMock.Object,
                _loggerMock.Object
            );

        var cohortDistributionParticipant = new CohortDistributionParticipant();
        _commandMock.Setup(x => x.ExecuteNonQuery()).Returns(1);

        // Act
        var result = createCohortDistributionData.InsertCohortDistributionData(cohortDistributionParticipant);

        // Assert
        Assert.IsTrue(result);
        _commandMock.Verify(m => m.ExecuteNonQuery(), Times.Once);
    }

    [TestMethod]
    public void InsertCohortDistributionData_FailureDueToExecution()
    {
        // Arrange
        var createCohortDistributionData = new CreateCohortDistributionData(
                _mockDBConnection.Object,
                _databaseHelperMock.Object,
                _loggerMock.Object
            );

        var cohortDistributionParticipant = new CohortDistributionParticipant();
        _commandMock.Setup(x => x.ExecuteNonQuery()).Returns(0);

        // Act
        var result = createCohortDistributionData.InsertCohortDistributionData(cohortDistributionParticipant);

        // Assert
        Assert.IsFalse(result);
        _commandMock.Verify(m => m.ExecuteNonQuery(), Times.Once);
        _mockTransaction.Verify(t => t.Rollback(), Times.Once);
    }

    [TestMethod]
    public void GetParticipant_ReturnsListOfParticipants()
    {
        // Arrange
        var createCohortDistributionData = new CreateCohortDistributionData(
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
        var result = createCohortDistributionData.ExtractCohortDistributionParticipants();

        // Assert
        Assert.AreEqual("1", result.FirstOrDefault().ParticipantId);
        Assert.AreEqual(1, result.Count());
    }

    [TestMethod]
    public void GetParticipant_MarksParticipantsAsExtracted()
    {
        // Arrange
        var createCohortDistributionData = new CreateCohortDistributionData(
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
        var result = createCohortDistributionData.ExtractCohortDistributionParticipants();

        // Assert
        _commandMock.Verify(x => x.ExecuteNonQuery(), Times.AtLeastOnce());
        Assert.AreEqual("1", result.FirstOrDefault().ParticipantId);
    }

    [TestMethod]
    public void GetParticipant_ReturnsEmptyCollection_WhenNoParticipants()
    {
        // Arrange
        var createCohortDistributionData = new CreateCohortDistributionData(
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
        var result = createCohortDistributionData.ExtractCohortDistributionParticipants();

        // Assert
        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(Array.Empty<CreateCohortDistributionData>(), result);
    }

    private void SetUpReader()
    {
        _mockDataReader.Setup(reader => reader["PARTICIPANT_ID"]).Returns(() => 1);
        _mockDataReader.Setup(reader => reader["NHS_NUMBER"]).Returns(() => 123456);
        _mockDataReader.Setup(reader => reader["SUPERSEDED_NHS_NUMBER"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["PRIMARY_CARE_PROVIDER"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["PRIMARY_CARE_PROVIDER_FROM_DT"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["NAME_PREFIX"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["GIVEN_NAME"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["OTHER_GIVEN_NAME"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["FAMILY_NAME"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["PREVIOUS_FAMILY_NAME"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["DATE_OF_BIRTH"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["GENDER"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["ADDRESS_LINE_1"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["ADDRESS_LINE_2"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["ADDRESS_LINE_3"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["ADDRESS_LINE_4"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["ADDRESS_LINE_5"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["POST_CODE"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["USUAL_ADDRESS_FROM_DT"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["DATE_OF_DEATH"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["TELEPHONE_NUMBER_HOME"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["TELEPHONE_NUMBER_HOME_FROM_DT"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["TELEPHONE_NUMBER_MOB"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["TELEPHONE_NUMBER_MOB_FROM_DT"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["EMAIL_ADDRESS_HOME"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["EMAIL_ADDRESS_HOME_FROM_DT"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["PREFERRED_LANGUAGE"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["INTERPRETER_REQUIRED"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["REASON_FOR_REMOVAL"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["REASON_FOR_REMOVAL_DT"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["RECORD_INSERT_DATETIME"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["RECORD_UPDATE_DATETIME"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["IS_EXTRACTED"]).Returns(() => 0);
    }
}
