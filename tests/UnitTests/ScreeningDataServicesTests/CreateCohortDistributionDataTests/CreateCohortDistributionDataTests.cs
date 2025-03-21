namespace NHS.CohortManager.Tests.UnitTests.ScreeningDataServicesTests;

using Moq;
using Data.Database;
using Model;
using NHS.CohortManager.Tests.TestUtils;

[TestClass]
public class CreateCohortDistributionDataTests : DatabaseTestBaseSetup<CreateCohortDistributionData>
{
    private readonly CreateCohortDistributionData _createCohortDistributionData;
    private readonly Dictionary<string, string> _columnToClassPropertyMapping;
    private List<CohortDistributionParticipant> _cohortDistributionList;
    private readonly string _requestId = Guid.NewGuid().ToString();

    public CreateCohortDistributionDataTests() : base((conn, logger, transaction, command, response) =>
        new CreateCohortDistributionData(conn, logger))
    {
        _createCohortDistributionData = new CreateCohortDistributionData(
            _mockDBConnection.Object,
            _loggerMock.Object);

        _columnToClassPropertyMapping = new Dictionary<string, string>
            {
                { "REQUEST_ID", "RequestId" },
                { "PARTICIPANT_ID", "ParticipantId" },
                { "NHS_NUMBER", "NhsNumber" },
                { "IS_EXTRACTED", "Extracted" },
                { "SUPERSEDED_NHS_NUMBER", "SupersededByNhsNumber" },
                { "PRIMARY_CARE_PROVIDER", "PrimaryCareProvider" },
                { "PRIMARY_CARE_PROVIDER_FROM_DT", "PrimaryCareProviderEffectiveFromDate" },
                { "NAME_PREFIX", "NamePrefix" },
                { "GIVEN_NAME", "FirstName" },
                { "OTHER_GIVEN_NAME", "OtherGivenNames" },
                { "FAMILY_NAME", "FamilyName" },
                { "PREVIOUS_FAMILY_NAME", "PreviousFamilyName" },
                { "DATE_OF_BIRTH", "DateOfBirth" },
                { "GENDER", "Gender" },
                { "ADDRESS_LINE_1", "AddressLine1" },
                { "ADDRESS_LINE_2", "AddressLine2" },
                { "ADDRESS_LINE_3", "AddressLine3" },
                { "ADDRESS_LINE_4", "AddressLine4" },
                { "ADDRESS_LINE_5", "AddressLine5" },
                { "POST_CODE", "Postcode" },
                { "USUAL_ADDRESS_FROM_DT", "UsualAddressEffectiveFromDate" },
                { "CURRENT_POSTING", "CurrentPosting" },
                { "CURRENT_POSTING_FROM_DT", "CurrentPostingEffectiveFromDate" },
                { "DATE_OF_DEATH", "DateOfDeath" },
                { "TELEPHONE_NUMBER_HOME", "TelephoneNumber" },
                { "TELEPHONE_NUMBER_HOME_FROM_DT", "TelephoneNumberEffectiveFromDate" },
                { "TELEPHONE_NUMBER_MOB", "MobileNumber" },
                { "TELEPHONE_NUMBER_MOB_FROM_DT", "MobileNumberEffectiveFromDate" },
                { "EMAIL_ADDRESS_HOME", "EmailAddress" },
                { "EMAIL_ADDRESS_HOME_FROM_DT", "EmailAddressEffectiveFromDate" },
                { "PREFERRED_LANGUAGE", "PreferredLanguage" },
                { "INTERPRETER_REQUIRED", "IsInterpreterRequired" },
                { "REASON_FOR_REMOVAL", "ReasonForRemoval" },
                { "REASON_FOR_REMOVAL_FROM_DT", "ReasonForRemovalEffectiveFromDate" },
                { "RECORD_INSERT_DATETIME", "RecordInsertDateTime" },
                { "RECORD_UPDATE_DATETIME", "RecordUpdateDateTime" },
                { "SCREENING_ACRONYM", "ScreeningAcronym" },
                { "SCREENING_SERVICE_ID", "ScreeningServiceId" },
                { "SCREENING_NAME", "ScreeningName" },
                { "ELIGIBILITY_FLAG", "EligibilityFlag" },
                { "RECORD_TYPE", "RecordType" }
            };

        _cohortDistributionList = new List<CohortDistributionParticipant>
            {
                new CohortDistributionParticipant
                {
                    RequestId = _requestId,
                    ParticipantId = "1",
                    NhsNumber = "1234567890",
                    Extracted = "0",
                    RecordType = "ADD"
                }
            };

        SetupDataReader(_cohortDistributionList, _columnToClassPropertyMapping);
    }

    #region GetCohortRequestAudit
    [TestMethod]
    public async Task GetCohortRequestAudit_WithAllParameters_ReturnsValidCohortRequestAudit()
    {
        // Arrange
        string requestId = "testRequestId";
        string statusCode = "testStatusCode";
        DateTime dateFrom = DateTime.Now.AddDays(-1);
        SetUpReader();

        // Act
        var result = await _createCohortDistributionData.GetCohortRequestAudit(requestId, statusCode, dateFrom);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("testRequestId", result[0].RequestId);
        Assert.AreEqual("200", result[0].StatusCode);
        Assert.IsInstanceOfType(result, typeof(List<CohortRequestAudit>));
        Assert.IsTrue(dateFrom <= DateTime.Parse(result[0].CreatedDateTime));
    }

    [TestMethod]
    public async Task GetCohortRequestAudit_WithNullParameters_ReturnsValidCohortRequestAudit()
    {
        // Arrange
        string? requestId = null;
        string? statusCode = null;
        DateTime? dateFrom = null;
        SetUpReader();

        // Act
        var result = await _createCohortDistributionData.GetCohortRequestAudit(requestId, statusCode, dateFrom);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.IsInstanceOfType(result, typeof(List<CohortRequestAudit>));
    }

    [TestMethod]
    public async Task GetCohortRequestAudit_WithEmptyRequestId_ReturnsValidCohortRequestAudit()
    {
        // Arrange
        string requestId = "";
        string statusCode = "testStatusCode";
        DateTime dateFrom = DateTime.Now.AddDays(-1);
        SetUpReader();

        // Act
        var result = await _createCohortDistributionData.GetCohortRequestAudit(requestId, statusCode, dateFrom);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("testRequestId", result[0].RequestId);
        Assert.AreEqual("200", result[0].StatusCode);
        Assert.IsInstanceOfType(result, typeof(List<CohortRequestAudit>));
        Assert.IsTrue(dateFrom <= DateTime.Parse(result[0].CreatedDateTime));
    }

    [TestMethod]
    public async Task GetCohortRequestAudit_WithEmptyStatusCode_ReturnsValidCohortRequestAudit()
    {
        // Arrange
        string requestId = "testRequestId";
        string statusCode = "";
        DateTime dateFrom = DateTime.Now.AddDays(-1);
        SetUpReader();

        // Act
        var result = await _createCohortDistributionData.GetCohortRequestAudit(requestId, statusCode, dateFrom);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("testRequestId", result[0].RequestId);
        Assert.AreEqual("200", result[0].StatusCode);
        Assert.IsInstanceOfType(result, typeof(List<CohortRequestAudit>));
        Assert.IsTrue(dateFrom <= DateTime.Parse(result[0].CreatedDateTime));
    }

    [TestMethod]
    public async Task GetCohortRequestAudit_WithFutureDateFrom_ReturnsEmptyList()
    {
        // Arrange
        string requestId = "testRequestId";
        string statusCode = "testStatusCode";
        DateTime dateFrom = DateTime.Now.AddDays(1);
        _mockDataReader.Setup(reader => reader.Read()).Returns(false);

        // Act
        var result = await _createCohortDistributionData.GetCohortRequestAudit(requestId, statusCode, dateFrom);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
        Assert.IsInstanceOfType(result, typeof(List<CohortRequestAudit>));
    }

    [TestMethod]
    public async Task GetCohortRequestAudit_WithNoMatchingRecords_ReturnsEmptyList()
    {
        // Arrange
        string requestId = "nonExistentRequestId";
        string statusCode = "nonExistentStatusCode";
        DateTime dateFrom = DateTime.Now.AddDays(-1);
        _mockDataReader.Setup(reader => reader.Read()).Returns(false);

        // Act
        var result = await _createCohortDistributionData.GetCohortRequestAudit(requestId, statusCode, dateFrom);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
        Assert.IsInstanceOfType(result, typeof(List<CohortRequestAudit>));
    }
    private void SetUpReader()
    {
        _mockDataReader.Setup(reader => reader["REQUEST_ID"]).Returns("testRequestId");
        _mockDataReader.Setup(reader => reader["STATUS_CODE"]).Returns("200");
        _mockDataReader.Setup(reader => reader["CREATED_DATETIME"]).Returns(DateTime.Now.ToString());
    }
    #endregion

    #region UpdateCohortParticipantAsInactive
    [TestMethod]
    public void UpdateCohortDistributionParticipantAsInactive_Success()
    {
        // Arrange
        _commandMock.Setup(x => x.ExecuteNonQuery()).Returns(1);
        var nhsNumber = "123456890";

        // Act
        var result = _createCohortDistributionData.UpdateCohortParticipantAsInactive(nhsNumber);
        //Assert
        Assert.IsTrue(result);
        _commandMock.Verify(m => m.ExecuteNonQuery(), Times.Once);
    }

    [TestMethod]
    public void UpdateCohortDistributionParticipantAsInactive_ParticipantNotExists_Failure()
    {
        // Arrange
        _commandMock.Setup(x => x.ExecuteNonQuery()).Returns(0);
        var nhsNumber = "123456890";

        // Act
        var result = _createCohortDistributionData.UpdateCohortParticipantAsInactive(nhsNumber);

        // Assert
        Assert.IsFalse(result);
        _commandMock.Verify(m => m.ExecuteNonQuery(), Times.Once);
    }

    [TestMethod]
    public void UpdateCohortDistributionParticipantAsInactive_NoNhsNumberProvided_Failure()
    {
        // Arrange
        _commandMock.Setup(x => x.ExecuteNonQuery()).Returns(0);
        var nhsNumber = string.Empty;

        // Act
        var result = _createCohortDistributionData.UpdateCohortParticipantAsInactive(nhsNumber);

        // Assert
        Assert.IsFalse(result);
        _commandMock.Verify(m => m.ExecuteNonQuery(), Times.Never);
    }
    #endregion

    #region GetUnextractedCohortDistributionParticipants
    [TestMethod]
    public void ExtractCohortDistributionParticipants_ValidRequest_ReturnsListOfParticipants()
    {
        // Arrange
        _commandMock.Setup(x => x.ExecuteNonQuery()).Returns(1);
        var rowCount = 1;

        // Act
        var result = _createCohortDistributionData.GetUnextractedCohortDistributionParticipants(rowCount);

        // Assert
        Assert.AreEqual("1", result.FirstOrDefault()?.ParticipantId);
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public void ExtractCohortDistributionParticipants_AfterExtraction_MarksBothParticipantsAsExtracted()
    {
        // Arrange
        _cohortDistributionList = new List<CohortDistributionParticipant>
        {
            new CohortDistributionParticipant
            {
                ParticipantId = "1",
                Extracted = "0"
            },
            new CohortDistributionParticipant
            {
                ParticipantId = "2",
                Extracted = "0"
            }
        };
        SetupDataReader(_cohortDistributionList, _columnToClassPropertyMapping);
        _commandMock.Setup(x => x.ExecuteNonQuery()).Returns(1);
        var rowCount = 2;

        // Act
        var result = _createCohortDistributionData.GetUnextractedCohortDistributionParticipants(rowCount);

        // Assert
        _commandMock.Verify(x => x.ExecuteNonQuery(), Times.AtLeast(2));
        Assert.AreEqual("1", result[0].ParticipantId);
        Assert.AreEqual("1", result[0].IsExtracted);
        Assert.AreEqual("2", result[1].ParticipantId);
        Assert.AreEqual("1", result[1].IsExtracted);
    }

    [TestMethod]
    public void GetParticipant_NoParticipants_ReturnsEmptyCollection()
    {
        // Arrange
        _mockDataReader.SetupSequence(reader => reader.Read()).Returns(false);
        var rowCount = 0;

        // Act
        var result = _createCohortDistributionData.GetUnextractedCohortDistributionParticipants(rowCount);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }
    #endregion

    #region GetCohortDistributionParticipantsByRequestId
    [TestMethod]
    public void GetCohortDistributionParticipantsByRequestId_RequestId_ReturnsMatchingParticipants()
    {
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
        _mockDataReader.SetupSequence(reader => reader.Read()).Returns(false);

        // Act
        var result = _createCohortDistributionData.GetCohortDistributionParticipantsByRequestId(_requestId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
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
        // Act
        var result = _createCohortDistributionData.GetCohortDistributionParticipantsByRequestId(_requestId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("1", result[0].ParticipantId);
        Assert.AreEqual(_requestId, result[0].RequestId);
    }
    #endregion

}
