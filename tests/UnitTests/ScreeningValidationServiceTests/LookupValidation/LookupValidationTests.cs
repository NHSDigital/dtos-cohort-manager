
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
using Data.Database;
using Microsoft.Extensions.Options;

[TestClass]
public class LookupValidationTests
{
    private readonly Mock<FunctionContext> _context = new();
    private Mock<HttpRequestData> _request;
    private readonly Mock<IExceptionHandler> _exceptionHandler = new();
    private readonly CreateResponse _createResponse = new();
    private readonly ServiceCollection _serviceCollection = new();
    private LookupValidationRequestBody _requestBody;
    private LookupValidation _sut;
    private readonly Mock<ILogger<LookupValidation>> _mockLogger = new();
    private readonly Mock<IReadRules> _readRules = new();
    private readonly Mock<IDataLookupFacadeBreastScreening> _lookupValidation = new();

    private readonly Mock<IOptions<LookupValidationConfig>> _lookupValidationConfig = new();

    [TestInitialize]
    public void IntialiseTests()
    {
        // Function setup
        Environment.SetEnvironmentVariable("CreateValidationExceptionURL", "CreateValidationExceptionURL");
        _request = new Mock<HttpRequestData>(_context.Object);
        var serviceProvider = _serviceCollection.BuildServiceProvider();
        _context.SetupProperty(c => c.InstanceServices, serviceProvider);
        _exceptionHandler.Setup(x => x.CreateValidationExceptionLog(It.IsAny<IEnumerable<RuleResultTree>>(), It.IsAny<ParticipantCsvRecord>()))
            .Returns(Task.FromResult(new ValidationExceptionLog()
            {
                IsFatal = true,
                CreatedException = true
            }));
        LookupValidationConfig lookupValidationConfig = new LookupValidationConfig
        {
            ExceptionFunctionUrl = "CreateValidationExceptionURL"
        };
        _lookupValidationConfig.Setup(x => x.Value).Returns(lookupValidationConfig);


        _lookupValidation.Setup(x => x.CheckIfPrimaryCareProviderExists(It.IsAny<string>())).Returns(true);
        _lookupValidation.Setup(x => x.ValidateLanguageCode(It.IsAny<string>())).Returns(true);
        _lookupValidation.Setup(x => x.CheckIfCurrentPostingExists(It.IsAny<string>())).Returns(true);

        // Test data setup
        var existingParticipant = new Participant
        {
            NhsNumber = "9876543210",
            FirstName = "John",
            FamilyName = "Smith",
            CurrentPosting = "DMS"
        };
        var newParticipant = new Participant
        {
            NhsNumber = "9876543210",
            FirstName = "John",
            FamilyName = "Smith"
        };

        _requestBody = new LookupValidationRequestBody(existingParticipant, newParticipant, "caas.csv", RulesType.CohortDistribution);
        _request.Setup(r => r.CreateResponse()).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(_context.Object);
            response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
            response.SetupProperty(r => r.StatusCode);
            response.SetupProperty(r => r.Body, new MemoryStream());
            return response.Object;
        });
    }

    private void SetupRules(string ruleType)
    {
        string json;
        switch (ruleType)
        {
            case "LookupRules":
                json = File.ReadAllText("../../../../../../../application/CohortManager/src/Functions/ScreeningValidationService/LookupValidation/Breast_Screening_lookupRules.json");
                break;
            case "CohortRules":
                json = File.ReadAllText("../../../../../../../application/CohortManager/src/Functions/ScreeningValidationService/LookupValidation/Breast_Screening_cohortRules.json");
                break;
            default:
                json = File.ReadAllText("../../../../../../../application/CohortManager/src/Functions/ScreeningValidationService/LookupValidation/Breast_Screening_lookupRules.json");
                break;
        }
        _readRules.Setup(x => x.GetRulesFromDirectory(It.IsAny<string>()))
        .Returns(Task.FromResult<string>(json));
        _sut = new LookupValidation(_createResponse, _exceptionHandler.Object, _mockLogger.Object, _lookupValidation.Object, _readRules.Object);


    }

    [TestMethod]
    public async Task Run_EmptyRequest_ReturnBadRequest()
    {
        // Arrange
        SetupRules("LookupRules");

        // Act
        var result = await _sut.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _exceptionHandler.Verify(handler => handler.CreateValidationExceptionLog(
            It.IsAny<IEnumerable<RuleResultTree>>(),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    public async Task Run_InvalidRequest_ReturnBadRequest()
    {
        // Arrange
        SetupRules("LookupRules");
        SetUpRequestBody("Invalid request body");

        // Act
        var result = await _sut.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _exceptionHandler.Verify(handler => handler.CreateValidationExceptionLog(
            It.IsAny<IEnumerable<RuleResultTree>>(),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow(Actions.Amended, "")]
    [DataRow(Actions.Amended, null)]
    [DataRow(Actions.Amended, " ")]
    [DataRow(Actions.Removed, "")]
    [DataRow(Actions.Removed, null)]
    [DataRow(Actions.Removed, " ")]
    public async Task Run_NullNhsNumber_ReturnCreatedAndCreateException(string recordType, string nhsNumber)
    {
        // Arrange
        SetupRules("LookupRules");
        _requestBody.NewParticipant.RecordType = recordType;
        _requestBody.ExistingParticipant.NhsNumber = nhsNumber;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _sut.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _exceptionHandler.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "22.ParticipantMustExist.Fatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }

    [TestMethod]
    [DataRow(Actions.New, "")]
    [DataRow(Actions.New, null)]
    [DataRow(Actions.New, " ")]
    [DataRow(Actions.Amended, "0000000000")]
    [DataRow(Actions.Amended, "9999999999")]
    [DataRow(Actions.Removed, "0000000000")]
    [DataRow(Actions.Removed, "9999999999")]
    public async Task Run_Should_Not_Create_Exception_When_ParticipantMustExist_Rule_Passes(string recordType, string nhsNumber)
    {
        // Arrange
        SetupRules("LookupRules");
        _requestBody.NewParticipant.RecordType = recordType;
        _requestBody.ExistingParticipant.NhsNumber = nhsNumber;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        await _sut.RunAsync(_request.Object);

        // Assert
        _exceptionHandler.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "22.ParticipantMustExist")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow("0000000000")]
    [DataRow("9999999999")]
    public async Task Run_Should_Return_Created_And_Create_Exception_When_ParticipantMustNotExist_Rule_Fails(string nhsNumber)
    {
        // Arrange
        SetupRules("LookupRules");
        _requestBody.NewParticipant.RecordType = Actions.New;
        _requestBody.ExistingParticipant.NhsNumber = nhsNumber;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _sut.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _exceptionHandler.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "47.ParticipantMustNotExist.Fatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }

    [TestMethod]
    [DataRow(Actions.New, "")]
    [DataRow(Actions.New, null)]
    [DataRow(Actions.New, " ")]
    [DataRow(Actions.Amended, "0000000000")]
    [DataRow(Actions.Amended, "9999999999")]
    [DataRow(Actions.Removed, "0000000000")]
    [DataRow(Actions.Removed, "9999999999")]
    public async Task Run_Should_Not_Create_Exception_When_ParticipantMustNotExist_Rule_Passes(string recordType, string nhsNumber)
    {
        // Arrange
        SetupRules("LookupRules");
        _requestBody.NewParticipant.RecordType = recordType;
        _requestBody.ExistingParticipant.NhsNumber = nhsNumber;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        await _sut.RunAsync(_request.Object);

        // Assert
        _exceptionHandler.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "47.ParticipantMustNotExist")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow("Smith", Gender.Female, "19700101", "Jones", Gender.Male, "19700101")]     // New Family Name & Gender
    [DataRow("Smith", Gender.Female, "19700101", "Jones", Gender.Female, "19700102")]   // New Family Name & Date of Birth
    [DataRow("Smith", Gender.Female, "19700101", "Smith", Gender.Male, "19700102")]     // New Gender & Date of Birth
    [DataRow("Smith", Gender.Female, "19700101", "Jones", Gender.Male, "19700102")]     // New Family Name, Gender & Date of Birth
    public async Task Run_MultipleDemographicsFieldsChanged_DemographicsRuleFails(
    string existingFamilyName, Gender existingGender, string existingDateOfBirth, string newFamilyName,
    Gender newGender, string newDateOfBirth)
    {
        // Arrange
        SetupRules("CohortRules");
        _requestBody.NewParticipant.RecordType = Actions.Amended;
        _requestBody.ExistingParticipant.FamilyName = existingFamilyName;
        _requestBody.ExistingParticipant.ParticipantId = "1234567";
        _requestBody.ExistingParticipant.Gender = existingGender;
        _requestBody.ExistingParticipant.DateOfBirth = existingDateOfBirth;
        _requestBody.NewParticipant.FamilyName = newFamilyName;
        _requestBody.NewParticipant.Gender = newGender;
        _requestBody.NewParticipant.DateOfBirth = newDateOfBirth;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _sut.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _exceptionHandler.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "35.TooManyDemographicsFieldsChanged.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }


    [TestMethod]
    [DataRow(Actions.Amended, "Smith", Gender.Female, "19700101", "Jones", Gender.Female, "19700101")]  // New Family Name Only
    [DataRow(Actions.Amended, "Smith", Gender.Female, "19700101", "Smith", Gender.Male, "19700101")]    // New Gender Only
    [DataRow(Actions.Amended, "Smith", Gender.Female, "19700101", "Smith", Gender.Female, "19700102")]  // New Date of Birth Only
    [DataRow(Actions.Amended, "Smith", Gender.Female, "19700101", "Smith", Gender.Female, "19700101")]  // No Change
    [DataRow(Actions.New, "", new Gender(), "", "Smith", Gender.Female, "19700101")]                    // New Record Type
    public async Task Run_OneFieldChanged_DemographicsRulePasses(string recordType,
        string existingFamilyName, Gender existingGender, string existingDateOfBirth, string newFamilyName,
        Gender newGender, string newDateOfBirth)
    {
        // Arrange
        SetupRules("CohortRules");
        _requestBody.NewParticipant.RecordType = recordType;
        _requestBody.ExistingParticipant.FamilyName = existingFamilyName;
        _requestBody.ExistingParticipant.ParticipantId = "1234567";
        _requestBody.ExistingParticipant.Gender = existingGender;
        _requestBody.ExistingParticipant.DateOfBirth = existingDateOfBirth;
        _requestBody.NewParticipant.FamilyName = newFamilyName;
        _requestBody.NewParticipant.Gender = newGender;
        _requestBody.NewParticipant.DateOfBirth = newDateOfBirth;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        await _sut.RunAsync(_request.Object);

        // Assert
        _exceptionHandler.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "35.TooManyDemographicsFieldsChanged")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow("RDR", null, null)] // postcode and primary care provider null
    public async Task Run_invalidParticipant_ValidateBsoCodeRuleFails(string ReasonForRemoval, string postcode,
                                                                    string primaryCareProvider)
    {
        // Arrange
        SetupRules("LookupRules");
        _requestBody.NewParticipant.PrimaryCareProvider = primaryCareProvider;
        _requestBody.NewParticipant.Postcode = postcode;
        _requestBody.NewParticipant.ReasonForRemoval = ReasonForRemoval;
        _requestBody.NewParticipant.RecordType = "ADD";
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _sut.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
    }

    [TestMethod]
    [DataRow("TNR", "not valid", "not valid")] // Reason for removal is not valid
    [DataRow("RDI", "TR2 7FG", "Y02688")] // All fields are valid
    [DataRow("RDR", "ZZZTR2 7FG", null)] // Postcode starts with ZZZ
    public async Task Run_validParticipant_ValidateBsoCodeRulePasses(string primaryCareProvider, string postcode,
                                                                    string ReasonForRemoval)
    {
        // Arrange
        SetupRules("CohortRules");
        _requestBody.NewParticipant.PrimaryCareProvider = primaryCareProvider;
        _requestBody.NewParticipant.Postcode = postcode;
        _requestBody.NewParticipant.ReasonForRemoval = ReasonForRemoval;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _sut.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_CurrentPosting_CreatesException()
    {
        // Arrange
        SetupRules("LookupRules");
        _requestBody.NewParticipant.CurrentPosting = "InvalidCurrentPosting";
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _lookupValidation.Setup(x => x.CheckIfCurrentPostingExists(It.IsAny<string>())).Returns(false);

        // Act
        await _sut.RunAsync(_request.Object);

        // Assert
        _exceptionHandler.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "58.CurrentPosting.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }

    [TestMethod]
    [DataRow("ValidCurrentPosting")]
    [DataRow(null)]
    public async Task Run_CurrentPosting_DoesNotCreateException(string currentPosting)
    {
        // Arrange
        SetupRules("LookupRules");
        _requestBody.NewParticipant.CurrentPosting = currentPosting;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _lookupValidation.Setup(x => x.CheckIfCurrentPostingExists(It.IsAny<string>())).Returns(currentPosting == "ValidCurrentPosting");

        // Act
        await _sut.RunAsync(_request.Object);

        // Assert
        _exceptionHandler.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "58.CurrentPosting.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow("ENG", null)]
    public async Task Run_CurrentPostingAndPrimaryProvider_CreatesException(string currentPosting, string primaryCareProvider)
    {
        // Arrange
        SetupRules("LookupRules");
        _requestBody.NewParticipant.CurrentPosting = currentPosting;
        _requestBody.NewParticipant.PrimaryCareProvider = primaryCareProvider;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _lookupValidation.Setup(x => x.CheckIfPrimaryCareProviderExists(It.IsAny<string>())).Returns(primaryCareProvider == "ValidPCP");

        // Act
        await _sut.RunAsync(_request.Object);

        // Assert
        _exceptionHandler.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "3602.CurrentPostingAndPrimaryProvider.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }

    [TestMethod]
    [DataRow("InvalidCurrentPosting", "InvalidPCP")]
    [DataRow("ValidCurrentPosting", "ValidPCP")]
    [DataRow("InvalidCurrentPosting", "ValidPCP")]
    [DataRow("ValidCurrentPosting", null)]
    [DataRow("InvalidCurrentPosting", null)]
    public async Task Run_CurrentPostingAndPrimaryProvider_DoesNotCreateException(string currentPosting, string primaryCareProvider)
    {
        // Arrange
        SetupRules("LookupRules");
        _requestBody.NewParticipant.CurrentPosting = currentPosting;
        _requestBody.NewParticipant.PrimaryCareProvider = primaryCareProvider;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _lookupValidation.Setup(x => x.ValidatePostingCategories(It.IsAny<string>())).Returns(currentPosting == "ValidCurrentPosting");
        _lookupValidation.Setup(x => x.CheckIfPrimaryCareProviderExists(It.IsAny<string>())).Returns(primaryCareProvider == "ValidPCP");

        // Act
        await _sut.RunAsync(_request.Object);

        // Assert
        _exceptionHandler.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "3602.CurrentPostingAndPrimaryProvider.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow("InvalidPCP")]
    public async Task Run_ValidatePrimaryCareProvider_CreatesException(string primaryCareProvider)
    {
        // Arrange
        SetupRules("LookupRules");
        _requestBody.NewParticipant.PrimaryCareProvider = primaryCareProvider;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _lookupValidation.Setup(x => x.CheckIfPrimaryCareProviderExists(It.IsAny<string>())).Returns(false);

        // Act
        var result = await _sut.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _exceptionHandler.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "3601.ValidatePrimaryCareProvider.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }

    [TestMethod]
    [DataRow("ValidPCP")]
    [DataRow(null)]
    public async Task Run_ValidatePrimaryCareProvider_DoesNotCreateException(string primaryCareProvider)
    {
        // Arrange
        SetupRules("LookupRules");
        _requestBody.NewParticipant.PrimaryCareProvider = primaryCareProvider;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _lookupValidation.Setup(x => x.CheckIfPrimaryCareProviderExists(It.IsAny<string>())).Returns(primaryCareProvider == "ValidPCP");

        // Act
        var result = await _sut.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _exceptionHandler.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "36.ValidatePrimaryCareProvider.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow(Actions.Amended, "LDN")]
    [DataRow(Actions.Amended, "R/C")]
    [DataRow(Actions.Removed, "LDN")]
    [DataRow(Actions.Removed, "R/C")]
    public async Task Run_ValidateReasonForRemoval_CreatesException(string recordType, string reasonForRemoval)
    {
        // Arrange
        SetupRules("LookupRules");
        _requestBody.NewParticipant.RecordType = recordType;
        _requestBody.ExistingParticipant.ReasonForRemoval = reasonForRemoval;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        await _sut.RunAsync(_request.Object);

        // Assert
        _exceptionHandler.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "11.ValidateReasonForRemoval.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }

    [TestMethod]
    [DataRow(Actions.Amended, "XXX")]
    [DataRow(Actions.Removed, "XXX")]
    [DataRow(Actions.New, "LDN")]
    [DataRow(Actions.New, "R/C")]
    [DataRow(Actions.New, "")]
    public async Task Run_ValidateReasonForRemoval_DoesNotCreateException(string recordType, string reasonForRemoval)
    {
        // Arrange
        SetupRules("LookupRules");
        _requestBody.NewParticipant.RecordType = recordType;
        _requestBody.ExistingParticipant.ReasonForRemoval = reasonForRemoval;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        await _sut.RunAsync(_request.Object);

        // Assert
        _exceptionHandler.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "11.ValidateReasonForRemoval.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }


    #region Validate BSO Code (Rule 54)
    [TestMethod]
    [DataRow("RPR", "", "", Actions.Amended)]
    [DataRow("RDR", "ZZZPCP", "", Actions.Amended)]

    public async Task Run_AmendedRFRParticipantHasInvalidPostcodeAndGpPractice_ThrowsException(string reasonForRemoval, string primaryCareProvider, string postcode, string recordType)
    {
        // Arrange
        SetupRules("CohortRules");
        _requestBody.NewParticipant.ReasonForRemoval = reasonForRemoval;
        _requestBody.NewParticipant.PrimaryCareProvider = primaryCareProvider;
        _requestBody.NewParticipant.Postcode = postcode;
        _requestBody.NewParticipant.RecordType = recordType;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        await _sut.RunAsync(_request.Object);

        // Assert
        _exceptionHandler.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "54.ValidateBsoCode.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }

    [TestMethod]
    [DataRow("RDI", "ValidPCP", "AL1 1BB", Actions.Amended)]
    [DataRow("RDR", "", "AL3 0AX", Actions.Amended)]
    [DataRow("ABC", "ZZZPCP", "", Actions.Amended)]
    [DataRow("RPR", "", "", Actions.New)]

    public async Task Run_AmendedParticipantHasValidBSO_NoExceptionIsRaised(string reasonForRemoval, string primaryCareProvider, string postcode, string recordType)
    {
        // Arrange
        SetupRules("CohortRules");

        _requestBody.NewParticipant.ReasonForRemoval = reasonForRemoval;
        _requestBody.NewParticipant.PrimaryCareProvider = primaryCareProvider;
        _requestBody.NewParticipant.Postcode = postcode;
        _requestBody.NewParticipant.RecordType = recordType;

        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);
        _lookupValidation.Setup(x => x.CheckIfPrimaryCareProviderExists(It.IsAny<string>())).Returns(primaryCareProvider == "ValidPCP");

        // Act
        await _sut.RunAsync(_request.Object);

        // Assert
        _exceptionHandler.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "54.ValidateBsoCode.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }
    #endregion

    [TestMethod]
    [DataRow("DMS", "ValidPCP", "", "ABC", "ExcludedPCP", "")] //Valid -> Valid
    [DataRow("ABC", null, "ABC", "ABC", null, "ENGLAND")]//  Valid -> Valid
    [DataRow("ABC", "ExcludedPCP", "ABC", "ABC", "ExcludedPCP", "ENGLAND")] //Valid -> Valid

    [DataRow("DMS", "ExcludedPCP", "", "ABC", "ValidPCP", "ENGLAND")] // DMS Invalid -> Valid
    [DataRow("ABC", "ValidPCP", "", "DMS", "ExcludedPCP", "ENGLAND")] // Valid -> DMS Invalid

    [DataRow("DMS", "ValidPCP", "", "ABC", "ExcludedPCP", "WALES")] // Valid -> Wales Invalid
    [DataRow("CYM", "ValidPCP", "WALES", "ABC", "ExcludedPCP", "ENGLAND")] // Wales Invalid -> Valid
    public async Task Run_ParticipantLocationRemainingOutsideOfCohort_ShouldNotThrowException(string existingCurrentPosting, string existingPrimaryCareProvider, string existingPostingCategory, string newCurrentPosting, string newPrimaryCareProvider, string newPostingCategory)
    {
        // Arrange
        SetupRules("CohortRules");

        _requestBody.NewParticipant.RecordType = Actions.Amended;
        _requestBody.NewParticipant.CurrentPosting = newCurrentPosting;
        _requestBody.NewParticipant.PrimaryCareProvider = newPrimaryCareProvider;
        _requestBody.ExistingParticipant.CurrentPosting = existingCurrentPosting;
        _requestBody.ExistingParticipant.PrimaryCareProvider = existingPrimaryCareProvider;

        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _lookupValidation.Setup(x => x.CheckIfPrimaryCareProviderInExcludedSmuList(newPrimaryCareProvider)).Returns(newPrimaryCareProvider == "ExcludedPCP");
        _lookupValidation.Setup(x => x.CheckIfPrimaryCareProviderInExcludedSmuList(existingPrimaryCareProvider)).Returns(existingPrimaryCareProvider == "ExcludedPCP");
        _lookupValidation.Setup(x => x.RetrievePostingCategory(newCurrentPosting)).Returns(newPostingCategory);
        _lookupValidation.Setup(x => x.RetrievePostingCategory(existingCurrentPosting)).Returns(existingPostingCategory);

        // Act
        await _sut.RunAsync(_request.Object);

        // Assert
        _exceptionHandler.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "51.ParticipantLocationRemainingOutsideOfCohort.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow("DMS", "ExcludedPCP", "ENGLAND", "DMS", "ExcludedPCP", "ENGLAND")] //DMS Invalid -> DMS Invalid
    [DataRow("DMS", "ExcludedPCP", "WALES", "DMS", "ExcludedPCP", "WALES")] //DMS + Wales Invalid -> DMS Wales Invalid
    [DataRow("CYM","ValidPCP","WALES","CYM","ValidPCP","WALES")] //Wales Invalid -> Wales Invalid
    [DataRow("CYM","ExcludedPCP","WALES","CYM","ExcludedPCP","WALES")] //Wales Invalid -> Wales Invalid
    [DataRow("DMS", "ExcludedPCP", "ENGLAND", "ABC", "ValidPCP", "WALES")] //DMS Invalid -> Wales Invalid
    public async Task Run_ParticipantLocationRemainingOutsideOfCohort_ShouldThrowException(string existingCurrentPosting, string existingPrimaryCareProvider, string existingPostingCategory, string newCurrentPosting, string newPrimaryCareProvider, string newPostingCategory)
    {
        // Arrange
        SetupRules("CohortRules");

        _requestBody.NewParticipant.RecordType = Actions.Amended;
        _requestBody.NewParticipant.CurrentPosting = newCurrentPosting;
        _requestBody.NewParticipant.PrimaryCareProvider = newPrimaryCareProvider;
        _requestBody.ExistingParticipant.CurrentPosting = existingCurrentPosting;
        _requestBody.ExistingParticipant.PrimaryCareProvider = existingPrimaryCareProvider;

        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _lookupValidation.Setup(x => x.CheckIfPrimaryCareProviderInExcludedSmuList(newPrimaryCareProvider)).Returns(newPrimaryCareProvider == "ExcludedPCP");
        _lookupValidation.Setup(x => x.CheckIfPrimaryCareProviderInExcludedSmuList(existingPrimaryCareProvider)).Returns(existingPrimaryCareProvider == "ExcludedPCP");
        _lookupValidation.Setup(x => x.RetrievePostingCategory(newCurrentPosting)).Returns(newPostingCategory);
        _lookupValidation.Setup(x => x.RetrievePostingCategory(existingCurrentPosting)).Returns(existingPostingCategory);

        // Act
        await _sut.RunAsync(_request.Object);

        // Assert
        _exceptionHandler.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "51.ParticipantLocationRemainingOutsideOfCohort.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }
    private void SetUpRequestBody(string json)
    {
        var byteArray = Encoding.ASCII.GetBytes(json);
        var bodyStream = new MemoryStream(byteArray);

        _request.Setup(r => r.Body).Returns(bodyStream);
    }
}
