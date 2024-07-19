namespace NHS.CohortManager.Tests.ScreeningValidationServiceTests;

using System.IO.Compression;
using System.Linq.Expressions;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Common;
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
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly Mock<FunctionContext> _context = new();
    private readonly Mock<HttpRequestData> _request;
    private readonly Mock<IExceptionHandler> _handleException = new();
    private readonly CreateResponse _createResponse = new();
    private readonly ServiceCollection _serviceCollection = new();
    private readonly ParticipantCsvRecord _participantCsvRecord;
    private readonly StaticValidation _function;

    public StaticValidationTests()
    {
        Environment.SetEnvironmentVariable("CreateValidationExceptionURL", "CreateValidationExceptionURL");

        _request = new Mock<HttpRequestData>(_context.Object);

        var serviceProvider = _serviceCollection.BuildServiceProvider();

        _context.SetupProperty(c => c.InstanceServices, serviceProvider);

        _handleException.Setup(x => x.CreateValidationExceptionLog(It.IsAny<IEnumerable<RuleResultTree>>(), It.IsAny<ParticipantCsvRecord>()))
            .Returns(Task.FromResult(true)).Verifiable();

        _function = new StaticValidation(_logger.Object, _callFunction.Object, _handleException.Object, _createResponse);

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
        _callFunction.Verify(call => call.SendPost(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
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
        _callFunction.Verify(call => call.SendPost(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
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
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "9.NhsNumber")),
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
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "57.SupersededByNhsNumber")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }
    #endregion

    #region Record Type (Rule 8)
    [TestMethod]
    [DataRow("New")]
    [DataRow("Amended")]
    [DataRow("Removed")]
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
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "8.RecordType")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }
    #endregion

    #region Current Posting (Rule 58)
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("England")]
    [DataRow("Wales")]
    [DataRow("IoM")]
    public async Task Run_Should_Not_Create_Exception_When_CurrentPosting_Rule_Passes(string currentPosting)
    {
        // Arrange
        _participantCsvRecord.Participant.CurrentPosting = currentPosting;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "58.CurrentPosting")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow("Scotland")]
    public async Task Run_Should_Return_BadRequest_And_Create_Exception_When_CurrentPosting_Rule_Fails(string currentPosting)
    {
        // Arrange
        _participantCsvRecord.Participant.CurrentPosting = currentPosting;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "58.CurrentPosting")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }
    #endregion

    #region Previous Posting (Rule 59)
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("England")]
    [DataRow("Wales")]
    [DataRow("IoM")]
    public async Task Run_Should_Not_Create_Exception_When_PreviousPosting_Rule_Passes(string previousPosting)
    {
        // Arrange
        _participantCsvRecord.Participant.PreviousPosting = previousPosting;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "59.PreviousPosting")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow("Scotland")]
    public async Task Run_Should_Return_Created_And_Create_Exception_When_PreviousPosting_Rule_Fails(string previousPosting)
    {
        // Arrange
        _participantCsvRecord.Participant.PreviousPosting = previousPosting;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "59.PreviousPosting")),
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
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "14.ReasonForRemoval")),
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
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "14.ReasonForRemoval")),
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
    [DataRow(null)]
    [DataRow("")]
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
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "30.Postcode")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
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
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "3.PrimaryCareProviderAndReasonForRemoval")),
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
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "17.DateOfBirth")),
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
        _participantCsvRecord.Participant.Surname = familyName;
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
        _participantCsvRecord.Participant.Surname = familyName;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "39.FamilyName")),
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
        _participantCsvRecord.Participant.FirstName = firstName;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "40.FirstName")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }
    #endregion

    #region GP Practice Code (Rule 42)
    [TestMethod]
    [DataRow("New", "ABC")]
    [DataRow("Amended", null)]
    [DataRow("Removed", null)]
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
    [DataRow("New", null)]
    [DataRow("New", "")]
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
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "42.GPPracticeCode")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }
    #endregion

    #region Death Status (Rule 66)
    [TestMethod]
    [DataRow("Amended", Status.Formal, "DEA")]
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
    [DataRow("Amended", Status.Formal, null)]
    [DataRow("Amended", Status.Formal, "")]
    [DataRow("Amended", Status.Formal, "AFL")]
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
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "66.DeathStatus")),
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
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "19.ReasonForRemovalEffectiveFromDate")),
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
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "19.ReasonForRemovalEffectiveFromDate")),
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
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "18.DateOfDeath")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }
    #endregion

    #region New Participant with Reason For Removal, Removal Date or Date Of Death (Rule 47)
    [TestMethod]
    [DataRow("New", null, null, null)]
    [DataRow("New", "", "", "")]
    [DataRow("Amended", "DEA", "20240101", "20240101")]
    [DataRow("Removed", "DEA", "20240101", "20240101")]
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
    [DataRow("New", "DEA", null, null)]
    [DataRow("New", null, "20240101", null)]
    [DataRow("New", null, null, "20240101")]
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
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "47.NewParticipantWithRemovalOrDeath")),
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
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "61.InvalidFlag")),
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

}
