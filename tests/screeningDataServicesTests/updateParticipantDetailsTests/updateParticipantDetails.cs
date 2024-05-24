namespace NHS.CohortManger.Tests.ScreeningDataServices;

using System.Data;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Common;
using Data.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using updateParticipantDetails;

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

    private readonly Mock<IDataReader> mockDataReader;

    private readonly Mock<IDbTransaction> mockTransaction;

    private readonly Mock<ICallFunction> callFunction;

    Mock<HttpWebResponse> webResponse;

    public UpdateParticipantDetailsTests()
    {
        Environment.SetEnvironmentVariable("DtOsDatabaseConnectionString", "DtOsDatabaseConnectionString");
        Environment.SetEnvironmentVariable("VaildationURL", "VaildationURL");

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
    public async Task UpdateParticipantDetails_Success()
    {
        // Arrange
        moqDataReader.SetupSequence(reader => reader.Read())
        .Returns(true)
        .Returns(false)
        .Returns(true)
        .Returns(false)
        .Returns(true)
        .Returns(false);
        moqDataReader.Setup(reader => reader.GetInt32(0)).Returns(1); // Return expected id

        commandMock.Setup(x => x.ExecuteNonQuery()).Returns(1);
        setUpReader();
        _databaseHelperMock.Setup(helper => helper.ConvertNullToDbNull(It.IsAny<string>())).Returns(DBNull.Value);
        _databaseHelperMock.Setup(helper => helper.parseDates(It.IsAny<string>())).Returns(DateTime.Today);

        var validationErrors = new List<string>();

        var data = Encoding.UTF8.GetBytes(string.Join(",", validationErrors));
        MemoryStream memoryStream = new MemoryStream(data);

        webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        webResponse.Setup(x => x.GetResponseStream()).Returns(memoryStream);

        callFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
        .Returns(Task.FromResult<HttpWebResponse>(webResponse.Object));

        var participantData = GetParticipant();
        var updateParticipantData = new UpdateParticipantData(mockDBConnection.Object, _databaseHelperMock.Object, _loggerMock.Object, callFunction.Object);

        // Act
        var result = await updateParticipantData.UpdateParticipantDetails(participantData);

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
        moqDataReader.Setup(reader => reader.GetInt32(0)).Returns(1); // Return expected id
        commandMock.Setup(x => x.ExecuteNonQuery()).Returns(0);

        setUpReader();

        _databaseHelperMock.Setup(helper => helper.ConvertNullToDbNull(It.IsAny<string>())).Returns(DBNull.Value);
        _databaseHelperMock.Setup(helper => helper.parseDates(It.IsAny<string>())).Returns(DateTime.Today);


        var validationErrors = new List<string>();

        var data = Encoding.UTF8.GetBytes(string.Join(",", validationErrors));
        MemoryStream memoryStream = new MemoryStream(data);

        webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        webResponse.Setup(x => x.GetResponseStream()).Returns(memoryStream);

        callFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
        .Returns(Task.FromResult<HttpWebResponse>(webResponse.Object));

        var participantData = GetParticipant();
        var updateParticipantData = new UpdateParticipantData(mockDBConnection.Object, _databaseHelperMock.Object, _loggerMock.Object, callFunction.Object);

        // Act
        var result = await updateParticipantData.UpdateParticipantDetails(participantData);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task UpdateParticipantAsEligible_UpdatesRecords_True()
    {
        // Arrange
        moqDataReader.SetupSequence(reader => reader.Read())
        .Returns(true)
        .Returns(false);
        moqDataReader.Setup(reader => reader.GetInt32(0)).Returns(1);
        commandMock.Setup(x => x.ExecuteNonQuery()).Returns(1);

        setUpReader();

        _databaseHelperMock.Setup(helper => helper.ConvertNullToDbNull(It.IsAny<string>())).Returns(DBNull.Value);
        _databaseHelperMock.Setup(helper => helper.parseDates(It.IsAny<string>())).Returns(DateTime.Today);


        var validationErrors = new List<string>();

        var data = Encoding.UTF8.GetBytes(string.Join(",", validationErrors));
        MemoryStream memoryStream = new MemoryStream(data);

        webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        webResponse.Setup(x => x.GetResponseStream()).Returns(memoryStream);

        callFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
        .Returns(Task.FromResult<HttpWebResponse>(webResponse.Object));

        var participantData = GetParticipant();
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

        setUpReader();

        _databaseHelperMock.Setup(helper => helper.ConvertNullToDbNull(It.IsAny<string>())).Returns(DBNull.Value);
        _databaseHelperMock.Setup(helper => helper.parseDates(It.IsAny<string>())).Returns(DateTime.Today);


        var validationErrors = new List<string>();

        var data = Encoding.UTF8.GetBytes(string.Join(",", validationErrors));
        MemoryStream memoryStream = new MemoryStream(data);

        webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        webResponse.Setup(x => x.GetResponseStream()).Returns(memoryStream);

        callFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
        .Returns(Task.FromResult<HttpWebResponse>(webResponse.Object));

        var participantData = GetParticipant();
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

        setUpReader();

        _databaseHelperMock.Setup(helper => helper.ConvertNullToDbNull(It.IsAny<string>())).Returns(DBNull.Value);
        _databaseHelperMock.Setup(helper => helper.parseDates(It.IsAny<string>())).Returns(DateTime.Today);


        var validationErrors = new List<string>();

        var data = Encoding.UTF8.GetBytes(string.Join(",", validationErrors));
        MemoryStream memoryStream = new MemoryStream(data);

        webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        webResponse.Setup(x => x.GetResponseStream()).Returns(memoryStream);

        callFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
        .Returns(Task.FromResult<HttpWebResponse>(webResponse.Object));


        // Mock ExecuteQuery and CreateCommand
        var updateParticipantData = new UpdateParticipantData(mockDBConnection.Object, _databaseHelperMock.Object, _loggerMock.Object, callFunction.Object);

        // Create an instance of the class under test
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

        setUpReader();

        _databaseHelperMock.Setup(helper => helper.ConvertNullToDbNull(It.IsAny<string>())).Returns(DBNull.Value);
        _databaseHelperMock.Setup(helper => helper.parseDates(It.IsAny<string>())).Returns(DateTime.Today);


        var validationErrors = new List<string>();

        var data = Encoding.UTF8.GetBytes(string.Join(",", validationErrors));
        MemoryStream memoryStream = new MemoryStream(data);

        webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        webResponse.Setup(x => x.GetResponseStream()).Returns(memoryStream);

        callFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
        .Returns(Task.FromResult<HttpWebResponse>(webResponse.Object));


        // Mock ExecuteQuery and CreateCommand
        var updateParticipantData = new UpdateParticipantData(mockDBConnection.Object, _databaseHelperMock.Object, _loggerMock.Object, callFunction.Object);

        // Create an instance of the class under test
        // Act
        var result = updateParticipantData.GetParticipant(nhsId);

        // Assert
        Assert.AreEqual("123456", result.NHSId);
    }

    [TestMethod]
    public async Task UpdateParticipantDetails_Validate_rule_1()
    {
        // Arrange
        moqDataReader.SetupSequence(reader => reader.Read())
        .Returns(true)
        .Returns(false)
        .Returns(true)
        .Returns(false)
        .Returns(true)
        .Returns(false);
        moqDataReader.Setup(reader => reader.GetInt32(0)).Returns(1); // Return expected id

        commandMock.Setup(x => x.ExecuteNonQuery()).Returns(1);
        setUpReader();
        _databaseHelperMock.Setup(helper => helper.ConvertNullToDbNull(It.IsAny<string>())).Returns(DBNull.Value);
        _databaseHelperMock.Setup(helper => helper.parseDates(It.IsAny<string>())).Returns(DateTime.Today);

        var validationErrors = new List<string>();

        var data = Encoding.UTF8.GetBytes(string.Join(",", validationErrors));
        MemoryStream memoryStream = new MemoryStream(data);

        webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        webResponse.Setup(x => x.GetResponseStream()).Returns(memoryStream);

        callFunction.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
        .Returns(Task.FromResult<HttpWebResponse>(webResponse.Object));

        var participantData = GetParticipant();
        var updateParticipantData = new UpdateParticipantData(mockDBConnection.Object, _databaseHelperMock.Object, _loggerMock.Object, callFunction.Object);

        // Act
        var result = await updateParticipantData.UpdateParticipantDetails(participantData);

        // Assert
        Assert.IsTrue(result);
    }

    private void setUpReader()
    {
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
            DateOfBirth = "04/04/1959",
            Gender = "Male",
            AddressLine1 = "123 Main Street",
            AddressLine2 = "Apt 101",
            AddressLine3 = "Suburb",
            AddressLine4 = "City",
            AddressLine5 = "State",
            Postcode = "12345",
            ReasonForRemoval = "Moved",
            ReasonForRemovalEffectiveFromDate = "04/04/1959",
            DateOfDeath = "04/04/1959",
            TelephoneNumber = "1234567890",
            MobileNumber = "9876543210",
            EmailAddress = "john.doe@example.com",
            PreferredLanguage = "English",
            IsInterpreterRequired = "No",
            Action = "Update"
        };
    }
}
