namespace NHS.CohortManager.Tests.UnitTests.AddCohortDistributionDataTests;

using System.Data;
using Data.Database;
using Microsoft.Extensions.Logging;
using Model;
using Model.DTO;
using Model.Enums;
using Moq;
using NHS.CohortManager.Tests.TestUtils;

[TestClass]
public class AddCohortDistributionTests : DatabaseTestBaseSetup<CreateCohortDistributionData>
{
    private static readonly Mock<IDatabaseHelper> _databaseHelperMock = new();
    private readonly CreateCohortDistributionData _createCohortDistributionData;
    private const int serviceProviderId = (int)ServiceProvider.BSS;
    private readonly string _requestId = new Guid().ToString();
    private Dictionary<string, string> columnToClassPropertyMapping;
    private List<CohortDistributionParticipantDto> _cohortDistributionList;
    // private readonly int _requestId = 1;

public AddCohortDistributionTests(): base((conn, logger, transaction, command, response) =>
    new CreateCohortDistributionData(conn, _databaseHelperMock.Object, logger))
    {
        _databaseHelperMock.Setup(helper => helper.ConvertNullToDbNull(It.IsAny<string>())).Returns(DBNull.Value);
        _databaseHelperMock.Setup(helper => helper.ParseDates(It.IsAny<string>())).Returns(DateTime.Today);

        _createCohortDistributionData = new CreateCohortDistributionData(
            _mockDBConnection.Object,
            _databaseHelperMock.Object,
            _loggerMock.Object);


columnToClassPropertyMapping = new Dictionary<string, string>
        {
            { "REQUEST_ID", "RequestId" },
            { "PARTICIPANT_ID", "ParticipantId" },
            { "NHS_NUMBER", "NhsNumber" },
            { "SUPERSEDED_NHS_NUMBER", "SupersededNhsNumber" },
            { "PRIMARY_CARE_PROVIDER", "PrimaryCareProvider" },
            { "PRIMARY_CARE_PROVIDER_FROM_DT", "PrimaryCareProviderFromDt" },
            { "NAME_PREFIX", "NamePrefix" },
            { "GIVEN_NAME", "GivenName" },
            { "OTHER_GIVEN_NAME", "OtherGivenName" },
            { "FAMILY_NAME", "FamilyName" },
            { "PREVIOUS_FAMILY_NAME", "PreviousFamilyName" },
            { "DATE_OF_BIRTH", "DateOfBirth" },
            { "GENDER", "Gender" },
            { "ADDRESS_LINE_1", "AddressLine1" },
            { "ADDRESS_LINE_2", "AddressLine2" },
            { "ADDRESS_LINE_3", "AddressLine3" },
            { "ADDRESS_LINE_4", "AddressLine4" },
            { "ADDRESS_LINE_5", "AddressLine5" },
            { "POST_CODE", "PostCode" },
            { "USUAL_ADDRESS_FROM_DT", "UsualAddressFromDt" },
            { "CURRENT_POSTING", "CurrentPosting" },
            { "CURRENT_POSTING_FROM_DT", "CurrentPostingFromDt" },
            { "DATE_OF_DEATH", "DateOfDeath" },
            { "TELEPHONE_NUMBER_HOME", "TelephoneNumberHome" },
            { "TELEPHONE_NUMBER_HOME_FROM_DT", "TelephoneNumberHomeFromDt" },
            { "TELEPHONE_NUMBER_MOB", "TelephoneNumberMob" },
            { "TELEPHONE_NUMBER_MOB_FROM_DT", "TelephoneNumberMobFromDt" },
            { "EMAIL_ADDRESS_HOME", "EmailAddressHome" },
            { "EMAIL_ADDRESS_HOME_FROM_DT", "EmailAddressHomeFromDt" },
            { "PREFERRED_LANGUAGE", "PreferredLanguage" },
            { "INTERPRETER_REQUIRED", "InterpreterRequired" },
            { "REASON_FOR_REMOVAL", "ReasonForRemoval" },
            { "REASON_FOR_REMOVAL_FROM_DT", "ReasonForRemovalFromDt" },
            { "RECORD_INSERT_DATETIME", "RecordInsertDatetime" },
            { "RECORD_UPDATE_DATETIME", "RecordUpdateDatetime" },
            { "IS_EXTRACTED", "IsExtracted" }
        };

        _cohortDistributionList = new List<CohortDistributionParticipantDto>
        {
            new CohortDistributionParticipantDto
            {
                RequestId = _requestId,
                ParticipantId = "1",
                NhsNumber = "123456",
                IsExtracted = "0"
            }
        };
                    SetupDataReader(_cohortDistributionList, columnToClassPropertyMapping);
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

        var rowCount = 1;

        // Act
        var result = _createCohortDistributionData.GetUnextractedCohortDistributionParticipantsByScreeningServiceId(serviceProviderId, rowCount);

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
        var rowCount = 1;

        // Act
        var result = _createCohortDistributionData.GetUnextractedCohortDistributionParticipantsByScreeningServiceId(serviceProviderId, rowCount);

        // Assert
        _commandMock.Verify(x => x.ExecuteNonQuery(), Times.AtLeast(2));
        Assert.AreEqual("1", result[0].ParticipantId);
        Assert.AreEqual("1", result[0].IsExtracted);
        Assert.AreEqual("1", result[1].ParticipantId);
        Assert.AreEqual("1", result[1].IsExtracted);
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
        var result = _createCohortDistributionData.GetUnextractedCohortDistributionParticipantsByScreeningServiceId(serviceProviderId, rowCount);

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

    public void GetNextCohortRequestAudit_GuidParseFails_ReturnsEmptyCohortRequestAudit()
    {
        // Arrange
        var invalidRequestId = "i-Am-Not-A-Guid";

        // Act
        var result = _createCohortDistributionData.GetNextCohortRequestAudit(invalidRequestId);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(CohortRequestAudit));
    }

    [TestMethod]
    public void GetCohortDistributionParticipantsByRequestId_ValidRequestId_ReturnsParticipants()
    {
        // Arrange
        _mockDataReader.SetupSequence(reader => reader.Read())
            .Returns(true)
            .Returns(false);

        _mockDataReader.Setup(reader => reader["REQUEST_ID"]).Returns(_requestId);
        _mockDataReader.Setup(reader => reader["PARTICIPANT_ID"]).Returns("participantId");

        _commandMock.Setup(m => m.ExecuteReader()).Returns(_mockDataReader.Object);

        // Act
        var result = _createCohortDistributionData.GetCohortDistributionParticipantsByRequestId(_requestId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("participantId", result[0].ParticipantId);
        Assert.AreEqual(_requestId, result[0].RequestId);
    }

    // private void SetUpReader()
    // {
    //     _mockDataReader.Setup(reader => reader["REQUEST_ID"]).Returns(() => _requestId);
    //     _mockDataReader.Setup(reader => reader["PARTICIPANT_ID"]).Returns(() => "1");
    //     _mockDataReader.Setup(reader => reader["NHS_NUMBER"]).Returns(() => "123456");
    //     _mockDataReader.Setup(reader => reader["SUPERSEDED_NHS_NUMBER"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["PRIMARY_CARE_PROVIDER"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["PRIMARY_CARE_PROVIDER_FROM_DT"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["NAME_PREFIX"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["GIVEN_NAME"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["OTHER_GIVEN_NAME"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["FAMILY_NAME"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["PREVIOUS_FAMILY_NAME"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["DATE_OF_BIRTH"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["GENDER"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["ADDRESS_LINE_1"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["ADDRESS_LINE_2"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["ADDRESS_LINE_3"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["ADDRESS_LINE_4"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["ADDRESS_LINE_5"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["POST_CODE"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["USUAL_ADDRESS_FROM_DT"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["CURRENT_POSTING"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["CURRENT_POSTING_FROM_DT"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["DATE_OF_DEATH"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["TELEPHONE_NUMBER_HOME"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["TELEPHONE_NUMBER_HOME_FROM_DT"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["TELEPHONE_NUMBER_MOB"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["TELEPHONE_NUMBER_MOB_FROM_DT"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["EMAIL_ADDRESS_HOME"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["EMAIL_ADDRESS_HOME_FROM_DT"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["PREFERRED_LANGUAGE"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["INTERPRETER_REQUIRED"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["REASON_FOR_REMOVAL"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["REASON_FOR_REMOVAL_FROM_DT"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["RECORD_INSERT_DATETIME"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["RECORD_UPDATE_DATETIME"]).Returns(DBNull.Value);
    //     _mockDataReader.Setup(reader => reader["IS_EXTRACTED"]).Returns(() => 0);
    // }
}
