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

[TestClass]
public class StaticValidationTests
{
    private readonly Mock<ILogger<StaticValidation>> _logger = new();
    private readonly Mock<FunctionContext> _context = new();
    private readonly Mock<HttpRequestData> _request;
    private readonly Mock<IExceptionHandler> _handleException = new();
    private readonly CreateResponse _createResponse = new();
    private readonly ServiceCollection _serviceCollection = new();
    private readonly ParticipantCsvRecord _participantCsvRecord;
    private readonly StaticValidation _function;
    private readonly Mock<IReadRules> _readRules = new();
    private readonly Mock<ICallFunction> _callFunction = new();

    public StaticValidationTests()
    {
        Environment.SetEnvironmentVariable("CreateValidationExceptionURL", "CreateValidationExceptionURL");

        _request = new Mock<HttpRequestData>(_context.Object);

        var serviceProvider = _serviceCollection.BuildServiceProvider();

        _context.SetupProperty(c => c.InstanceServices, serviceProvider);

        _handleException.Setup(x => x.CreateValidationExceptionLog(It.IsAny<IEnumerable<RuleResultTree>>(), It.IsAny<ParticipantCsvRecord>()))
            .Returns(Task.FromResult(new ValidationExceptionLog()
            {
                IsFatal = true,
                CreatedException = true
            })).Verifiable();

        var json = File.ReadAllText("../../../../../../../application/CohortManager/src/Functions/ScreeningValidationService/StaticValidation/Breast_Screening_staticRules.json");
        _readRules.Setup(x => x.GetRulesFromDirectory(It.IsAny<string>())).Returns(Task.FromResult<string>(json));

        _function = new StaticValidation(_logger.Object, _handleException.Object, _createResponse, _readRules.Object, _callFunction.Object);

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
        };
    }

    [TestMethod]
    public async Task Run_Should_Return_InternalServerError_When_Request_Body_Empty()
    {
        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_Should_Return_InternalServerError_When_Request_Body_Invalid()
    {
        // Arrange
        SetUpRequestBody("Invalid request body");

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    #region NHS Number (Rule 9)
    [TestMethod]
    [DataRow("0000000000")]
    [DataRow("9999999999")]
    public async Task Run_Should_Not_Create_Exceptions_When_NhsNumber_Rule_Passes(string nhsNumber)
    {
        // Arrange
        _participantCsvRecord.Participant.NhsNumber = nhsNumber;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "9.NhsNumber")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("0")]
    [DataRow("999999999")]      // 9 digits
    [DataRow("12.3456789")]     // 9 digits and 1 non-digit
    [DataRow("12.34567899")]    // 10 digits and 1 non-digit
    [DataRow("10000000000")]    // 11 digits
    public async Task Run_Should_Return_BadRequest_And_Create_Exception_When_NhsNumber_Rule_Fails(string nhsNumber)
    {
        // Arrange
        _participantCsvRecord.Participant.NhsNumber = nhsNumber;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "9.NhsNumber.Fatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }
    #endregion

    #region Superseded By NHS Number (Rule 57)
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("0000000000")]
    [DataRow("9999999999")]
    public async Task Run_Should_Not_Create_Exceptions_When_SupersededByNhsNumber_Rule_Passes(string supersededByNhsNumber)
    {
        // Arrange
        _participantCsvRecord.Participant.SupersededByNhsNumber = supersededByNhsNumber;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "57.SupersededByNhsNumber")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow("0")]
    [DataRow("999999999")]      // 9 digits
    [DataRow("12.3456789")]     // 9 digits and 1 non-digit
    [DataRow("12.34567899")]    // 10 digits and 1 non-digit
    [DataRow("10000000000")]    // 11 digits
    public async Task Run_Should_Return_Created_And_Create_Exception_When_SupersededByNhsNumber_Rule_Fails(string supersededByNhsNumber)
    {
        // Arrange
        _participantCsvRecord.Participant.SupersededByNhsNumber = supersededByNhsNumber;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "57.SupersededByNhsNumber.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }
    #endregion

    #region Record Type (Rule 8)
    [TestMethod]
    [DataRow("ADD")]
    [DataRow("AMENDED")]
    [DataRow("REMOVED")]
    public async Task Run_Should_Not_Create_Exception_When_RecordType_Rule_Passes(string recordType)
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = recordType;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "8.RecordType")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("Newish")]
    public async Task Run_Should_Return_Create_And_Create_Exception_When_RecordType_Rule_Fails(string recordType)
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = recordType;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "8.RecordType.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }
    #endregion

    #region Reason For Removal (Rule 14)
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("AFL")]
    [DataRow("AFN")]
    [DataRow("CGA")]
    [DataRow("DEA")]
    [DataRow("DIS")]
    [DataRow("EMB")]
    [DataRow("LDN")]
    [DataRow("NIT")]
    [DataRow("OPA")]
    [DataRow("ORR")]
    [DataRow("RDI")]
    [DataRow("RDR")]
    [DataRow("RFI")]
    [DataRow("RPR")]
    [DataRow("SCT")]
    [DataRow("SDL")]
    [DataRow("SDN")]
    [DataRow("TRA")]
    public async Task Run_Should_Not_Create_Exception_When_ReasonForRemoval_Rule_Passes(string reasonForRemoval)
    {
        // Arrange
        _participantCsvRecord.Participant.ReasonForRemoval = reasonForRemoval;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "14.ReasonForRemoval.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow("ABC")]
    [DataRow("123")]
    public async Task Run_Should_Return_Created_And_Create_Exception_When_ReasonForRemoval_Rule_Fails(string reasonForRemoval)
    {
        // Arrange
        _participantCsvRecord.Participant.ReasonForRemoval = reasonForRemoval;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "14.ReasonForRemoval.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
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
    [DataRow("")]
    [DataRow(null)]
    public async Task Run_Should_Not_Create_Exception_When_Postcode_Rule_Passes(string postcode)
    {
        // Arrange
        _participantCsvRecord.Participant.Postcode = postcode;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "30.Postcode")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow("ABC123")]
    [DataRow("ABC 123")]
    public async Task Run_Should_Return_Created_And_Create_Exception_When_Postcode_Rule_Fails(string postcode)
    {
        // Arrange
        _participantCsvRecord.Participant.Postcode = postcode;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "30.Postcode.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }
    #endregion

    #region NewParticipantWithNoAddress (Rule 19)
    [TestMethod]
    public async Task Run_Should_Not_Create_Exception_When_NewParticipantWithNoAddress_Rule_Passes()
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
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "71.NewParticipantWithNoAddress")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    public async Task Run_Should_Create_Exception_When_NewParticipantWithNoAddress_Rule_Fails()
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
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "71.NewParticipantWithNoAddress.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }

    [TestMethod]
    public async Task Run_Should_Not_Create_Exception_When_RecordType_Is_Not_New_And_Address_Is_Empty()
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
        var result = await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "71.NewParticipantWithNoAddress.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }
    #endregion

    #region Primary Care Provider and Reason For Removal (Rule 3)
    [TestMethod]
    [DataRow("ABC", null)]
    [DataRow(null, "123")]
    public async Task Run_Should_Not_Create_Exception_When_PrimaryCareProviderAndReasonForRemoval_Rule_Passes(string primaryCareProvider, string reasonForRemoval)
    {
        // Arrange
        _participantCsvRecord.Participant.PrimaryCareProvider = primaryCareProvider;
        _participantCsvRecord.Participant.ReasonForRemoval = reasonForRemoval;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "3.PrimaryCareProviderAndReasonForRemoval")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow(null, null)]
    [DataRow("ABC", "123")]
    public async Task Run_Should_Return_Created_And_Create_Exception_When_PrimaryCareProviderAndReasonForRemoval_Rule_Fails(string primaryCareProvider, string reasonForRemoval)
    {
        // Arrange
        _participantCsvRecord.Participant.PrimaryCareProvider = primaryCareProvider;
        _participantCsvRecord.Participant.ReasonForRemoval = reasonForRemoval;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "3.PrimaryCareProviderAndReasonForRemoval.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }
    #endregion

    #region Date Of Birth (Rule 17)
    [TestMethod]
    [DataRow("19700101")]   // ccyymmdd
    [DataRow("197001")]     // ccyymm
    [DataRow("1970")]       // ccyy
    public async Task Run_Should_Not_Create_Exception_When_DateOfBirth_Rule_Passes(string dateOfBirth)
    {
        // Arrange
        _participantCsvRecord.Participant.DateOfBirth = dateOfBirth;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "17.DateOfBirth")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("20700101")]   // In the future
    [DataRow("19700229")]   // Not a real date (1970 was not a leap year)
    [DataRow("1970023")]    // Incorrect format
    [DataRow("197013")]     // Not a real date or incorrect format
    public async Task Run_Should_Return_Created_And_Create_Exception_When_DateOfBirth_Rule_Fails(string dateOfBirth)
    {
        // Arrange
        _participantCsvRecord.Participant.DateOfBirth = dateOfBirth;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "17.DateOfBirth.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
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
    public async Task Run_Should_Not_Create_Exception_When_FamilyName_Rule_Passes(string familyName)
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = Actions.New;
        _participantCsvRecord.Participant.FamilyName = familyName;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "39.FamilyName")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public async Task Run_Should_Return_Created_And_Create_Exception_When_FamilyName_Rule_Fails(string familyName)
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = Actions.New;
        _participantCsvRecord.Participant.FamilyName = familyName;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "39.FamilyName.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }
    #endregion

    #region Given Name (Rule 40)
    [TestMethod]
    [DataRow("Jo")]
    [DataRow("Jean-Luc")]
    [DataRow("Sarah Jane")]
    [DataRow("Bartholomew")]
    public async Task Run_Should_Not_Create_Exception_When_GivenName_Rule_Passes(string givenName)
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = Actions.New;
        _participantCsvRecord.Participant.FirstName = givenName;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "40.FirstName")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public async Task Run_Should_Return_Created_And_Create_Exception_When_FirstName_Rule_Fails(string firstName)
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = Actions.New;
        _participantCsvRecord.Participant.FirstName = firstName;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "40.FirstName.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }
    #endregion

    #region GP Practice Code (Rule 42)
    [TestMethod]
    [DataRow("ADD", "ABC")]
    [DataRow("AMENDED", null)]
    [DataRow("REMOVED", null)]
    public async Task Run_Should_Not_Create_Exception_When_GPPracticeCode_Rule_Passes(string recordType, string gpPracticeCode)
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = recordType;
        _participantCsvRecord.Participant.PrimaryCareProvider = gpPracticeCode;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "42.GPPracticeCode")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow("ADD", null)]
    [DataRow("ADD", "")]
    public async Task Run_Should_Return_Created_And_Create_Exception_When_GPPracticeCode_Rule_Fails(string recordType, string practiceCode)
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = recordType;
        _participantCsvRecord.Participant.PrimaryCareProvider = practiceCode;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "42.GPPracticeCode.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }
    #endregion

    #region Death Status (Rule 66)
    [TestMethod]
    [DataRow("AMENDED", Status.Formal, "DEA")]
    public async Task Run_Should_Not_Create_Exception_When_DeathStatus_Rule_Passes(string recordType, Status deathStatus, string reasonForRemoval)
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = recordType;
        _participantCsvRecord.Participant.DeathStatus = deathStatus;
        _participantCsvRecord.Participant.ReasonForRemoval = reasonForRemoval;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "66.DeathStatus")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow("AMENDED", Status.Formal, null)]
    [DataRow("AMENDED", Status.Formal, "")]
    [DataRow("AMENDED", Status.Formal, "AFL")]
    public async Task Run_Should_Return_Created_And_Create_Exception_When_DeathStatus_Rule_Fails(string recordType, Status deathStatus, string reasonForRemoval)
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = recordType;
        _participantCsvRecord.Participant.DeathStatus = deathStatus;
        _participantCsvRecord.Participant.ReasonForRemoval = reasonForRemoval;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "66.DeathStatus.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }
    #endregion

    #region Reason For Removal Effective From Date (Rule 19)
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("19700101")]   // ccyymmdd
    [DataRow("197001")]     // ccyymm
    [DataRow("1970")]       // ccyy
    public async Task Run_Should_Not_Create_Exception_When_ReasonForRemovalEffectiveFromDate_Rule_Passes(string date)
    {
        // Arrange
        _participantCsvRecord.Participant.ReasonForRemovalEffectiveFromDate = date;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "19.ReasonForRemovalEffectiveFromDate.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow("20700101")]   // In the future
    [DataRow("19700229")]   // Not a real date (1970 was not a leap year)
    [DataRow("1970023")]    // Incorrect format
    [DataRow("197013")]     // Not a real date or incorrect format
    public async Task Run_Should_Return_Created_And_Create_Exception_When_ReasonForRemovalEffectiveFromDate_Rule_Fails(string date)
    {
        // Arrange
        _participantCsvRecord.Participant.ReasonForRemovalEffectiveFromDate = date;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "19.ReasonForRemovalEffectiveFromDate.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }
    #endregion

    #region Date Of Death (Rule 18)
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("19700101")]   // ccyymmdd
    [DataRow("197001")]     // ccyymm
    [DataRow("1970")]       // ccyy
    public async Task Run_Should_Not_Create_Exception_When_DateOfDeath_Rule_Passes(string date)
    {
        // Arrange
        _participantCsvRecord.Participant.DateOfDeath = date;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "18.DateOfDeath")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow("20700101")]   // In the future
    [DataRow("19700229")]   // Not a real date (1970 was not a leap year)
    [DataRow("1970023")]    // Incorrect format
    [DataRow("197013")]     // Not a real date or incorrect format
    public async Task Run_Should_Return_Created_And_Create_Exception_When_DateOfDeath_Rule_Fails(string date)
    {
        // Arrange
        _participantCsvRecord.Participant.DateOfDeath = date;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "18.DateOfDeath.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }
    #endregion

    #region New Participant with Reason For Removal, Removal Date or Date Of Death (Rule 47)
    [TestMethod]
    [DataRow("ADD", null, null, null)]
    [DataRow("ADD", "", "", "")]
    [DataRow("AMENDED", "DEA", "20240101", "20240101")]
    [DataRow("REMOVED", "DEA", "20240101", "20240101")]
    public async Task Run_Should_Not_Create_Exception_When_NewParticipantRemovalOrDeath_Rule_Passes(
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
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "47.NewParticipantWithRemovalOrDeath")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow("ADD", "DEA", null, null)]
    [DataRow("ADD", null, "20240101", null)]
    [DataRow("ADD", null, null, "20240101")]
    public async Task Run_Should_Return_Created_And_Create_Exception_When_NewParticipantRemovalOrDeath_Rule_Fails(
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
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "47.NewParticipantWithRemovalOrDeath.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }
    #endregion

    #region Invalid Flag (Rule 61)
    [TestMethod]
    [DataRow("True")]
    [DataRow("true")]
    [DataRow("False")]
    [DataRow("false")]
    public async Task Run_Should_Not_Create_Exception_When_InvalidFlag_Rule_Passes(string invalidFlag)
    {
        // Arrange
        _participantCsvRecord.Participant.InvalidFlag = invalidFlag;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "61.InvalidFlag")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("ABC")]
    public async Task Run_Should_Return_Created_And_Create_Exception_When_InvalidFlag_Rule_Fails(string invalidFlag)
    {
        // Arrange
        _participantCsvRecord.Participant.InvalidFlag = invalidFlag;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "61.InvalidFlag.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }
    #endregion

    #region IsInterpreterRequired (Rule 49)
    [TestMethod]
    [DataRow("1")]
    [DataRow("0")]
    public async Task Run_Should_Not_Create_Exception_When_IsInterpreterRequired_Rule_Passes(string isInterpreterRequired)
    {
        // Arrange
        _participantCsvRecord.Participant.IsInterpreterRequired = isInterpreterRequired;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "49.IsInterpreterRequired.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("ABC")]
    [DataRow("ABC")]
    public async Task Run_Should_Return_Created_And_Create_Exception_When_IsInterpreterRequired_Rule_Fails(string isInterpreterRequired)
    {
        // Arrange
        _participantCsvRecord.Participant.IsInterpreterRequired = isInterpreterRequired;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "49.InterpreterCheck.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }
    #endregion
    #region Validate Reason For Removal (Rule 62)
    [TestMethod]
    [DataRow("123456", "LDN")]
    [DataRow(null, "ABC")]
    [DataRow(null, null)]
    public async Task Run_Should_Not_Create_Exception_When_Validate_Reason_For_Removal_Rule_Passes(string? supersededByNhsNumber, string? ReasonForRemoval)
    {


        // Arrange
        _participantCsvRecord.Participant.SupersededByNhsNumber = supersededByNhsNumber;
        _participantCsvRecord.Participant.ReasonForRemoval = ReasonForRemoval;

        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "62.ValidateReasonForRemoval")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow(null, "LDN")]
    public async Task Run_Should_Return_Created_And_Create_Exception_Validate_Reason_For_Removal_Rule_Fails(string? supersededByNhsNumber, string ReasonForRemoval)
    {
        // Arrange
        _participantCsvRecord.Participant.SupersededByNhsNumber = supersededByNhsNumber;
        _participantCsvRecord.Participant.ReasonForRemoval = ReasonForRemoval;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "62.ValidateReasonForRemoval.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
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
    public async Task Run_CurrentPostingAndPrimaryCareProvider_CreatesException(string? currentPosting, string? primaryCareProvider)
    {
        // Arrange
        _participantCsvRecord.Participant.CurrentPosting = currentPosting;
        _participantCsvRecord.Participant.PrimaryCareProvider = primaryCareProvider;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "53.CurrentPostingAndPrimaryCareProvider.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }

    [TestMethod]
    [DataRow("BAA", "E85121")]
    [DataRow("BAA", null)]
    [DataRow(null, null)]
    public async Task Run_CurrentPostingAndPrimaryCareProvider_DoesNotCreateException(string? currentPosting, string? primaryCareProvider)
    {
        // Arrange
        _participantCsvRecord.Participant.CurrentPosting = currentPosting;
        _participantCsvRecord.Participant.PrimaryCareProvider = primaryCareProvider;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "53.CurrentPostingAndPrimaryCareProvider.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }
    #endregion

    #region Validate Eligibility Flag as per Record Type (Rule 94)
    [TestMethod]
    [DataRow(Actions.New, "0")]
    [DataRow(Actions.Removed, "1")]
    public async Task Run_InvalidEligibilityFlag_ShouldThrowException(string recordType, string eligibilityFlag)
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = recordType;
        _participantCsvRecord.Participant.EligibilityFlag = eligibilityFlag;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "94.EligibilityFlag.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }

    [TestMethod]
    [DataRow(Actions.New, "1")]
    [DataRow(Actions.Removed, "0")]
    [DataRow(Actions.Amended, "1")]
    public async Task Run_ValidEligibilityFlag_ShouldNotThrowException(string recordType, string eligibilityFlag)
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = recordType;
        _participantCsvRecord.Participant.EligibilityFlag = eligibilityFlag;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "94.EligibilityFlag.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }
    #endregion

    #region  Primary Care Provider Business Effective From Date (Rule 100)
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("19700101")]   // ccyymmdd
    [DataRow("197001")]     // ccyymm
    [DataRow("1970")]       // ccyy
    public async Task Run_ValidPrimaryCareProviderEffectiveFromDate_ShouldNotThrowException(string date)
    {
        // Arrange
        _participantCsvRecord.Participant.PrimaryCareProviderEffectiveFromDate = date;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "100.PrimaryCareProviderEffectiveFromDate.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow("20700101")]   // In the future
    [DataRow("19700229")]   // Not a real date (1970 was not a leap year)
    [DataRow("1970023")]    // Incorrect format
    [DataRow("197013")]     // Not a real date or incorrect format
    public async Task Run_InvalidPrimaryCareProviderEffectiveFromDate_ShouldThrowException(string date)
    {
        // Arrange
        _participantCsvRecord.Participant.PrimaryCareProviderEffectiveFromDate = date;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "100.PrimaryCareProviderEffectiveFromDate.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }
    #endregion

    #region Current Posting Business Effective From Date (Rule 101)
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("19700101")]   // ccyymmdd
    [DataRow("197001")]     // ccyymm
    [DataRow("1970")]       // ccyy
    public async Task Run_ValidCurrentPostingEffectiveFromDate_ShouldNotThrowException(string date)
    {
        // Arrange
        _participantCsvRecord.Participant.CurrentPostingEffectiveFromDate = date;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "101.CurrentPostingEffectiveFromDate.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow("20700101")]   // In the future
    [DataRow("19700229")]   // Not a real date (1970 was not a leap year)
    [DataRow("1970023")]    // Incorrect format
    [DataRow("197013")]     // Not a real date or incorrect format
    public async Task Run_InvalidCurrentPostingEffectiveFromDate_ShouldThrowException(string date)
    {
        // Arrange
        _participantCsvRecord.Participant.CurrentPostingEffectiveFromDate = date;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "101.CurrentPostingEffectiveFromDate.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }
    #endregion

    #region Usual Address Business Effective From Date (Rule 102)
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("19700101")]   // ccyymmdd
    [DataRow("197001")]     // ccyymm
    [DataRow("1970")]       // ccyy
    public async Task Run_ValidUsualAddressEffectiveFromDate_ShouldNotThrowException(string date)
    {
        // Arrange
        _participantCsvRecord.Participant.UsualAddressEffectiveFromDate = date;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "102.UsualAddressEffectiveFromDate.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow("20700101")]   // In the future
    [DataRow("19700229")]   // Not a real date (1970 was not a leap year)
    [DataRow("1970023")]    // Incorrect format
    [DataRow("197013")]     // Not a real date or incorrect format
    public async Task Run_InvalidUsualAddressEffectiveFromDate_ShouldThrowException(string date)
    {
        // Arrange
        _participantCsvRecord.Participant.UsualAddressEffectiveFromDate = date;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "102.UsualAddressEffectiveFromDate.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }
    #endregion

    #region Telephone Number (Home) Business Effective From Date (Rule 103)
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("19700101")]   // ccyymmdd
    [DataRow("197001")]     // ccyymm
    [DataRow("1970")]       // ccyy
    public async Task Run_ValidTelephoneNumberEffectiveFromDate_ShouldNotThrowException(string date)
    {
        // Arrange
        _participantCsvRecord.Participant.TelephoneNumberEffectiveFromDate = date;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
                It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "103.TelephoneNumberEffectiveFromDate.NonFatal")),
                It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow("20700101")]   // In the future
    [DataRow("19700229")]   // Not a real date (1970 was not a leap year)
    [DataRow("1970023")]    // Incorrect format
    [DataRow("197013")]     // Not a real date or incorrect format
    public async Task Run_InvalidTelephoneNumberEffectiveFromDate_ShouldThrowException(string date)
    {
        // Arrange
        _participantCsvRecord.Participant.TelephoneNumberEffectiveFromDate = date;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
                It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "103.TelephoneNumberEffectiveFromDate.NonFatal")),
                It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }
    #endregion

    #region Telephone Number (Mobile) Business Effective From Date (Rule 104)
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("19700101")]   // ccyymmdd
    [DataRow("197001")]     // ccyymm
    [DataRow("1970")]       // ccyy
    public async Task Run_ValidMobileNumberEffectiveFromDate_ShouldNotThrowException(string date)
    {
        // Arrange
        _participantCsvRecord.Participant.MobileNumberEffectiveFromDate = date;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
                It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "104.MobileNumberEffectiveFromDate.NonFatal")),
                It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow("20700101")]   // In the future
    [DataRow("19700229")]   // Not a real date (1970 was not a leap year)
    [DataRow("1970023")]    // Incorrect format
    [DataRow("197013")]     // Not a real date or incorrect format
    public async Task Run_InvalidMobileNumberEffectiveFromDate_ShouldThrowException(string date)
    {
        // Arrange
        _participantCsvRecord.Participant.MobileNumberEffectiveFromDate = date;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
                It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "104.MobileNumberEffectiveFromDate.NonFatal")),
                It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }
    #endregion

    #region E-mail address (Home)  Business Effective From Date (Rule 105)
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("19700101")]   // ccyymmdd
    [DataRow("197001")]     // ccyymm
    [DataRow("1970")]       // ccyy
    public async Task Run_ValidEmailAddressEffectiveFromDate_ShouldNotThrowException(string date)
    {
        // Arrange
        _participantCsvRecord.Participant.EmailAddressEffectiveFromDate = date;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
                It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "105.EmailAddressEffectiveFromDate.NonFatal")),
                It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow("20700101")]   // In the future
    [DataRow("19700229")]   // Not a real date (1970 was not a leap year)
    [DataRow("1970023")]    // Incorrect format
    [DataRow("197013")]     // Not a real date or incorrect format
    public async Task Run_InvalidEmailAddressEffectiveFromDate_ShouldThrowException(string date)
    {
        // Arrange
        _participantCsvRecord.Participant.EmailAddressEffectiveFromDate = date;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
                It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "105.EmailAddressEffectiveFromDate.NonFatal")),
                It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }
    #endregion
}