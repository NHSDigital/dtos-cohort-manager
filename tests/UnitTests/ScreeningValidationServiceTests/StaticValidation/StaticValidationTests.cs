namespace NHS.CohortManager.Tests.UnitTests.ScreeningValidationServiceTests;
using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;
using Moq;
using NHS.CohortManager.ScreeningValidationService;
using RulesEngine.Models;
using NHS.CohortManager.Tests.TestUtils;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;

[TestClass]
public class StaticValidationTests
{
    private readonly Mock<ILogger<StaticValidation>> _logger = new();
    private readonly Mock<FunctionContext> _context = new();
    private readonly Mock<HttpRequestData> _request;
    private readonly CreateResponse _createResponse = new();
    private readonly ServiceCollection _serviceCollection = new();
    private readonly ParticipantCsvRecord _participantCsvRecord;
    private readonly StaticValidation _function;

    public StaticValidationTests()
    {
        _request = new Mock<HttpRequestData>(_context.Object);

        var serviceProvider = _serviceCollection.BuildServiceProvider();

        _context.SetupProperty(c => c.InstanceServices, serviceProvider);

        _function = new StaticValidation(
            _logger.Object,
            _createResponse,
            new ReadRules(new NullLogger<ReadRules>())
        );

        _request.Setup(r => r.CreateResponse()).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(_context.Object);
            response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
            response.SetupProperty(r => r.StatusCode);
            response.SetupProperty(r => r.Body, new MemoryStream());
            return response.Object;
        });

        _participantCsvRecord = new ParticipantCsvRecord()
        {
            FileName = "test",
            Participant = new Participant()
            {
                ScreeningName = "Breast Screening",
                NhsNumber = "1211111881",
                RecordType = Actions.New,
                AddressLine1 = "Address1",
                AddressLine2 = "Address2",
                AddressLine3 = "Address3",
                AddressLine4 = "Address4",
                AddressLine5 = "Address5",
                PrimaryCareProvider = "E85121",
                DateOfBirth = "20130112",
                FirstName = "Test",
                FamilyName = "Test",
                InvalidFlag = "0",
                IsInterpreterRequired = "0",
                CurrentPosting = "ABC",
                EligibilityFlag = "1",
                ReferralFlag = "false"
            }
        };
    }

    [TestMethod]
    public async Task Run_Should_Return_InternalServerError_When_Request_Body_Empty()
    {
        // Act
        var response = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [TestMethod]
    public async Task Run_Should_Return_InternalServerError_When_Request_Body_Invalid()
    {
        // Arrange
        SetUpRequestBody("Invalid request body");

        // Act
        var response = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [TestMethod]
    public async Task Run_ParticipantReferred_DoNotRunRoutineRules()
    {
        // Arrange
        _participantCsvRecord.Participant.ReferralFlag = "true";
        _participantCsvRecord.Participant.PrimaryCareProvider = "ABC";
        _participantCsvRecord.Participant.ReasonForRemoval = "123";
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
    }

    [TestMethod]
    public async Task Run_ParticipantReferred_RunCommonRules()
    {
        // Arrange
        _participantCsvRecord.Participant.ReferralFlag = "true";
        _participantCsvRecord.Participant.Postcode = "ZzZ99 LZ";
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);
        string body = await AssertionHelper.ReadResponseBodyAsync(response);

        // Assert
        StringAssert.Contains(body, "30.Postcode.NBO.NonFatal");
    }

    #region Record Type (Rule 8)
    [TestMethod]
    [DataRow(Actions.New, "1")]
    [DataRow(Actions.Amended, "1")]
    [DataRow(Actions.Removed, "0")]
    public async Task Run_ValidRecordType_ReturnNoContent(string recordType, string eligibilityFlag)
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = recordType;
        _participantCsvRecord.Participant.EligibilityFlag = eligibilityFlag;

        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("Newish")]
    public async Task Run_InvalidRecordType_ReturnValidationException(string recordType)
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = recordType;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);
        string body = await AssertionHelper.ReadResponseBodyAsync(response);

        // Assert
        StringAssert.Contains(body, "8.RecordType.CaaS.NonFatal");
    }
    #endregion

    #region Postcode (Rule 30)
    [TestMethod]
    [DataRow("ec1a1bb")]
    [DataRow("EC1A1BB")]
    [DataRow("ec1a 1bb")]
    [DataRow("EC1A 1BB")]
    [DataRow("W1A 0AX")]
    [DataRow("M1 1AE")]
    [DataRow("B33 8TH")]
    [DataRow("CR2 6XH")]
    [DataRow("LS10 1LT")]
    [DataRow("GIR 0AA")]
    [DataRow("GIR0AA")]
    [DataRow("")]
    [DataRow(null)]
    // Dummy Postcodes
    [DataRow("ZZ99 9FZ")]
    [DataRow("ZZ999FZ")]
    [DataRow("ZZ99 3WZ")]
    public async Task Run_ValidPostcode_ReturnNoContent(string postcode)
    {
        // Arrange
        _participantCsvRecord.Participant.Postcode = postcode;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
    }

    [TestMethod]
    [DataRow("ABC123")]
    [DataRow("1234 AB")]
    [DataRow("AA 12345")]
    [DataRow("A1B 1CDE")]
    [DataRow("A1A@1AA")]
    [DataRow("ZZ9 4LZ")]
    [DataRow("Z99 4")]
    [DataRow("ZzZ99 LZ")]
    public async Task Run_InvalidPostcode_ReturnValidationException(string postcode)
    {
        // Arrange
        _participantCsvRecord.Participant.Postcode = postcode;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);
        string body = await AssertionHelper.ReadResponseBodyAsync(response);

        // Assert
        StringAssert.Contains(body, "30.Postcode.NBO.NonFatal");
    }
    #endregion

    #region NewParticipantWithNoAddress (Rule 19)
    [TestMethod]
    public async Task Run_AddWithAdress_ReturnNoContent()
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = Actions.New;
        _participantCsvRecord.Participant.AddressLine1 = "SomeAddress";
        _participantCsvRecord.Participant.AddressLine2 = "";
        _participantCsvRecord.Participant.AddressLine3 = "";
        _participantCsvRecord.Participant.AddressLine4 = "";
        _participantCsvRecord.Participant.AddressLine5 = "";
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
    }

    [TestMethod]
    public async Task Run_AddWithEmptyAddress_ReturnValidationException()
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = Actions.New;
        _participantCsvRecord.Participant.AddressLine1 = "";
        _participantCsvRecord.Participant.AddressLine2 = "";
        _participantCsvRecord.Participant.AddressLine3 = "";
        _participantCsvRecord.Participant.AddressLine4 = "";
        _participantCsvRecord.Participant.AddressLine5 = "";
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);
        string body = await AssertionHelper.ReadResponseBodyAsync(response);

        // Assert
        StringAssert.Contains(body, "71.NewParticipantWithNoAddress.NBO.NonFatal");
    }

    [TestMethod]
    public async Task Run_AmendWithEmptyAddress_ReturnNoContent()
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = Actions.Amended;
        _participantCsvRecord.Participant.AddressLine1 = "";
        _participantCsvRecord.Participant.AddressLine2 = "";
        _participantCsvRecord.Participant.AddressLine3 = "";
        _participantCsvRecord.Participant.AddressLine4 = "";
        _participantCsvRecord.Participant.AddressLine5 = "";
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
    }
    #endregion

    #region Primary Care Provider and Reason For Removal (Rule 3)
    [TestMethod]
    [DataRow("ABC", null)]
    [DataRow(null, "123")]
    public async Task Run_CompatiblePcpAndRfr_ReturnNoContent(string primaryCareProvider, string reasonForRemoval)
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = Actions.Amended;
        _participantCsvRecord.Participant.PrimaryCareProvider = primaryCareProvider;
        _participantCsvRecord.Participant.ReasonForRemoval = reasonForRemoval;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
    }

    [TestMethod]
    [DataRow(null, null)]
    [DataRow("ABC", "123")]
    public async Task Run_IncompatiblePcpAndRfr_ReturnValidationException(string primaryCareProvider, string reasonForRemoval)
    {
        // Arrange
        _participantCsvRecord.Participant.PrimaryCareProvider = primaryCareProvider;
        _participantCsvRecord.Participant.ReasonForRemoval = reasonForRemoval;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);
        string body = await AssertionHelper.ReadResponseBodyAsync(response);

        // Assert
        StringAssert.Contains(body, "3.PrimaryCareProviderAndReasonForRemoval.NBO.NonFatal");
    }
    #endregion

    #region Date Of Birth (Rule 17)
    [TestMethod]
    [DataRow("19700101")]   // ccyymmdd
    [DataRow("197001")]     // ccyymm
    [DataRow("1970")]       // ccyy
    public async Task Run_ValidDateOfBirth_ReturnNoContent(string dateOfBirth)
    {
        // Arrange
        _participantCsvRecord.Participant.DateOfBirth = dateOfBirth;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("20700101")]   // In the future
    [DataRow("19700229")]   // Not a real date (1970 was not a leap year)
    [DataRow("1970023")]    // Incorrect format
    [DataRow("197013")]     // Not a real date or incorrect format
    public async Task Run_InvalidDateOfBirth_ReturnValidationException(string dateOfBirth)
    {
        // Arrange
        _participantCsvRecord.Participant.DateOfBirth = dateOfBirth;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);
        string body = await AssertionHelper.ReadResponseBodyAsync(response);

        // Assert
        StringAssert.Contains(body, "17.DateOfBirth.NBO.NonFatal");
    }
    #endregion

    #region Family Name (Rule 39)
    [TestMethod]
    [DataRow("Li")]
    [DataRow("McDonald")]
    [DataRow("O'Neill")]
    [DataRow("Zeta-Jones")]
    [DataRow("Bonham Carter")]
    [DataRow("Venkatasubramanian")]
    public async Task Run_ValidFamilyName_ReturnNoContent(string familyName)
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = Actions.New;
        _participantCsvRecord.Participant.FamilyName = familyName;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public async Task Run_InvalidFamilyName_ReturnValidationException(string familyName)
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = Actions.New;
        _participantCsvRecord.Participant.FamilyName = familyName;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);
        string body = await AssertionHelper.ReadResponseBodyAsync(response);

        // Assert
        StringAssert.Contains(body, "39.FamilyName.NBO.NonFatal");
    }
    #endregion

    #region Given Name (Rule 40)
    [TestMethod]
    [DataRow("Jo")]
    [DataRow("Jean-Luc")]
    [DataRow("Sarah Jane")]
    [DataRow("Bartholomew")]
    public async Task Run_ValidFirstName_ReturnNoContent(string givenName)
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = Actions.New;
        _participantCsvRecord.Participant.FirstName = givenName;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public async Task Run_InvalidFirstName_ReturnValidationExceptions(string firstName)
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = Actions.New;
        _participantCsvRecord.Participant.FirstName = firstName;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);
        string body = await AssertionHelper.ReadResponseBodyAsync(response);

        // Assert
        StringAssert.Contains(body, "40.FirstName.NBO.NonFatal");
    }
    #endregion

    #region Death Status (Rule 66)
    [TestMethod]
    [DataRow("AMENDED", Status.Formal, "DEA")]
    public async Task Run_ValidDeathStatus_ReturnNoContent(string recordType, Status deathStatus, string reasonForRemoval)
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = recordType;
        _participantCsvRecord.Participant.DeathStatus = deathStatus;
        _participantCsvRecord.Participant.ReasonForRemoval = reasonForRemoval;
        _participantCsvRecord.Participant.PrimaryCareProvider = null;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
    }

    [TestMethod]
    [DataRow("AMENDED", Status.Formal, null)]
    [DataRow("AMENDED", Status.Formal, "")]
    [DataRow("AMENDED", Status.Formal, "AFL")]
    public async Task Run_InvalidDeathStatus_ReturnValidationException(string recordType, Status deathStatus, string reasonForRemoval)
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = recordType;
        _participantCsvRecord.Participant.DeathStatus = deathStatus;
        _participantCsvRecord.Participant.ReasonForRemoval = reasonForRemoval;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);
        string body = await AssertionHelper.ReadResponseBodyAsync(response);

        // Assert
        StringAssert.Contains(body, "66.DeathStatus.NBO.NonFatal");
    }
    #endregion

    #region Date Of Death (Rule 18)
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("19700101")]   // ccyymmdd
    [DataRow("197001")]     // ccyymm
    [DataRow("1970")]       // ccyy
    public async Task Run_ValidDateOfDeath_ReturnNoContent(string date)
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = Actions.Removed;
        _participantCsvRecord.Participant.DateOfDeath = date;
        _participantCsvRecord.Participant.EligibilityFlag = "0";
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
    }

    [TestMethod]
    [DataRow("20700101")]   // In the future
    [DataRow("19700229")]   // Not a real date (1970 was not a leap year)
    [DataRow("1970023")]    // Incorrect format
    [DataRow("197013")]     // Not a real date or incorrect format
    public async Task Run_InvalidDateOfDeath_ReturnValidationException(string date)
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = Actions.Removed;
        _participantCsvRecord.Participant.DateOfDeath = date;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);
        string body = await AssertionHelper.ReadResponseBodyAsync(response);

        // Assert
        StringAssert.Contains(body, "18.DateOfDeath.NBO.NonFatal");
    }
    #endregion

    #region New Participant with Reason For Removal, Removal Date or Date Of Death (Rule 47)
    [TestMethod]
    [DataRow(Actions.New, null, null, null, "EA123AB", "1")]
    [DataRow(Actions.New, "", "", "", "EA123AB", "1")]
    [DataRow(Actions.Amended, "DEA", "20240101", "20240101", null, "1")]
    [DataRow(Actions.Removed, "DEA", "20240101", "20240101", null, "0")]
    public async Task Run_ValidRfrAndDeathDate_ReturnNoContent(
        string recordType, string reasonForRemoval, string removalDate, string dateOfDeath, string pcp, string eligibilityFlag)
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = recordType;
        _participantCsvRecord.Participant.ReasonForRemoval = reasonForRemoval;
        _participantCsvRecord.Participant.ReasonForRemovalEffectiveFromDate = removalDate;
        _participantCsvRecord.Participant.DateOfDeath = dateOfDeath;
        _participantCsvRecord.Participant.PrimaryCareProvider = pcp;
        _participantCsvRecord.Participant.EligibilityFlag = eligibilityFlag;

        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
    }

    [TestMethod]
    [DataRow("ADD", "DEA", null, null)]
    [DataRow("ADD", null, "20240101", null)]
    [DataRow("ADD", null, null, "20240101")]
    public async Task Run_AddRecordWithRfrOrDateOfDeath_ReturnValidationException(
        string recordType, string reasonForRemoval, string removalDate, string dateOfDeath)
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = recordType;
        _participantCsvRecord.Participant.ReasonForRemoval = reasonForRemoval;
        _participantCsvRecord.Participant.ReasonForRemovalEffectiveFromDate = removalDate;
        _participantCsvRecord.Participant.DateOfDeath = dateOfDeath;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);
        string body = await AssertionHelper.ReadResponseBodyAsync(response);

        // Assert
        StringAssert.Contains(body, "47.NewParticipantWithRemovalOrDeath.NBO.NonFatal");
    }
    #endregion

    #region IsInterpreterRequired (Rule 49)
    [TestMethod]
    [DataRow("1")]
    [DataRow("0")]
    public async Task Run_ValidInterpreterRequiredFlag_ReturnNoContent(string isInterpreterRequired)
    {
        // Arrange
        _participantCsvRecord.Participant.IsInterpreterRequired = isInterpreterRequired;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("ABC")]
    public async Task Run_InvalidInterpreterRequiredFlag_ReturnValidationExcpetion(string isInterpreterRequired)
    {
        // Arrange
        _participantCsvRecord.Participant.IsInterpreterRequired = isInterpreterRequired;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);
        string body = await AssertionHelper.ReadResponseBodyAsync(response);

        // Assert
        StringAssert.Contains(body, "49.InterpreterCheck.NBO.NonFatal");
    }
    #endregion

    #region Validate Reason For Removal (Rule 62)
    [TestMethod]
    [DataRow("123456", "LDN", null)]
    [DataRow(null, "ABC", null)]
    [DataRow(null, null, "EC12AB")]
    public async Task Run_ValidRfr_ReturnNoContent(string? supersededByNhsNumber, string? ReasonForRemoval, string? pcp)
    {
        // Arrange
        _participantCsvRecord.Participant.SupersededByNhsNumber = supersededByNhsNumber;
        _participantCsvRecord.Participant.ReasonForRemoval = ReasonForRemoval;
        _participantCsvRecord.Participant.PrimaryCareProvider = pcp;
        _participantCsvRecord.Participant.RecordType = Actions.Amended;

        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
    }

    [TestMethod]
    [DataRow(null, "LDN")]
    public async Task Run_InvalidRfr_ReturnValidationException(string? supersededByNhsNumber, string ReasonForRemoval)
    {
        // Arrange
        _participantCsvRecord.Participant.SupersededByNhsNumber = supersededByNhsNumber;
        _participantCsvRecord.Participant.ReasonForRemoval = ReasonForRemoval;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);
        string body = await AssertionHelper.ReadResponseBodyAsync(response);

        // Assert
        StringAssert.Contains(body, "62.ValidateReasonForRemoval.NBO.NonFatal");
    }
    #endregion
    private void SetUpRequestBody(string json)
    {
        var byteArray = Encoding.ASCII.GetBytes(json);
        var bodyStream = new MemoryStream(byteArray);

        _request.Setup(r => r.Body).Returns(bodyStream);
    }

    #region Supplied Posting is Null Validation (Rule 53)
    [TestMethod]
    [DataRow(null, "E85121")]
    public async Task Run_IncompatibleCurrentPostingAndPrimaryCareProvider_ReturnValidationException(string? currentPosting, string? primaryCareProvider)
    {
        // Arrange
        _participantCsvRecord.Participant.CurrentPosting = currentPosting;
        _participantCsvRecord.Participant.PrimaryCareProvider = primaryCareProvider;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);
        string body = await AssertionHelper.ReadResponseBodyAsync(response);

        // Assert
        StringAssert.Contains(body, "53.CurrentPostingAndPrimaryCareProvider.NBO.NonFatal");
    }

    [TestMethod]
    [DataRow("BAA", "E85121", null)]
    [DataRow("BAA", null, "DEA")]
    [DataRow(null, null, "DEA")]
    public async Task Run_CompatibleCurrentPostingAndPrimaryCareProvider_ReturnNoContent(string? currentPosting, string? primaryCareProvider, string? rfr)
    {
        // Arrange
        _participantCsvRecord.Participant.CurrentPosting = currentPosting;
        _participantCsvRecord.Participant.PrimaryCareProvider = primaryCareProvider;
        _participantCsvRecord.Participant.ReasonForRemoval = rfr;
        _participantCsvRecord.Participant.RecordType = Actions.Amended;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
    }
    #endregion

    #region Validate Eligibility Flag as per Record Type (Rule 94)
    [TestMethod]
    [DataRow(Actions.New, "0")]
    [DataRow(Actions.Removed, "1")]
    public async Task Run_InvalidEligibilityFlag_ReturnValidationException(string recordType, string eligibilityFlag)
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = recordType;
        _participantCsvRecord.Participant.EligibilityFlag = eligibilityFlag;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);
        string body = await AssertionHelper.ReadResponseBodyAsync(response);

        // Assert
        StringAssert.Contains(body, "94.EligibilityFlag.CaaS.NonFatal");
    }

    [TestMethod]
    [DataRow(Actions.New, "1")]
    [DataRow(Actions.Removed, "0")]
    [DataRow(Actions.Amended, "1")]
    public async Task Run_ValidEligibilityFlag_ReturnNoContent(string recordType, string eligibilityFlag)
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = recordType;
        _participantCsvRecord.Participant.EligibilityFlag = eligibilityFlag;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
    }
    #endregion

    [TestMethod]
    public async Task Run_ValidParticipantFile_ReturnNoContent()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var response = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
    }
}
