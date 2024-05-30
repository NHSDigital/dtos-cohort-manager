namespace NHS.CohortManger.Tests.ScreeningDataServicesTests;

using System.Data;
using Data.Database;
using Microsoft.Extensions.Logging;
using Moq;

[TestClass]
public class UpdateParticipantDetailsTests
{
    private readonly Mock<IDbConnection> mockDBConnection;
    private readonly Model.Participant participant;
    private readonly Mock<IDbCommand> commandMock;
    private readonly Mock<IDataReader> moqDataReader;
    private readonly Mock<ILogger<UpdateParticipantData>> _loggerMock;
    private readonly Mock<IDatabaseHelper> _databaseHelperMock;
    private readonly Mock<IDbDataParameter> mockParameter;
    private readonly Mock<IDbTransaction> mockTransaction;

    public UpdateParticipantDetailsTests()
    {
        Environment.SetEnvironmentVariable("DtOsDatabaseConnectionString", "DtOsDatabaseConnectionString");

        mockTransaction = new Mock<IDbTransaction>();
        mockParameter = new Mock<IDbDataParameter>();
        mockDBConnection = new Mock<IDbConnection>();

        mockDBConnection.Setup(x => x.ConnectionString).Returns("someFakeCOnnectionString");
        mockDBConnection.Setup(x => x.BeginTransaction()).Returns(mockTransaction.Object);

        commandMock = new Mock<IDbCommand>();
        commandMock.Setup(c => c.Dispose());

        moqDataReader = new Mock<IDataReader>();

        commandMock.SetupSequence(m => m.Parameters.Add(It.IsAny<IDbDataParameter>()));

        commandMock.Setup(m => m.Parameters.Clear()).Verifiable();
        commandMock.SetupProperty<System.Data.CommandType>(c => c.CommandType);
        commandMock.SetupProperty<string>(c => c.CommandText);


        mockParameter.Setup(m => m.ParameterName).Returns("@fakeparam");
        mockParameter.Setup(m => m.Value).Returns("fakeValue");

        commandMock.Setup(x => x.CreateParameter()).Returns(mockParameter.Object);

        // Setup the IdbConnection Mock with the mocked command
        mockDBConnection.Setup(m => m.CreateCommand()).Returns(commandMock.Object);
        commandMock.Setup(m => m.Parameters.Add(It.IsAny<IDbDataParameter>())).Verifiable();
        commandMock.Setup(m => m.ExecuteReader())
        .Returns(moqDataReader.Object);
        mockDBConnection.Setup(conn => conn.Open());


        _loggerMock = new Mock<ILogger<UpdateParticipantData>>();
        _databaseHelperMock = new Mock<IDatabaseHelper>();
        participant = GetParticipant();
    }

    [TestMethod]
    public void UpdateParticipantDetails_Success()
    {
        // Arrange
        moqDataReader.SetupSequence(reader => reader.Read())
            .Returns(true)
            .Returns(false)
            .Returns(true)
            .Returns(false);
        moqDataReader.Setup(reader => reader.GetInt32(0)).Returns(1); // Return expected id
        commandMock.Setup(x => x.ExecuteNonQuery()).Returns(1);

        _databaseHelperMock.Setup(helper => helper.ConvertNullToDbNull(It.IsAny<string>())).Returns(DBNull.Value);
        _databaseHelperMock.Setup(helper => helper.parseDates(It.IsAny<string>())).Returns(DateTime.Today);

        var participantData = GetParticipant();
        var updateParticipantData = new UpdateParticipantData(mockDBConnection.Object, _databaseHelperMock.Object, _loggerMock.Object);

        // Act
        var result = updateParticipantData.UpdateParticipantDetails(participantData);

        // Assert
        Assert.IsTrue(result);

    }

    [TestMethod]
    public void UpdateParticipantDetails_FailsToGetOldId_False()
    {
        // Arrange
        moqDataReader.SetupSequence(reader => reader.Read())
            .Returns(false);
        moqDataReader.Setup(reader => reader.GetInt32(0)).Returns(1); // Return expected id
        commandMock.Setup(x => x.ExecuteNonQuery()).Returns(1);

        _databaseHelperMock.Setup(helper => helper.ConvertNullToDbNull(It.IsAny<string>())).Returns(DBNull.Value);
        _databaseHelperMock.Setup(helper => helper.parseDates(It.IsAny<string>())).Returns(DateTime.Today);

        var participantData = GetParticipant();
        var updateParticipantData = new UpdateParticipantData(mockDBConnection.Object, _databaseHelperMock.Object, _loggerMock.Object);

        // Act
        var result = updateParticipantData.UpdateParticipantDetails(participantData);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void UpdateParticipantAsEligible_UpdatesRecords_True()
    {
        // Arrange
        moqDataReader.SetupSequence(reader => reader.Read())
            .Returns(true)
            .Returns(false);
        moqDataReader.Setup(reader => reader.GetInt32(0)).Returns(1);
        commandMock.Setup(x => x.ExecuteNonQuery()).Returns(1);

        var participantData = GetParticipant();
        var updateParticipantData = new UpdateParticipantData(mockDBConnection.Object, _databaseHelperMock.Object, _loggerMock.Object);

        // Act
        var result = updateParticipantData.UpdateParticipantAsEligible(participant, 'Y');

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void UpdateParticipantAsEligible_DoesNetGetOldId_False()
    {
        // Arrange
        moqDataReader.SetupSequence(reader => reader.Read())
            .Returns(false)
            .Returns(false);
        moqDataReader.Setup(reader => reader.GetInt32(0)).Returns(1);
        commandMock.Setup(x => x.ExecuteNonQuery()).Returns(0);

        var participantData = GetParticipant();
        var updateParticipantData = new UpdateParticipantData(mockDBConnection.Object, _databaseHelperMock.Object, _loggerMock.Object);

        // Act
        var result = updateParticipantData.UpdateParticipantAsEligible(participant, 'Y');

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void GetParticipantId_SuccessfulQueryExecution_ReturnsParticipantId()
    {
        // Arrange
        var nhsId = "123456";
        var expectedParticipantId = 123;
        moqDataReader.SetupSequence(reader => reader.Read())
            .Returns(true)
            .Returns(false);
        moqDataReader.Setup(reader => reader.GetInt32(0)).Returns(expectedParticipantId);
        commandMock.Setup(x => x.ExecuteNonQuery()).Returns(1);


        // Mock ExecuteQuery and CreateCommand
        var updateParticipantData = new UpdateParticipantData(mockDBConnection.Object, _databaseHelperMock.Object, _loggerMock.Object);

        // Create an instance of the class under test
        // Act
        var result = updateParticipantData.GetParticipantId(nhsId);

        // Assert
        Assert.AreEqual(expectedParticipantId, result);
    }

    [TestMethod]
    public void GetParticipantId_EmptyResultSet_ReturnsDefaultParticipantId()
    {
        // Arrange
        var nhsId = "123456";

        moqDataReader.SetupSequence(reader => reader.Read())
            .Returns(false);
        moqDataReader.Setup(reader => reader.GetInt32(0)).Returns(0);
        commandMock.Setup(x => x.ExecuteNonQuery()).Returns(1);


        // Mock ExecuteQuery and CreateCommand
        var updateParticipantData = new UpdateParticipantData(mockDBConnection.Object, _databaseHelperMock.Object, _loggerMock.Object);

        // Create an instance of the class under test
        // Act
        var result = updateParticipantData.GetParticipantId(nhsId);

        // Assert
        Assert.AreEqual(0, result);
    }

    private Model.Participant GetParticipant()
    {
        return new Model.Participant()
        {
            NHSId = "123456",
            SupersededByNhsNumber = "789012",
            PrimaryCareProvider = "ABC Clinic",
            GpConnect = "GP Connect ID",
            NamePrefix = "Mr.",
            FirstName = "John",
            OtherGivenNames = "Middle",
            Surname = "Doe",
            DateOfBirth = "1990-01-01",
            Gender = "Male",
            AddressLine1 = "123 Main Street",
            AddressLine2 = "Apt 101",
            AddressLine3 = "Suburb",
            AddressLine4 = "City",
            AddressLine5 = "State",
            Postcode = "12345",
            ReasonForRemoval = "Moved",
            ReasonForRemovalEffectiveFromDate = "2023-01-01",
            DateOfDeath = "2024-04-23",
            TelephoneNumber = "123-456-7890",
            MobileNumber = "987-654-3210",
            EmailAddress = "john.doe@example.com",
            PreferredLanguage = "English",
            IsInterpreterRequired = "No",
            Action = "Update"
        };
    }
}
