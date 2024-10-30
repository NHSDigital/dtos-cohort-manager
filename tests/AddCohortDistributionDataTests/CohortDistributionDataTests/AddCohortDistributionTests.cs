namespace NHS.CohortManager.Tests.AddCohortDistributionDataTests;

using System.Data;
using Data.Database;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;
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
    private readonly CreateCohortDistributionData _createCohortDistributionData;
    private const int serviceProviderId = (int)ServiceProvider.BSS;
    private readonly string _requestId = new Guid().ToString();

    public AddCohortDistributionTests()
    {
        Environment.SetEnvironmentVariable("DtOsDatabaseConnectionString", "DtOsDatabaseConnectionString");
        Environment.SetEnvironmentVariable("LookupValidationURL", "LookupValidationURL");

        _mockDBConnection.Setup(x => x.ConnectionString).Returns("someFakeConnectionString");
        _mockDBConnection.Setup(x => x.BeginTransaction()).Returns(_mockTransaction.Object);

        _commandMock.Setup(c => c.Dispose());
        _commandMock.Setup(m => m.Parameters.Add(It.IsAny<IDbDataParameter>())).Verifiable();
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

        _createCohortDistributionData = new CreateCohortDistributionData(
            _mockDBConnection.Object,
            _databaseHelperMock.Object,
            _loggerMock.Object);
        }

    [TestMethod]
    public void InsertCohortDistributionData_ValidData_ReturnsSuccess()
    {
        // Arrange
        var cohortDistributionParticipant = new CohortDistributionParticipant();
        _commandMock.Setup(x => x.ExecuteNonQuery()).Returns(1);

        // Act
        var result = _createCohortDistributionData.InsertCohortDistributionData(cohortDistributionParticipant);

        // Assert
        Assert.IsTrue(result);
        _commandMock.Verify(m => m.ExecuteNonQuery(), Times.Once);
    }

    [TestMethod]
    public void InsertCohortDistributionData_InvalidData_ReturnsFailure()
    {
        // Arrange
        var cohortDistributionParticipant = new CohortDistributionParticipant();
        _commandMock.Setup(x => x.ExecuteNonQuery()).Returns(0);

        // Act
        var result = _createCohortDistributionData.InsertCohortDistributionData(cohortDistributionParticipant);

        // Assert
        Assert.IsFalse(result);
        _commandMock.Verify(m => m.ExecuteNonQuery(), Times.Once);
        _mockTransaction.Verify(t => t.Rollback(), Times.Once);
    }

    [TestMethod]
    public void ExtractCohortDistributionParticipants_ValidRequest_ReturnsListOfParticipants()
    {
        // Arrange
        _mockDataReader.SetupSequence(reader => reader.Read())
            .Returns(true)
            .Returns(false);
        _commandMock.Setup(x => x.ExecuteNonQuery()).Returns(1);

        SetUpReader();
        var rowCount = 1;

        // Act
        var result = _createCohortDistributionData.ExtractCohortDistributionParticipants(serviceProviderId, rowCount);

        // Assert
        Assert.AreEqual("1", result.FirstOrDefault()?.ParticipantId);
        Assert.AreEqual(1, result.Count());
    }

    [TestMethod]
    public void ExtractCohortDistributionParticipants_AfterExtraction_MarksBothParticipantsAsExtracted()
    {
        // Arrange
        _mockDataReader.SetupSequence(reader => reader.Read())
            .Returns(true)
            .Returns(true)
            .Returns(false);
        _commandMock.Setup(x => x.ExecuteNonQuery()).Returns(1);

        SetUpReader();
        var rowCount = 1;

        // Act
        var result = _createCohortDistributionData.ExtractCohortDistributionParticipants(serviceProviderId, rowCount);

        // Assert
        _commandMock.Verify(x => x.ExecuteNonQuery(), Times.AtLeast(2));
        Assert.AreEqual("1", result[0].ParticipantId);
        Assert.AreEqual("1", result[0].Extracted);
        Assert.AreEqual("1", result[1].ParticipantId);
        Assert.AreEqual("1", result[1].Extracted);
    }

    [TestMethod]
    public void GetParticipant_NoParticipants_ReturnsEmptyCollection()
    {
        // Arrange
        _mockDataReader.SetupSequence(reader => reader.Read())
            .Returns(false);
        _commandMock.Setup(x => x.ExecuteNonQuery()).Returns(1);
        var rowCount = 0;

        // Act
        var result = _createCohortDistributionData.ExtractCohortDistributionParticipants(serviceProviderId, rowCount);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void GetCohortDistributionParticipantsByRequestId_RequestId_ReturnsMatchingParticipants()
    {
        // Arrange
        _mockDataReader.SetupSequence(reader => reader.Read())
        .Returns(true)
        .Returns(false);

        SetUpReader();

        _commandMock.Setup(m => m.ExecuteReader()).Returns(_mockDataReader.Object);

        // Act
        var validRequestIdResult = _createCohortDistributionData.GetCohortDistributionParticipantsByRequestId(_requestId);

        var inValidRequestIdResult = _createCohortDistributionData.GetCohortDistributionParticipantsByRequestId("Non Matching RequestID");

        // Assert
        Assert.AreEqual(_requestId, validRequestIdResult.First().RequestId);
        Assert.AreEqual(1, validRequestIdResult.Count);
        Assert.AreEqual(0, inValidRequestIdResult.Count);
    }

    [TestMethod]
    public void GetCohortDistributionParticipantsByRequestId_NoParticipants_ReturnsEmptyList()
    {
        // Arrange
        _mockDataReader.SetupSequence(reader => reader.Read())
            .Returns(false);

        _commandMock.Setup(m => m.ExecuteReader()).Returns(_mockDataReader.Object);

        // Act
        var result = _createCohortDistributionData.GetCohortDistributionParticipantsByRequestId(_requestId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }


    // [TestMethod]
    // public void GetLastCohortRequest_NewerErrorRequestId_ReturnsParticipants()
    // {
    //     // Arrange
    //     var lastRequestId = "lastRequestId";

    //     var cohortRequestAuditList = new List<CohortRequestAudit>
    // {
    //     new CohortRequestAudit { RequestId = "lastRequestId", StatusCode = "200", CreatedDateTime = DateTime.Now.AddDays(-1).ToString() },
    //     new CohortRequestAudit { RequestId = "NewRequestId", StatusCode = "500", CreatedDateTime = DateTime.Now.ToString() }
    // };

    //     var expectedParticipants = new List<CohortDistributionParticipant>
    // {
    //     new CohortDistributionParticipant { ParticipantId = "1", RequestId = "NewRequestId" }
    // };

    //     _mockDataReader.SetupSequence(reader => reader.Read())
    //         .Returns(true)
    //         .Returns(true)
    //         .Returns(false);

    //     _mockDataReader.Setup(reader => reader["REQUEST_ID"]).Returns("NewRequestId");


    //         //wp issue is here calling 2 different database commands and trying to mock them
    //         //this may not possible as the mock is not able to differentiate between the two commands
    //      _mockDataReader.Setup(reader => reader["PARTICIPANT_ID"]).Returns(expectedParticipants[0].ParticipantId);
    //      _mockDataReader.Setup(reader => reader["REQUEST_ID"]).Returns(expectedParticipants[0].RequestId);


    //     // Act
    //     var result = _createCohortDistributionData.GetLastCohortRequest(lastRequestId);

    //     // Assert
    //     Assert.AreEqual(1, result.Count);
    //     Assert.AreEqual(expectedParticipants[0].ParticipantId, result[0].ParticipantId);
    //     Assert.AreEqual(expectedParticipants[0].RequestId, result[0].RequestId);
    //     Assert.IsInstanceOfType(result, typeof(List<CohortDistributionParticipant>));
    // }


//     [TestMethod]
//     public void GetLastCohortRequest_NewerErrorRequestId_ReturnsParticipants_Test2()
//     {
//         // Arrange
//         // Arrange
//         var lastRequestId = "lastRequestId";
//         var expectedRequestId = _requestId;
//         var expectedParticipants = new List<CohortDistributionParticipant>
// {
//     new CohortDistributionParticipant { ParticipantId = "1", RequestId = expectedRequestId }
// };

//         var cohortRequestAuditList = new List<CohortRequestAudit>
//     {
//         new CohortRequestAudit { RequestId = "lastRequestId", StatusCode = "200", CreatedDateTime = DateTime.Now.AddDays(-1).ToString() },
//         new CohortRequestAudit { RequestId = "NewRequestId", StatusCode = "500", CreatedDateTime = DateTime.Now.ToString() }
//     };


//         _mockDataReader.SetupSequence(reader => reader.Read())
//             .Returns(true)
//             .Returns(false);

//         // audit
//         _mockDataReader.Setup(reader => reader["REQUEST_ID"]).Returns(expectedRequestId);

//         //GetParticipant
//         _mockDataReader.SetupSequence(reader => reader.Read())
//             .Returns(true)
//             .Returns(false);

//         _mockDataReader.Setup(reader => reader["PARTICIPANT_ID"]).Returns(expectedParticipants[0].ParticipantId);
//         _mockDataReader.Setup(reader => reader["REQUEST_ID"]).Returns(expectedParticipants[0].RequestId);

//         // Act
//         var result = _createCohortDistributionData.GetLastCohortRequest(lastRequestId);

//         // Assert
//         Assert.AreEqual(1, result.Count);
//         Assert.AreEqual(expectedParticipants[0].ParticipantId, result[0].ParticipantId);
//         Assert.AreEqual(expectedParticipants[0].RequestId, result[0].RequestId);

//     }


//wp - look at making 2 seperate calls in the same test and compare results



    [TestMethod]
    public void GetLastCohortRequest_NoParticipants_ReturnsEmptyList()
    {
        // Arrange
        var lastRequestId = "lastRequestId";
        _mockDataReader.Setup(r => r.Read()).Returns(false);

        // Act
        var result = _createCohortDistributionData.GetLastCohortRequest(lastRequestId);

        // Assert
        Assert.AreEqual(0, result.Result.Count);
    }

    private void SetUpReader()
    {
        _mockDataReader.Setup(reader => reader["REQUEST_ID"]).Returns(() => _requestId);
        _mockDataReader.Setup(reader => reader["PARTICIPANT_ID"]).Returns(() => "1");
        _mockDataReader.Setup(reader => reader["NHS_NUMBER"]).Returns(() => "123456");
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
        _mockDataReader.Setup(reader => reader["CURRENT_POSTING"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["CURRENT_POSTING_FROM_DT"]).Returns(DBNull.Value);
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
        _mockDataReader.Setup(reader => reader["REASON_FOR_REMOVAL_FROM_DT"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["RECORD_INSERT_DATETIME"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["RECORD_UPDATE_DATETIME"]).Returns(DBNull.Value);
        _mockDataReader.Setup(reader => reader["IS_EXTRACTED"]).Returns(() => 0);
    }
}
