namespace NHS.CohortManager.Tests.ScreeningValidationServiceTests;

using System.Net;
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

[TestClass]
public class StaticValidationTests
{
    private readonly Mock<ILogger<StaticValidation>> _logger = new();
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly Mock<FunctionContext> _context = new();
    private readonly Mock<HttpRequestData> _request;
    private readonly ServiceCollection _serviceCollection = new();
    private readonly ParticipantCsvRecord _participantCsvRecord;
    private readonly StaticValidation _function;

    public StaticValidationTests()
    {
        Environment.SetEnvironmentVariable("CreateValidationExceptionURL", "CreateValidationExceptionURL");

        _request = new Mock<HttpRequestData>(_context.Object);

        var serviceProvider = _serviceCollection.BuildServiceProvider();

        _context.SetupProperty(c => c.InstanceServices, serviceProvider);

        _function = new StaticValidation(_logger.Object, _callFunction.Object);

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
    public async Task Run_Should_Return_BadRequest_When_Request_Body_Empty()
    {
        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _callFunction.Verify(call => call.SendPost(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    public async Task Run_Should_Return_BadRequest_When_Request_Body_Invalid()
    {
        // Arrange
        SetUpRequestBody("Invalid request body");

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _callFunction.Verify(call => call.SendPost(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
    }

    #region NhsNumber Rule Tests
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
        _callFunction.Verify(call => call.SendPost(
            It.Is<string>(s => s == "CreateValidationExceptionURL"),
            It.Is<string>(s => s.Contains("\"RuleId\":9") && s.Contains("\"RuleDescription\":\"NhsNumber\""))),
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
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _callFunction.Verify(call => call.SendPost(
            It.Is<string>(s => s == "CreateValidationExceptionURL"),
            It.Is<string>(s => s.Contains("\"RuleId\":9") && s.Contains("\"RuleDescription\":\"NhsNumber\""))),
            Times.Once());
    }
    #endregion

    #region SupersededByNhsNumber Rule Tests
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
        _callFunction.Verify(call => call.SendPost(
            It.Is<string>(s => s == "CreateValidationExceptionURL"),
            It.Is<string>(s => s.Contains("\"RuleId\":57") && s.Contains("\"RuleDescription\":\"SupersededByNhsNumber\""))),
            Times.Never());
    }

    [TestMethod]
    [DataRow("0")]
    [DataRow("999999999")]      // 9 digits
    [DataRow("12.3456789")]     // 9 digits and 1 non-digit
    [DataRow("12.34567899")]    // 10 digits and 1 non-digit
    [DataRow("10000000000")]    // 11 digits
    public async Task Run_Should_Return_BadRequest_And_Create_Exception_When_SupersededByNhsNumber_Rule_Fails(string supersededByNhsNumber)
    {
        // Arrange
        _participantCsvRecord.Participant.SupersededByNhsNumber = supersededByNhsNumber;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _callFunction.Verify(call => call.SendPost(
            It.Is<string>(s => s == "CreateValidationExceptionURL"),
            It.Is<string>(s => s.Contains("\"RuleId\":57") && s.Contains("\"RuleDescription\":\"SupersededByNhsNumber\""))),
            Times.Once());
    }
    #endregion

    #region RecordType Rule Tests
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
        _callFunction.Verify(call => call.SendPost(
            It.Is<string>(s => s == "CreateValidationExceptionURL"),
            It.Is<string>(s => s.Contains("\"RuleId\":8") && s.Contains("\"RuleDescription\":\"RecordType\""))),
            Times.Never());
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("Newish")]
    public async Task Run_Should_Return_BadRequest_And_Create_Exception_When_RecordType_Rule_Fails(string recordType)
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = recordType;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _callFunction.Verify(call => call.SendPost(
            It.Is<string>(s => s == "CreateValidationExceptionURL"),
            It.Is<string>(s => s.Contains("\"RuleId\":8") && s.Contains("\"RuleDescription\":\"RecordType\""))),
            Times.Once());
    }
    #endregion

    #region CurrentPosting Rule Tests
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
        _callFunction.Verify(call => call.SendPost(
            It.Is<string>(s => s == "CreateValidationExceptionURL"),
            It.Is<string>(s => s.Contains("\"RuleId\":58") && s.Contains("\"RuleDescription\":\"CurrentPosting\""))),
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
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _callFunction.Verify(call => call.SendPost(
            It.Is<string>(s => s == "CreateValidationExceptionURL"),
            It.Is<string>(s => s.Contains("\"RuleId\":58") && s.Contains("\"RuleDescription\":\"CurrentPosting\""))),
            Times.Once());
    }
    #endregion

    #region PreviousPosting Rule Tests
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
        _callFunction.Verify(call => call.SendPost(
            It.Is<string>(s => s == "CreateValidationExceptionURL"),
            It.Is<string>(s => s.Contains("\"RuleId\":59") && s.Contains("\"RuleDescription\":\"PreviousPosting\""))),
            Times.Never());
    }

    [TestMethod]
    [DataRow("Scotland")]
    public async Task Run_Should_Return_BadRequest_And_Create_Exception_When_PreviousPosting_Rule_Fails(string previousPosting)
    {
        // Arrange
        _participantCsvRecord.Participant.PreviousPosting = previousPosting;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _callFunction.Verify(call => call.SendPost(
            It.Is<string>(s => s == "CreateValidationExceptionURL"),
            It.Is<string>(s => s.Contains("\"RuleId\":59") && s.Contains("\"RuleDescription\":\"PreviousPosting\""))),
            Times.Once());
    }
    #endregion

    #region ReasonForRemoval Rule Tests
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
        _callFunction.Verify(call => call.SendPost(
            It.Is<string>(s => s == "CreateValidationExceptionURL"),
            It.Is<string>(s => s.Contains("\"RuleId\":14") && s.Contains("\"RuleDescription\":\"ReasonForRemoval\""))),
            Times.Never());
    }

    [TestMethod]
    [DataRow("ABC")]
    [DataRow("123")]
    public async Task Run_Should_Return_BadRequest_And_Create_Exception_When_ReasonForRemoval_Rule_Fails(string reasonForRemoval)
    {
        // Arrange
        _participantCsvRecord.Participant.ReasonForRemoval = reasonForRemoval;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _callFunction.Verify(call => call.SendPost(
            It.Is<string>(s => s == "CreateValidationExceptionURL"),
            It.Is<string>(s => s.Contains("\"RuleId\":14") && s.Contains("\"RuleDescription\":\"ReasonForRemoval\""))),
            Times.Once());
    }
    #endregion

    #region Postcode Rule Tests
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
        _callFunction.Verify(call => call.SendPost(
            It.Is<string>(s => s == "CreateValidationExceptionURL"),
            It.Is<string>(s => s.Contains("\"RuleId\":30") && s.Contains("\"RuleDescription\":\"Postcode\""))),
            Times.Never());
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("ABC123")]
    [DataRow("ABC 123")]
    public async Task Run_Should_Return_BadRequest_And_Create_Exception_When_Postcode_Rule_Fails(string postcode)
    {
        // Arrange
        _participantCsvRecord.Participant.Postcode = postcode;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _callFunction.Verify(call => call.SendPost(
            It.Is<string>(s => s == "CreateValidationExceptionURL"),
            It.Is<string>(s => s.Contains("\"RuleId\":30") && s.Contains("\"RuleDescription\":\"Postcode\""))),
            Times.Once());
    }
    #endregion

    #region PrimaryCareProviderAndReasonForRemoval Rule Tests
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
        _callFunction.Verify(call => call.SendPost(
            It.Is<string>(s => s == "CreateValidationExceptionURL"),
            It.Is<string>(s => s.Contains("\"RuleId\":3") && s.Contains("\"RuleDescription\":\"PrimaryCareProviderAndReasonForRemoval\""))),
            Times.Never());
    }

    [TestMethod]
    [DataRow(null, null)]
    [DataRow("ABC", "123")]
    public async Task Run_Should_Return_BadRequest_And_Create_Exception_When_PrimaryCareProviderAndReasonForRemoval_Rule_Fails(string primaryCareProvider, string reasonForRemoval)
    {
        // Arrange
        _participantCsvRecord.Participant.PrimaryCareProvider = primaryCareProvider;
        _participantCsvRecord.Participant.ReasonForRemoval = reasonForRemoval;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _callFunction.Verify(call => call.SendPost(
            It.Is<string>(s => s == "CreateValidationExceptionURL"),
            It.Is<string>(s => s.Contains("\"RuleId\":3") && s.Contains("\"RuleDescription\":\"PrimaryCareProviderAndReasonForRemoval\""))),
            Times.Once());
    }
    #endregion

    #region DateOfBirth Rule Tests
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
        _callFunction.Verify(call => call.SendPost(
            It.Is<string>(s => s == "CreateValidationExceptionURL"),
            It.Is<string>(s => s.Contains("\"RuleId\":17") && s.Contains("\"RuleDescription\":\"DateOfBirth\""))),
            Times.Never());
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("20700101")]   // In the future
    [DataRow("19700229")]   // Not a real date (1970 was not a leap year)
    [DataRow("1970023")]    // Incorrect format
    [DataRow("197013")]     // Not a real date or incorrect format
    public async Task Run_Should_Return_BadRequest_And_Create_Exception_When_DateOfBirth_Rule_Fails(string dateOfBirth)
    {
        // Arrange
        _participantCsvRecord.Participant.DateOfBirth = dateOfBirth;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _callFunction.Verify(call => call.SendPost(
            It.Is<string>(s => s == "CreateValidationExceptionURL"),
            It.Is<string>(s => s.Contains("\"RuleId\":17") && s.Contains("\"RuleDescription\":\"DateOfBirth\""))),
            Times.Once());
    }
    #endregion

    #region FamilyName Rule Tests
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
        _callFunction.Verify(call => call.SendPost(
            It.Is<string>(s => s == "CreateValidationExceptionURL"),
            It.Is<string>(s => s.Contains("\"RuleId\":39") && s.Contains("\"RuleDescription\":\"FamilyName\""))),
            Times.Never());
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public async Task Run_Should_Return_BadRequest_And_Create_Exception_When_FamilyName_Rule_Fails(string familyName)
    {
        // Arrange
        _participantCsvRecord.Participant.Surname = familyName;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _callFunction.Verify(call => call.SendPost(
            It.Is<string>(s => s == "CreateValidationExceptionURL"),
            It.Is<string>(s => s.Contains("\"RuleId\":39") && s.Contains("\"RuleDescription\":\"FamilyName\""))),
            Times.Once());
    }
    #endregion

    #region FirstName Rule Tests
    [TestMethod]
    [DataRow("Jo")]
    [DataRow("Jean-Luc")]
    [DataRow("Sarah Jane")]
    [DataRow("Bartholomew")]
    public async Task Run_Should_Not_Create_Exception_When_FirstName_Rule_Passes(string firstName)
    {
        // Arrange
        _participantCsvRecord.Participant.FirstName = firstName;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _callFunction.Verify(call => call.SendPost(
            It.Is<string>(s => s == "CreateValidationExceptionURL"),
            It.Is<string>(s => s.Contains("\"RuleId\":40") && s.Contains("\"RuleDescription\":\"FirstName\""))),
            Times.Never());
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public async Task Run_Should_Return_BadRequest_And_Create_Exception_When_FirstName_Rule_Fails(string firstName)
    {
        // Arrange
        _participantCsvRecord.Participant.FirstName = firstName;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _callFunction.Verify(call => call.SendPost(
            It.Is<string>(s => s == "CreateValidationExceptionURL"),
            It.Is<string>(s => s.Contains("\"RuleId\":40") && s.Contains("\"RuleDescription\":\"FirstName\""))),
            Times.Once());
    }
    #endregion

    #region GP Practice Code (Primary Care Provide) Rule Tests
    [TestMethod]
    [DataRow("New", "ABC")]
    [DataRow("Amended", null)]
    [DataRow("Removed", null)]
    public async Task Run_Should_Not_Create_Exception_When_GPPracticeCode_Rule_Passes(string recordType, string practiceCode)
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = recordType;
        _participantCsvRecord.Participant.PrimaryCareProvider = practiceCode;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _callFunction.Verify(call => call.SendPost(
            It.Is<string>(s => s == "CreateValidationExceptionURL"),
            It.Is<string>(s => s.Contains("\"RuleId\":42") && s.Contains("\"RuleDescription\":\"GPPracticeCode\""))),
            Times.Never());
    }

    [TestMethod]
    [DataRow("New", null)]
    [DataRow("New", "")]
    public async Task Run_Should_Return_BadRequest_And_Create_Exception_When_GPPracticeCode_Rule_Fails(string recordType, string practiceCode)
    {
        // Arrange
        _participantCsvRecord.Participant.RecordType = recordType;
        _participantCsvRecord.Participant.PrimaryCareProvider = practiceCode;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _callFunction.Verify(call => call.SendPost(
            It.Is<string>(s => s == "CreateValidationExceptionURL"),
            It.Is<string>(s => s.Contains("\"RuleId\":42") && s.Contains("\"RuleDescription\":\"GPPracticeCode\""))),
            Times.Once());
    }
    #endregion

    #region DeathStatus Rule Tests
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
        _callFunction.Verify(call => call.SendPost(
            It.Is<string>(s => s == "CreateValidationExceptionURL"),
            It.Is<string>(s => s.Contains("\"RuleId\":66") && s.Contains("\"RuleDescription\":\"DeathStatus\""))),
            Times.Never());
    }

    [TestMethod]
    [DataRow("Amended", Status.Formal, null)]
    [DataRow("Amended", Status.Formal, "")]
    [DataRow("Amended", Status.Formal, "AFL")]
    public async Task Run_Should_Return_BadRequest_And_Create_Exception_When_DeathStatus_Rule_Fails(string recordType, Status deathStatus, string reasonForRemoval)
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
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _callFunction.Verify(call => call.SendPost(
            It.Is<string>(s => s == "CreateValidationExceptionURL"),
            It.Is<string>(s => s.Contains("\"RuleId\":66") && s.Contains("\"RuleDescription\":\"DeathStatus\""))),
            Times.Once());
    }
    #endregion

    #region ReasonForRemovalEffectiveFromDate Rule Tests
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
        _callFunction.Verify(call => call.SendPost(
            It.Is<string>(s => s == "CreateValidationExceptionURL"),
            It.Is<string>(s => s.Contains("\"RuleId\":19") && s.Contains("\"RuleDescription\":\"ReasonForRemovalEffectiveFromDate\""))),
            Times.Never());
    }

    [TestMethod]
    [DataRow("20700101")]   // In the future
    [DataRow("19700229")]   // Not a real date (1970 was not a leap year)
    [DataRow("1970023")]    // Incorrect format
    [DataRow("197013")]     // Not a real date or incorrect format
    public async Task Run_Should_Return_BadRequest_And_Create_Exception_When_ReasonForRemovalEffectiveFromDate_Rule_Fails(string date)
    {
        // Arrange
        _participantCsvRecord.Participant.ReasonForRemovalEffectiveFromDate = date;
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _callFunction.Verify(call => call.SendPost(
            It.Is<string>(s => s == "CreateValidationExceptionURL"),
            It.Is<string>(s => s.Contains("\"RuleId\":19") && s.Contains("\"RuleDescription\":\"ReasonForRemovalEffectiveFromDate\""))),
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
