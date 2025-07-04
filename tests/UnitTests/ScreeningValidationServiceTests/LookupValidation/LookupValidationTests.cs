
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
        string filename;
        switch (ruleType)
        {
            case "LookupRules":
                filename = "Breast_Screening_lookupRules.json";
                break;
            case "CohortRules":
                filename = "Breast_Screening_cohortRules.json";
                break;
            default:
                filename = "Breast_Screening_lookupRules.json";
                break;
        }

        // Try various paths for the rules files
        string[] possiblePaths = new[]
        {
            // Relative paths with different nesting depths
            Path.Combine("../../../../../../../application/CohortManager/src/Functions/ScreeningValidationService/LookupValidation", filename),
            Path.Combine("../../../../../application/CohortManager/src/Functions/ScreeningValidationService/LookupValidation", filename),
            Path.Combine("../../../application/CohortManager/src/Functions/ScreeningValidationService/LookupValidation", filename),
            Path.Combine("../../application/CohortManager/src/Functions/ScreeningValidationService/LookupValidation", filename),
            Path.Combine("../../../../application/CohortManager/src/Functions/ScreeningValidationService/LookupValidation", filename),
            Path.Combine("../../../../../../../../../application/CohortManager/src/Functions/ScreeningValidationService/LookupValidation", filename),

            // Try with the ScreeningValidationService root directory
            Path.Combine("../../../../../application/CohortManager/src/Functions/ScreeningValidationService", filename),
            Path.Combine("../../../application/CohortManager/src/Functions/ScreeningValidationService", filename)
        };

        // Try to find the file in any of the possible locations
        string jsonContent = null;
        foreach (string relativePath in possiblePaths)
        {
            try
            {
                string fullPath = Path.GetFullPath(relativePath);
                if (File.Exists(fullPath))
                {
                    jsonContent = File.ReadAllText(fullPath);
                    Console.WriteLine($"Found rules file at: {fullPath}");
                    break;
                }
            }
            catch
            {
                // Ignore any errors and try the next path
            }
        }

        // If file wasn't found, check for an embedded resource
        if (jsonContent == null)
        {
            // Get the directory of the currently executing assembly
            string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string assemblyDirectory = Path.GetDirectoryName(assemblyLocation) ?? string.Empty;

            // Try to find in a TestData directory
            string testDataPath = Path.Combine(assemblyDirectory, "TestData", filename);
            if (File.Exists(testDataPath))
            {
                jsonContent = File.ReadAllText(testDataPath);
                Console.WriteLine($"Found rules file in TestData directory: {testDataPath}");
            }
        }

        _readRules.Setup(x => x.GetRulesFromDirectory(It.IsAny<string>()))
            .Returns(Task.FromResult(jsonContent));

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
    [DataRow("RDR", null, null)] // postcode and primary care provider null
    [DataRow("RDI", "not valid", "E85121")] // postcode invalid
    [DataRow("RPR", "BN20 1PH", "not valid")] // PCP invalid
    public async Task Run_invalidParticipant_ValidateBsoCodeRuleFails(string ReasonForRemoval, string postcode,
                                                                    string primaryCareProvider)
    {
        // Arrange
        SetupRules("CohortRules");
        _requestBody.NewParticipant.PrimaryCareProvider = primaryCareProvider;
        _requestBody.NewParticipant.Postcode = postcode;
        _requestBody.NewParticipant.ReasonForRemoval = ReasonForRemoval;
        _requestBody.NewParticipant.RecordType = "AMENDED";
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _sut.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _exceptionHandler.Verify(handleException =>
            handleException.CreateValidationExceptionLog(
                It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "54.ValidateBsoCode.NBO.NonFatal")),
                It.IsAny<ParticipantCsvRecord>()),
            Times.AtLeastOnce());
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
        _exceptionHandler.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "11.ValidateReasonForRemoval.NBO.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
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
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "58.CurrentPosting.NBO.NonFatal")),
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
    [DataRow("ENG", "InvalidPCP")]
    public async Task Run_CurrentPostingAndPrimaryProvider_CreatesException(string currentPosting, string primaryCareProvider)
    {
        // Arrange
        SetupRules("LookupRules");
        _requestBody.NewParticipant.CurrentPosting = currentPosting;
        _requestBody.NewParticipant.PrimaryCareProvider = primaryCareProvider;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _lookupValidation.Setup(x => x.CheckIfPrimaryCareProviderExists(It.IsAny<string>())).Returns(false);

        // Act
        await _sut.RunAsync(_request.Object);

        // Assert
        _exceptionHandler.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "3602.CurrentPostingAndPrimaryProvider.BSSelect.NonFatal")),
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
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "3601.ValidatePrimaryCareProvider.BSSelect.NonFatal")),
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
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "3601.ValidatePrimaryCareProvider.BSSelect.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow(Actions.Amended, "LDN")]
    [DataRow(Actions.Amended, "R/C")]
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
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "11.ValidateReasonForRemoval.NBO.NonFatal")),
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
    [DataRow("RDR", "", "", Actions.Amended)]
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
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "54.ValidateBsoCode.NBO.NonFatal")),
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

    [DataRow("DMS", "ValidPCP", "", null, "ExcludedPCP", "WALES")] // Valid -> Wales Invalid (Null current posting)
    [DataRow(null, "ValidPCP", "WALES", "ABC", "ExcludedPCP", "ENGLAND")] // Wales Invalid -> Valid (Null current posting)
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
    [DataRow("CYM", "ValidPCP", "WALES", "CYM", "ValidPCP", "WALES")] //Wales Invalid -> Wales Invalid
    [DataRow("CYM", "ExcludedPCP", "WALES", "CYM", "ExcludedPCP", "WALES")] //Wales Invalid -> Wales Invalid
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
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "51.ParticipantLocationRemainingOutsideOfCohort.ParticipantLocationRemainingOutsideOfCohort.NonFatal")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }

    [TestMethod]
    public async Task Run_BlockedParticipant_CreatesException()
    {
        // Arrange
        SetupRules("LookupRules");
        _requestBody.ExistingParticipant.BlockedFlag = "1";
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        await _sut.RunAsync(_request.Object);

        // Assert
        _exceptionHandler.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "12.BlockedParticipant.BSSelect.Fatal")),
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
