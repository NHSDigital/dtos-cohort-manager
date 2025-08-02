
namespace NHS.CohortManager.Tests.UnitTests.ScreeningValidationServiceTests;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Model;
using Moq;
using NHS.CohortManager.ScreeningValidationService;
using Microsoft.Extensions.Logging.Abstractions;
using NHS.CohortManager.Tests.TestUtils;

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
    private readonly Mock<IDataLookupFacadeBreastScreening> _lookupValidation = new();

    [TestInitialize]
    public void IntialiseTests()
    {
        // Function setup
        Environment.SetEnvironmentVariable("CreateValidationExceptionURL", "CreateValidationExceptionURL");
        _request = new Mock<HttpRequestData>(_context.Object);
        var serviceProvider = _serviceCollection.BuildServiceProvider();
        _context.SetupProperty(c => c.InstanceServices, serviceProvider);


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
            FamilyName = "Smith",
            ScreeningName = "Breast Screening"
        };

        _requestBody = new LookupValidationRequestBody(existingParticipant, newParticipant, "caas.csv");
        _request.Setup(r => r.CreateResponse()).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(_context.Object);
            response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
            response.SetupProperty(r => r.StatusCode);
            response.SetupProperty(r => r.Body, new MemoryStream());
            return response.Object;
        });

        _sut = new LookupValidation(_createResponse, _exceptionHandler.Object, _mockLogger.Object, _lookupValidation.Object, new ReadRules(new NullLogger<ReadRules>()));
    }

    [TestMethod]
    public async Task Run_EmptyRequest_ReturnBadRequest()
    {
        // Act
        var response = await _sut.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Run_InvalidRequest_ReturnBadRequest()
    {
        // Arrange
        SetUpRequestBody("Invalid request body");

        // Act
         var response = await _sut.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    [DataRow("RDR", null, null)] // postcode and primary care provider null
    [DataRow("RDI", "not valid", "E85121")] // postcode invalid
    [DataRow("RPR", "BN20 1PH", "not valid")] // PCP invalid
    public async Task Run_invalidParticipant_ValidateBsoCodeRuleFails(string ReasonForRemoval, string postcode,
                                                                    string primaryCareProvider)
    {
        // Arrange
        _requestBody.NewParticipant.PrimaryCareProvider = primaryCareProvider;
        _requestBody.NewParticipant.Postcode = postcode;
        _requestBody.NewParticipant.ReasonForRemoval = ReasonForRemoval;
        _requestBody.NewParticipant.RecordType = "AMENDED";
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var response = await _sut.RunAsync(_request.Object);
        string body = await AssertionHelper.ReadResponseBodyAsync(response);

        // Assert
        StringAssert.Contains(body, "54.ValidateBsoCode.NBO.NonFatal");
    }

    [TestMethod]
    [DataRow("TNR", "not valid", "not valid")] // Reason for removal is not valid
    [DataRow("RDI", "TR2 7FG", "Y02688")] // All fields are valid
    [DataRow("RDR", "ZZZTR2 7FG", null)] // Postcode starts with ZZZ
    public async Task Run_validParticipant_ReturnNoContent(string primaryCareProvider, string postcode,
                                                                    string ReasonForRemoval)
    {
        // Arrange
        _requestBody.NewParticipant.PrimaryCareProvider = primaryCareProvider;
        _requestBody.NewParticipant.Postcode = postcode;
        _requestBody.NewParticipant.ReasonForRemoval = ReasonForRemoval;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var response = await _sut.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(response.StatusCode, HttpStatusCode.NoContent);
    }

    [TestMethod]
    [DataRow("ValidCurrentPosting")]
    [DataRow(null)]
    public async Task Run_CurrentPosting_ReturnNoContent(string currentPosting)
    {
        // Arrange
        _requestBody.NewParticipant.CurrentPosting = currentPosting;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _lookupValidation.Setup(x => x.CheckIfCurrentPostingExists(It.IsAny<string>())).Returns(currentPosting == "ValidCurrentPosting");

        // Act
        var response = await _sut.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(response.StatusCode, HttpStatusCode.NoContent);
    }

    [TestMethod]
    [DataRow("InvalidCurrentPosting", "InvalidPCP")]
    [DataRow("ValidCurrentPosting", "ValidPCP")]
    [DataRow("InvalidCurrentPosting", "ValidPCP")]
    [DataRow("ValidCurrentPosting", null)]
    [DataRow("InvalidCurrentPosting", null)]
    public async Task Run_CurrentPostingAndPrimaryProvider_ReturnNoContent(string currentPosting, string primaryCareProvider)
    {
        // Arrange
        _requestBody.NewParticipant.CurrentPosting = currentPosting;
        _requestBody.NewParticipant.PrimaryCareProvider = primaryCareProvider;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _lookupValidation.Setup(x => x.ValidatePostingCategories(It.IsAny<string>())).Returns(currentPosting == "ValidCurrentPosting");
        _lookupValidation.Setup(x => x.CheckIfPrimaryCareProviderExists(It.IsAny<string>())).Returns(primaryCareProvider == "ValidPCP");

        // Act
        var response = await _sut.RunAsync(_request.Object);
        string body = await AssertionHelper.ReadResponseBodyAsync(response);

        // Assert
        Assert.IsFalse(body.Contains("45.GPPracticeCodeDoesNotExist.BSSelect.NonFatal"));
    }

    [TestMethod]
    [DataRow("InvalidPCP")]
    public async Task Run_ValidatePrimaryCareProvider_ReturnValidationException(string primaryCareProvider)
    {
        // Arrange
        _requestBody.NewParticipant.PrimaryCareProvider = primaryCareProvider;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _lookupValidation.Setup(x => x.CheckIfPrimaryCareProviderExists(It.IsAny<string>())).Returns(false);

        // Act
        var response = await _sut.RunAsync(_request.Object);
        string body = await AssertionHelper.ReadResponseBodyAsync(response);

        // Assert
        StringAssert.Contains(body, "3601.ValidatePrimaryCareProvider.BSSelect.NonFatal");
    }

    [TestMethod]
    [DataRow("ValidPCP")]
    [DataRow(null)]
    public async Task Run_ValidatePrimaryCareProvider_ReturnNoContent(string primaryCareProvider)
    {
        // Arrange
        _requestBody.NewParticipant.PrimaryCareProvider = primaryCareProvider;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _lookupValidation.Setup(x => x.CheckIfPrimaryCareProviderExists(It.IsAny<string>())).Returns(primaryCareProvider == "ValidPCP");

        // Act
        var response = await _sut.RunAsync(_request.Object);
        string body = await AssertionHelper.ReadResponseBodyAsync(response);

        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
    }

    #region Validate BSO Code (Rule 54)
    [TestMethod]
    [DataRow("RPR", "", "", Actions.Amended)]
    [DataRow("RDR", "", "", Actions.Amended)]
    public async Task Run_AmendedRFRParticipantHasInvalidPostcodeAndGpPractice_ReturnValidationException(string reasonForRemoval, string primaryCareProvider, string postcode, string recordType)
    {
        // Arrange
        _requestBody.NewParticipant.ReasonForRemoval = reasonForRemoval;
        _requestBody.NewParticipant.PrimaryCareProvider = primaryCareProvider;
        _requestBody.NewParticipant.Postcode = postcode;
        _requestBody.NewParticipant.RecordType = recordType;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var response = await _sut.RunAsync(_request.Object);
        string body = await AssertionHelper.ReadResponseBodyAsync(response);

        // Assert
        StringAssert.Contains(body, "54.ValidateBsoCode.NBO.NonFatal");
    }

    [TestMethod]
    [DataRow("RDI", "ValidPCP", "ValidPostcode", Actions.Amended)]
    [DataRow("RDR", "", "ValidPostcode", Actions.Amended)]
    [DataRow("ABC", "ValidPostcode", "", Actions.Amended)]
    [DataRow("RPR", "", "", Actions.New)]
    public async Task Run_AmendedParticipantHasValidBSO_ReturnNoContent(string reasonForRemoval, string primaryCareProvider, string postcode, string recordType)
    {
        // Arrange
        _requestBody.NewParticipant.ReasonForRemoval = reasonForRemoval;
        _requestBody.NewParticipant.PrimaryCareProvider = primaryCareProvider;
        _requestBody.NewParticipant.Postcode = postcode;
        _requestBody.NewParticipant.RecordType = recordType;

        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);
        _lookupValidation
            .Setup(x => x.CheckIfPrimaryCareProviderExists(It.IsAny<string>()))
            .Returns(primaryCareProvider == "ValidPCP");
        _lookupValidation
            .Setup(x => x.ValidateOutcode(It.IsAny<string>()))
            .Returns(postcode == "ValidPostcode");

        // Act
        var response = await _sut.RunAsync(_request.Object);
        string body = await AssertionHelper.ReadResponseBodyAsync(response);

        // Assert
        Assert.IsFalse(body.Contains("54.ValidateBsoCode.NBO.NonFatal"));
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
    public async Task Run_ParticipantLocationRemainingOutsideOfCohort_ReturnNoContent(string existingCurrentPosting, string existingPrimaryCareProvider, string existingPostingCategory, string newCurrentPosting, string newPrimaryCareProvider, string newPostingCategory)
    {
        // Arrange
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
        var response = await _sut.RunAsync(_request.Object);
        string body = await AssertionHelper.ReadResponseBodyAsync(response);

        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
    }

    [TestMethod]
    [DataRow("DMS", "ExcludedPCP", "ENGLAND", "DMS", "ExcludedPCP", "ENGLAND")] //DMS Invalid -> DMS Invalid
    [DataRow("DMS", "ExcludedPCP", "WALES", "DMS", "ExcludedPCP", "WALES")] //DMS + Wales Invalid -> DMS Wales Invalid
    [DataRow("CYM", "ValidPCP", "WALES", "CYM", "ValidPCP", "WALES")] //Wales Invalid -> Wales Invalid
    [DataRow("CYM", "ExcludedPCP", "WALES", "CYM", "ExcludedPCP", "WALES")] //Wales Invalid -> Wales Invalid
    [DataRow("DMS", "ExcludedPCP", "ENGLAND", "ABC", "ValidPCP", "WALES")] //DMS Invalid -> Wales Invalid
    public async Task Run_ParticipantLocationRemainingOutsideOfCohort_ReturnValidationException(string existingCurrentPosting, string existingPrimaryCareProvider, string existingPostingCategory, string newCurrentPosting, string newPrimaryCareProvider, string newPostingCategory)
    {
        // Arrange
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
        var response = await _sut.RunAsync(_request.Object);
        string body = await AssertionHelper.ReadResponseBodyAsync(response);

        // Assert
        StringAssert.Contains(body, "51.ParticipantLocationRemainingOutsideOfCohort.ParticipantLocationRemainingOutsideOfCohort.NonFatal");
    }

    [TestMethod]
    [DataRow("DMS", "Z00000")]
    [DataRow("ENG", "Z00000")] 
    [DataRow("IM", "Z00000")] 
    public async Task Run_ParticipantPrimaryCareProviderDoesNotExistAndNotInExcludedSMU_ReturnValidationException(string newCurrentPosting, string newPrimaryCareProvider)
    {
        // Arrange
        _requestBody.NewParticipant.RecordType = Actions.New;
        _requestBody.NewParticipant.CurrentPosting = newCurrentPosting;
        _requestBody.NewParticipant.PrimaryCareProvider = newPrimaryCareProvider;
        

        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _lookupValidation.Setup(x => x.RetrievePostingCategory(newCurrentPosting)).Returns(newCurrentPosting);
        _lookupValidation.Setup(x => x.CheckIfCurrentPostingExists(newCurrentPosting)).Returns(true);
        _lookupValidation.Setup(x => x.CheckIfPrimaryCareProviderInExcludedSmuList(newPrimaryCareProvider)).Returns(false);
        _lookupValidation.Setup(x => x.CheckIfPrimaryCareProviderExists(newPrimaryCareProvider)).Returns(false);

        // Act
        var response = await _sut.RunAsync(_request.Object);
        string body = await AssertionHelper.ReadResponseBodyAsync(response);

        // Assert
        StringAssert.Contains(body, "45.GPPracticeCodeDoesNotExist.BSSelect.NonFatal");
    }

    [TestMethod]
    public async Task Run_BlockedParticipant_ReturnValidationException()
    {
        // Arrange
        _requestBody.ExistingParticipant.BlockedFlag = "1";
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var response = await _sut.RunAsync(_request.Object);
        string body = await AssertionHelper.ReadResponseBodyAsync(response);

        // Assert
        StringAssert.Contains(body, "12.BlockedParticipant.BSSelect.Fatal");
    }

    private void SetUpRequestBody(string json)
    {
        var byteArray = Encoding.ASCII.GetBytes(json);
        var bodyStream = new MemoryStream(byteArray);

        _request.Setup(r => r.Body).Returns(bodyStream);
    }
}
