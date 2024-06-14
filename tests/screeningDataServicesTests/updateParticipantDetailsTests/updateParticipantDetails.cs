namespace NHS.CohortManger.Tests.ScreeningDataServices;

using System.Data;
using System.Net;
using Common;
using Data.Database;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;
using Moq;

[TestClass]
public class UpdateParticipantDetailsTests
{
    private readonly Mock<IDbConnection> mockDBConnection;
    private readonly Participant participant;
    private readonly Mock<IDbCommand> commandMock;
    private readonly Mock<IDataReader> moqDataReader;
    private readonly Mock<ILogger<UpdateParticipantData>> _loggerMock;
    private readonly Mock<IDatabaseHelper> _databaseHelperMock;
    private readonly Mock<IDbDataParameter> mockParameter;
    private readonly Mock<IDbTransaction> mockTransaction;
    private readonly Mock<ICallFunction> callFunction;
    private readonly Mock<HttpWebResponse> webResponse;

    public UpdateParticipantDetailsTests()
    {
        Environment.SetEnvironmentVariable("DtOsDatabaseConnectionString", "DtOsDatabaseConnectionString");
        Environment.SetEnvironmentVariable("LookupValidationURL", "LookupValidationURL");

        mockTransaction = new Mock<IDbTransaction>();
        mockParameter = new Mock<IDbDataParameter>();
        mockDBConnection = new Mock<IDbConnection>();
        callFunction = new Mock<ICallFunction>();
        webResponse = new Mock<HttpWebResponse>();

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

        mockDBConnection.Setup(m => m.CreateCommand()).Returns(commandMock.Object);
        commandMock.Setup(m => m.Parameters.Add(It.IsAny<IDbDataParameter>())).Verifiable();
        commandMock.Setup(m => m.ExecuteReader())
        .Returns(moqDataReader.Object);
        mockDBConnection.Setup(conn => conn.Open());

        _loggerMock = new Mock<ILogger<UpdateParticipantData>>();
        _databaseHelperMock = new Mock<IDatabaseHelper>();

        _databaseHelperMock.Setup(helper => helper.ConvertNullToDbNull(It.IsAny<string>())).Returns(DBNull.Value);
        _databaseHelperMock.Setup(helper => helper.parseDates(It.IsAny<string>())).Returns(DateTime.Today);

        participant = GetParticipant();
    }

    [TestMethod]
    public async Task UpdateParticipantDetails_Success()
    {
        // Arrange
        moqDataReader.SetupSequence(reader => reader.Read())
        .Returns(true)
        .Returns(false)
        .Returns(true)
        .Returns(false);
        moqDataReader.Setup(reader => reader.GetInt32(0)).Returns(1);

        commandMock.Setup(x => x.ExecuteNonQuery()).Returns(1);
        SetUpReader();

        webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        callFunction.Setup(x => x.SendPost(It.Is<string>(s => s == "LookupValidationURL"), It.IsAny<string>()))
        .Returns(Task.FromResult<HttpWebResponse>(webResponse.Object));

        var updateParticipantData = new UpdateParticipantData(mockDBConnection.Object, _databaseHelperMock.Object, _loggerMock.Object, callFunction.Object);

        // Act
        var result = await updateParticipantData.UpdateParticipantDetails(participant);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task UpdateParticipantDetails_FailsToGetOldId_False()
    {
        // Arrange
        moqDataReader.SetupSequence(reader => reader.Read())
        .Returns(true)
        .Returns(false);
        moqDataReader.Setup(reader => reader.GetInt32(0)).Returns(1);
        commandMock.Setup(x => x.ExecuteNonQuery()).Returns(0);

        SetUpReader();

        webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        callFunction.Setup(x => x.SendPost(It.Is<string>(s => s == "LookupValidationURL"), It.IsAny<string>()))
        .Returns(Task.FromResult<HttpWebResponse>(webResponse.Object));

        var updateParticipantData = new UpdateParticipantData(mockDBConnection.Object, _databaseHelperMock.Object, _loggerMock.Object, callFunction.Object);

        // Act
        var result = await updateParticipantData.UpdateParticipantDetails(participant);

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

        SetUpReader();

        var updateParticipantData = new UpdateParticipantData(mockDBConnection.Object, _databaseHelperMock.Object, _loggerMock.Object, callFunction.Object);

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
        .Returns(true)
        .Returns(false);
        moqDataReader.Setup(reader => reader.GetInt32(0)).Returns(1);
        commandMock.Setup(x => x.ExecuteNonQuery()).Returns(0);

        SetUpReader();

        var updateParticipantData = new UpdateParticipantData(mockDBConnection.Object, _databaseHelperMock.Object, _loggerMock.Object, callFunction.Object);

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
        var expectedParticipantId = 123456;
        moqDataReader.SetupSequence(reader => reader.Read())
        .Returns(true)
        .Returns(false);
        moqDataReader.Setup(reader => reader.GetInt32(0)).Returns(expectedParticipantId);
        commandMock.Setup(x => x.ExecuteNonQuery()).Returns(1);

        SetUpReader();

        var updateParticipantData = new UpdateParticipantData(mockDBConnection.Object, _databaseHelperMock.Object, _loggerMock.Object, callFunction.Object);

        // Act
        var result = updateParticipantData.GetParticipant(nhsId);

        // Assert
        Assert.AreEqual(nhsId, result.NHSId);
    }

    [TestMethod]
    public void GetParticipantId_EmptyResultSet_ReturnsDefaultParticipantId()
    {
        // Arrange
        var nhsId = "123456";

        moqDataReader.SetupSequence(reader => reader.Read())
            .Returns(true)
            .Returns(false);
        moqDataReader.Setup(reader => reader.GetInt32(0)).Returns(0);
        commandMock.Setup(x => x.ExecuteNonQuery()).Returns(1);

        SetUpReader();

        var updateParticipantData = new UpdateParticipantData(mockDBConnection.Object, _databaseHelperMock.Object, _loggerMock.Object, callFunction.Object);

        // Act
        var result = updateParticipantData.GetParticipant(nhsId);

        // Assert
        Assert.AreEqual("123456", result.NHSId);
    }

    [TestMethod]
    public async Task UpdateParticipantDetails_Fails_When_Validation_Fails()
    {
        // Arrange
        moqDataReader.SetupSequence(reader => reader.Read())
        .Returns(true)
        .Returns(false);

        SetUpReader();

        webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.BadRequest);

        callFunction.Setup(x => x.SendPost(It.Is<string>(s => s == "LookupValidationURL"), It.IsAny<string>()))
        .Returns(Task.FromResult<HttpWebResponse>(webResponse.Object));

        var updateParticipantData = new UpdateParticipantData(mockDBConnection.Object, _databaseHelperMock.Object, _loggerMock.Object, callFunction.Object);

        // Act
        var result = await updateParticipantData.UpdateParticipantDetails(participant);

        // Assert
        Assert.IsFalse(result);
        commandMock.Verify(command => command.ExecuteNonQuery(), Times.Never());
    }

    private void SetUpReader()
    {
        moqDataReader.Setup(m => m["PARTICIPANT_ID"]).Returns("123456");
        moqDataReader.Setup(m => m["NHS_NUMBER"]).Returns("123456");
        moqDataReader.Setup(m => m["SUPERSEDED_BY_NHS_NUMBER"]).Returns(DBNull.Value);
        moqDataReader.Setup(m => m["PRIMARY_CARE_PROVIDER"]).Returns("Some Provider");
        moqDataReader.Setup(m => m["GP_CONNECT"]).Returns(DBNull.Value);
        moqDataReader.Setup(m => m["PARTICIPANT_PREFIX"]).Returns(DBNull.Value);
        moqDataReader.Setup(m => m["PARTICIPANT_FIRST_NAME"]).Returns(DBNull.Value);
        moqDataReader.Setup(m => m["OTHER_NAME"]).Returns(DBNull.Value);
        moqDataReader.Setup(m => m["PARTICIPANT_LAST_NAME"]).Returns(DBNull.Value);
        moqDataReader.Setup(m => m["PARTICIPANT_BIRTH_DATE"]).Returns(DBNull.Value);
        moqDataReader.Setup(m => m["PARTICIPANT_GENDER"]).Returns(DBNull.Value);
        moqDataReader.Setup(m => m["REASON_FOR_REMOVAL_CD"]).Returns(DBNull.Value);
        moqDataReader.Setup(m => m["REMOVAL_DATE"]).Returns(DBNull.Value);
        moqDataReader.Setup(m => m["PARTICIPANT_DEATH_DATE"]).Returns(DBNull.Value);
        moqDataReader.Setup(m => m["ADDRESS_LINE_1"]).Returns(DBNull.Value);
        moqDataReader.Setup(m => m["ADDRESS_LINE_2"]).Returns(DBNull.Value);
        moqDataReader.Setup(m => m["CITY"]).Returns(DBNull.Value);
        moqDataReader.Setup(m => m["COUNTY"]).Returns(DBNull.Value);
        moqDataReader.Setup(m => m["POST_CODE"]).Returns(DBNull.Value);
    }

    private Participant GetParticipant()
    {
        return new Participant()
        {
            NHSId = "123456",
            SupersededByNhsNumber = "789012",
            PrimaryCareProvider = "ABC Clinic",
            NamePrefix = "Mr.",
            FirstName = "John",
            OtherGivenNames = "Middle",
            Surname = "Doe",
            DateOfBirth = new DateTime(1990-01-01),
            Gender = Gender.Male,
            AddressLine1 = "123 Main Street",
            AddressLine2 = "Apt 101",
            AddressLine3 = "Suburb",
            AddressLine4 = "City",
            AddressLine5 = "State",
            Postcode = "12345",
            ReasonForRemoval = "Moved",
            ReasonForRemovalEffectiveFromDate = new DateTime(2023-01-01),
            DateOfDeath = new DateTime(2024-04-23),
            TelephoneNumber = "123-456-7890",
            MobileNumber = "987-654-3210",
            EmailAddress = "john.doe@example.com",
            PreferredLanguage = "English",
            IsInterpreterRequired = false,
            RecordType = Actions.Amended
        };
    }
}
