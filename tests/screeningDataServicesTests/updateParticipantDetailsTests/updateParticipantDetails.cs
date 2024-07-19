namespace NHS.CohortManger.Tests.ScreeningDataServicesTests;

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
    private readonly Mock<IDbConnection> _mockDBConnection = new();
    private readonly ParticipantCsvRecord _participantCsvRecord;
    private readonly Mock<IDbCommand> _commandMock = new();
    private readonly Mock<IDataReader> _moqDataReader = new();
    private readonly Mock<ILogger<UpdateParticipantData>> _loggerMock = new();
    private readonly Mock<IDatabaseHelper> _databaseHelperMock = new();
    private readonly Mock<IDbDataParameter> _mockParameter = new();
    private readonly Mock<IDbTransaction> _mockTransaction = new();
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly Mock<HttpWebResponse> _webResponse = new();

    public UpdateParticipantDetailsTests()
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

        _mockParameter.Setup(m => m.ParameterName).Returns("@fakeparam");
        _mockParameter.Setup(m => m.Value).Returns("fakeValue");

        _commandMock.Setup(x => x.CreateParameter()).Returns(_mockParameter.Object);

        _mockDBConnection.Setup(m => m.CreateCommand()).Returns(_commandMock.Object);
        _commandMock.Setup(m => m.Parameters.Add(It.IsAny<IDbDataParameter>())).Verifiable();
        _commandMock.Setup(m => m.ExecuteReader())
        .Returns(_moqDataReader.Object);
        _mockDBConnection.Setup(conn => conn.Open());

        _databaseHelperMock.Setup(helper => helper.ConvertNullToDbNull(It.IsAny<string>())).Returns(DBNull.Value);
        _databaseHelperMock.Setup(helper => helper.ParseDates(It.IsAny<string>())).Returns(DateTime.Today);

        _participantCsvRecord = new ParticipantCsvRecord
        {
            FileName = "test.csv",
            Participant = GetParticipant()
        };
    }

    [TestMethod]
    public async Task UpdateParticipantDetails_Success()
    {
        // Arrange
        _moqDataReader.SetupSequence(reader => reader.Read())
        .Returns(true)
        .Returns(false)
        .Returns(true)
        .Returns(false);
        _moqDataReader.Setup(reader => reader.GetInt32(0)).Returns(1);

        _commandMock.Setup(x => x.ExecuteNonQuery()).Returns(1);
        SetUpReader();

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        _callFunction.Setup(x => x.SendPost(It.Is<string>(s => s == "LookupValidationURL"), It.IsAny<string>()))
        .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        var sut = new UpdateParticipantData(_mockDBConnection.Object, _databaseHelperMock.Object, _loggerMock.Object, _callFunction.Object);

        // Act
        var result = await sut.UpdateParticipantDetails(_participantCsvRecord);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task UpdateParticipantDetails_FailsToGetOldId_False()
    {
        // Arrange
        _moqDataReader.SetupSequence(reader => reader.Read())
        .Returns(true)
        .Returns(false);
        _moqDataReader.Setup(reader => reader.GetInt32(0)).Returns(1);
        _commandMock.Setup(x => x.ExecuteNonQuery()).Returns(0);

        SetUpReader();

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        _callFunction.Setup(x => x.SendPost(It.Is<string>(s => s == "LookupValidationURL"), It.IsAny<string>()))
        .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        var sut = new UpdateParticipantData(_mockDBConnection.Object, _databaseHelperMock.Object, _loggerMock.Object, _callFunction.Object);

        // Act
        var result = await sut.UpdateParticipantDetails(_participantCsvRecord);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void UpdateParticipantAsEligible_UpdatesRecords_True()
    {
        // Arrange
        _moqDataReader.SetupSequence(reader => reader.Read())
        .Returns(true)
        .Returns(false);
        _moqDataReader.Setup(reader => reader.GetInt32(0)).Returns(1);
        _commandMock.Setup(x => x.ExecuteNonQuery()).Returns(1);

        SetUpReader();

        var sut = new UpdateParticipantData(_mockDBConnection.Object, _databaseHelperMock.Object, _loggerMock.Object, _callFunction.Object);

        // Act
        var result = sut.UpdateParticipantAsEligible(_participantCsvRecord.Participant, 'Y');

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void UpdateParticipantAsEligible_DoesNetGetOldId_False()
    {
        // Arrange
        _moqDataReader.SetupSequence(reader => reader.Read())
        .Returns(true)
        .Returns(false);
        _moqDataReader.Setup(reader => reader.GetInt32(0)).Returns(1);
        _commandMock.Setup(x => x.ExecuteNonQuery()).Returns(0);

        SetUpReader();

        var sut = new UpdateParticipantData(_mockDBConnection.Object, _databaseHelperMock.Object, _loggerMock.Object, _callFunction.Object);

        // Act
        var result = sut.UpdateParticipantAsEligible(_participantCsvRecord.Participant, 'Y');

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void GetParticipantId_SuccessfulQueryExecution_ReturnsParticipantId()
    {
        // Arrange
        var nhsId = "123456";
        var expectedParticipantId = 123456;
        _moqDataReader.SetupSequence(reader => reader.Read())
        .Returns(true)
        .Returns(false);
        _moqDataReader.Setup(reader => reader.GetInt32(0)).Returns(expectedParticipantId);
        _commandMock.Setup(x => x.ExecuteNonQuery()).Returns(1);

        SetUpReader();

        var sut = new UpdateParticipantData(_mockDBConnection.Object, _databaseHelperMock.Object, _loggerMock.Object, _callFunction.Object);

        // Act
        var result = sut.GetParticipant(nhsId);

        // Assert
        Assert.AreEqual(nhsId, result.NhsNumber);
    }

    [TestMethod]
    public void GetParticipantId_EmptyResultSet_ReturnsDefaultParticipantId()
    {
        // Arrange
        var nhsId = "123456";

        _moqDataReader.SetupSequence(reader => reader.Read())
            .Returns(true)
            .Returns(false);
        _moqDataReader.Setup(reader => reader.GetInt32(0)).Returns(0);
        _commandMock.Setup(x => x.ExecuteNonQuery()).Returns(1);

        SetUpReader();

        var sut = new UpdateParticipantData(_mockDBConnection.Object, _databaseHelperMock.Object, _loggerMock.Object, _callFunction.Object);

        // Act
        var result = sut.GetParticipant(nhsId);

        // Assert
        Assert.AreEqual("123456", result.NhsNumber);
    }

    [TestMethod]
    public async Task UpdateParticipantDetails_Fails_When_Validation_Fails()
    {
        // Arrange
        _moqDataReader.SetupSequence(reader => reader.Read())
        .Returns(true)
        .Returns(false);

        SetUpReader();

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.BadRequest);

        _callFunction.Setup(x => x.SendPost(It.Is<string>(s => s == "LookupValidationURL"), It.IsAny<string>()))
        .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        var sut = new UpdateParticipantData(_mockDBConnection.Object, _databaseHelperMock.Object, _loggerMock.Object, _callFunction.Object);

        // Act
        var result = await sut.UpdateParticipantDetails(_participantCsvRecord);

        // Assert
        Assert.IsFalse(result);
        _commandMock.Verify(command => command.ExecuteNonQuery(), Times.Once());//We still update the participant, but only set the Exception Flag.
    }

    private void SetUpReader()
    {
        _moqDataReader.Setup(m => m["PARTICIPANT_ID"]).Returns("123456");
        _moqDataReader.Setup(m => m["SCREENING_ID"]).Returns(DBNull.Value);
        _moqDataReader.Setup(m => m["NHS_NUMBER"]).Returns("123456");
        _moqDataReader.Setup(m => m["REASON_FOR_REMOVAL"]).Returns("Some Provider");
        _moqDataReader.Setup(m => m["REASON_FOR_REMOVAL_DT"]).Returns(DBNull.Value);
        _moqDataReader.Setup(m => m["BUSINESS_RULE_VERSION"]).Returns(DBNull.Value);
        _moqDataReader.Setup(m => m["EXCEPTION_FLAG"]).Returns(DBNull.Value);
        _moqDataReader.Setup(m => m["OTHER_NAME"]).Returns(DBNull.Value);
        _moqDataReader.Setup(m => m["RECORD_INSERT_DATETIME"]).Returns(DBNull.Value);
        _moqDataReader.Setup(m => m["RECORD_UPDATE_DATETIME"]).Returns(DBNull.Value);
    }

    private static Participant GetParticipant()
    {
        return new Participant()
        {
            NhsNumber = "123456",
            SupersededByNhsNumber = "789012",
            PrimaryCareProvider = "ABC Clinic",
            NamePrefix = "Mr.",
            FirstName = "John",
            OtherGivenNames = "Middle",
            Surname = "Doe",
            DateOfBirth = "1990-01-01",
            Gender = Gender.Male,
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
            IsInterpreterRequired = "0",
            RecordType = Actions.Amended
        };
    }
}
