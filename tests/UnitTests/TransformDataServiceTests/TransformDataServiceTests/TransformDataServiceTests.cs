namespace NHS.CohortManager.Tests.TransformDataServiceTests;

using System.Net;
using System.Text;
using System.Text.Json;
using NHS.CohortManager.CohortDistributionService;
using NHS.CohortManager.Tests.TestUtils;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;
using Model;
using Common;
using Microsoft.Data.SqlClient;
using System.Data;
using Data.Database;
using Model.Enums;
using DataServices.Client;
using System.Linq.Expressions;
using System.Globalization;

[TestClass]
public class TransformDataServiceTests
{
    private readonly Mock<ILogger<TransformDataService>> _logger = new();
    private readonly Mock<FunctionContext> _context = new();
    private readonly Mock<HttpRequestData> _request;
    private readonly TransformDataRequestBody _requestBody;
    private readonly TransformDataService _function;
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<IExceptionHandler> _handleException = new();
    private readonly Mock<ITransformDataLookupFacade> _transformLookups = new();
    private readonly ITransformReasonForRemoval _transformReasonForRemoval;

    public TransformDataServiceTests()
    {
        _request = new Mock<HttpRequestData>(_context.Object);

        CohortDistributionParticipant requestParticipant = new()
        {
            NhsNumber = "1",
            FirstName = "John",
            FamilyName = "Smith",
            NamePrefix = "MR",
            Gender = Gender.Male,
            ReferralFlag = false
        };

        CohortDistribution databaseParticipant = new()
        {
            NHSNumber = 1,
            GivenName = "John",
            FamilyName = "Smith",
            NamePrefix = "MR",
            Gender = 1
        };

        _requestBody = new TransformDataRequestBody()
        {
            Participant = requestParticipant,
            ExistingParticipant = databaseParticipant,
            ServiceProvider = "1"
        };

        _transformLookups.Setup(x => x.ValidateOutcode(It.IsAny<string>())).Returns(true);
        _transformLookups.Setup(x => x.GetBsoCode(It.IsAny<string>())).Returns("ELD");
        _transformLookups.Setup(x => x.GetBsoCodeUsingPCP(It.IsAny<string>())).Returns("ELD");
        _transformLookups.Setup(x => x.ValidateLanguageCode(It.IsAny<string>())).Returns(true);

        _transformReasonForRemoval = new TransformReasonForRemoval(_handleException.Object, _transformLookups.Object);
        _function = new TransformDataService(_createResponse.Object, _handleException.Object, _logger.Object, _transformReasonForRemoval, _transformLookups.Object);

        _request.Setup(r => r.CreateResponse()).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(_context.Object);
            response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
            response.SetupProperty(r => r.StatusCode);
            response.SetupProperty(r => r.Body, new MemoryStream());
            return response.Object;
        });

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string ResponseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                response.WriteString(ResponseBody);
                return response;
            });
    }

    [TestMethod]
    public async Task Run_EmptyRequest_ReturnBadRequest()
    {
        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_InvalidRequest_ReturnBadRequest()
    {
        // Arrange
        SetUpRequestBody("Invalid request body");

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ParticipantReferred_DoNotRunRoutineRules()
    {
        // Arrange
        _requestBody.Participant.ReferralFlag = true;

        // Breaks rule InvalidFlagTrueAndNoPrimaryCareProvider in the routine rules
        _requestBody.Participant.PrimaryCareProvider = "G82650";
        _requestBody.Participant.ReasonForRemovalEffectiveFromDate = DateTime.UtcNow.Date.ToString();
        _requestBody.Participant.InvalidFlag = "1";
        _requestBody.Participant.RecordType = Actions.New;

        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        _handleException
            .Verify(i => i.CreateTransformExecutedExceptions(
                It.IsAny<CohortDistributionParticipant>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                null),
            times: Times.Never);
    }

    [TestMethod]
    public async Task Run_ParticipantReferred_RunCommonRules()
    {
        // Arrange
        // Should trigger truncate rule
        _requestBody.Participant.AddressLine1 = new string('A', 36);
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        _handleException
            .Verify(i => i.CreateTransformExecutedExceptions(It.IsAny<CohortDistributionParticipant>(), "TruncateAddressLine1ExceedsMaximumLength", It.IsAny<int>(), null), times: Times.Once);
    }

    [TestMethod]
    [DataRow("ADMIRAL", "ADM", 38)]
    [DataRow("AIR MARSHAL", "A.ML", 37)]
    [DataRow("AIR MARSHAL", "A.ML", 37)]
    [DataRow("HIS ROYAL HIGHNESS", "HRH", 54)]
    [DataRow("RIGHT REV", "R.RV", 70)]
    [DataRow("COUNT", "R.HN", 67)]
    public async Task Run_TransformNamePrefix_ReturnTransformedPrefix(string namePrefix, string expectedTransformedPrefix, int ruleId)
    {
        // Arrange
        _requestBody.Participant.NamePrefix = namePrefix;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);
        var expectedResponse = new CohortDistributionParticipant
        {
            NhsNumber = "1",
            FirstName = "John",
            FamilyName = "Smith",
            NamePrefix = expectedTransformedPrefix,
            Gender = Gender.Male,
            ReferralFlag = false
        };

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _handleException.Verify(i => i.CreateTransformExecutedExceptions(It.IsAny<CohortDistributionParticipant>(), It.IsAny<string>(), ruleId,ExceptionCategory.TransformExecuted), times: Times.Once);
    }

    [TestMethod]
    [DataRow("Smith", Gender.Female, "19700101", "Jones", Gender.Male, "19700101")]     // New Family Name & Gender
    [DataRow("Smith", Gender.Female, "19700101", "Jones", Gender.Female, "19700102")]   // New Family Name & Date of Birth
    [DataRow("Smith", Gender.Female, "19700101", "Smith", Gender.Male, "19700102")]     // New Gender & Date of Birth
    [DataRow("Smith", Gender.Female, "19700101", "Jones", Gender.Male, "19700102")]     // New Family Name, Gender & Date of Birth
    public async Task Run_TooManyDemographicsFieldsChanged_DemographicsRuleFails_LogsException(string existingFamilyName, Gender existingGender, string existingDateOfBirth, string newFamilyName, Gender newGender, string newDateOfBirth)
    {
        // Arrange
        _requestBody.Participant.RecordType = Actions.Amended;
        _requestBody.ExistingParticipant.FamilyName = existingFamilyName;
        _requestBody.ExistingParticipant.ParticipantId = 1234567;
        _requestBody.ExistingParticipant.Gender = (short)(Gender)existingGender;
        _requestBody.ExistingParticipant.DateOfBirth = DateTime.ParseExact(existingDateOfBirth, "yyyyMMdd", CultureInfo.InvariantCulture);
        _requestBody.Participant.FamilyName = newFamilyName;
        _requestBody.Participant.Gender = newGender;
        _requestBody.Participant.DateOfBirth = newDateOfBirth;

        var json = JsonSerializer.Serialize(_requestBody);
        var ruleId = 35;
        var ruleName = "TooManyDemographicsFieldsChangedConfusionNoTransformation";
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _handleException.Verify(i => i.CreateTransformExecutedExceptions(It.IsAny<CohortDistributionParticipant>(), ruleName, ruleId,null), times: Times.Once);
    }

    [TestMethod]
    [DataRow("Smith", Gender.Female, "19700101", "Jones", Gender.Female, "19700101")]  // New Family Name Only
    [DataRow("Smith", Gender.Female, "19700101", "Smith", Gender.Male, "19700101")]    // New Gender Only
    [DataRow("Smith", Gender.Female, "19700101", "Smith", Gender.Female, "19700102")]  // New Date of Birth Only
    [DataRow("Smith", Gender.Female, "19700101", "Smith", Gender.Female, "19700101")]  // No Change
    public async Task Run_OneFieldChanged_DemographicsRulePasses_NoExceptionLogs(string existingFamilyName, Gender existingGender, string existingDateOfBirth, string newFamilyName, Gender newGender, string newDateOfBirth)
    {
        // Arrange
        _requestBody.Participant.RecordType = Actions.Amended;
        _requestBody.ExistingParticipant.FamilyName = existingFamilyName;
        _requestBody.ExistingParticipant.ParticipantId = 1234567;
        _requestBody.ExistingParticipant.Gender = (short)(Gender)existingGender;
        _requestBody.ExistingParticipant.DateOfBirth = DateTime.ParseExact(existingDateOfBirth, "yyyyMMdd", CultureInfo.InvariantCulture);
        _requestBody.Participant.FamilyName = newFamilyName;
        _requestBody.Participant.Gender = newGender;
        _requestBody.Participant.DateOfBirth = newDateOfBirth;

        var json = JsonSerializer.Serialize(_requestBody);
        var ruleId = 35;
        var ruleName = "TooManyDemographicsFieldsChangedConfusionNoTransformation";
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _handleException.Verify(i => i.CreateTransformExecutedExceptions(It.IsAny<CohortDistributionParticipant>(), ruleName, ruleId,null), times: Times.Never);
    }


    [TestMethod]
    public async Task Run_InvalidNamePrefix_SetPrefixToNull()
    {
        // Arrange
        _requestBody.Participant.NamePrefix = "Not a valid name prefix";
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);
        var expectedResponse = new CohortDistributionParticipant
        {
            NhsNumber = "1",
            FirstName = "John",
            FamilyName = "Smith",
            NamePrefix = null,
            Gender = Gender.Male,
            ReferralFlag = false,
        };

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _handleException.Verify(i => i.CreateTransformExecutedExceptions(It.IsAny<CohortDistributionParticipant>(), It.IsAny<string>(), 83,ExceptionCategory.TransformExecuted), times: Times.Once);
    }

    [TestMethod]
    public async Task Run_TransformNamePrefixwithTrailingChars_ReturnTransformedPrefix()
    {
        // Arrange
        _requestBody.Participant.NamePrefix = "DRS";
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);
        var expectedResponse = new CohortDistributionParticipant
        {
            NhsNumber = "1",
            FirstName = "John",
            FamilyName = "Smith",
            NamePrefix = "DR",
            Gender = Gender.Male,
            ReferralFlag = false
        };

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        var responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _handleException.Verify(i => i.CreateTransformExecutedExceptions(It.IsAny<CohortDistributionParticipant>(), It.IsAny<string>(), 47, ExceptionCategory.TransformExecuted), times: Times.Once);
    }


    [TestMethod]
    public async Task Run_StringFieldsTooLong_TruncateFields()
    {
        // Arrange
        _requestBody.Participant = new CohortDistributionParticipant
        {
            NhsNumber = "123456789",
            FirstName = new string('A', 36),
            FamilyName = new string('A', 36),
            OtherGivenNames = new string('A', 105),
            PreviousFamilyName = new string('A', 36),
            AddressLine1 = new string('A', 36),
            AddressLine2 = new string('A', 36),
            AddressLine3 = new string('A', 36),
            AddressLine4 = new string('A', 36),
            AddressLine5 = new string('A', 36),
            Postcode = new string('A', 36),
            TelephoneNumber = new string('A', 33),
            MobileNumber = new string('A', 33),
            EmailAddress = new string('A', 91),
            ReferralFlag = false
        };
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);
        var expectedResponse = new CohortDistributionParticipant
        {
            NhsNumber = "123456789",
            FirstName = new string('A', 35),
            FamilyName = new string('A', 35),
            OtherGivenNames = new string('A', 100),
            PreviousFamilyName = new string('A', 35),
            AddressLine1 = new string('A', 35),
            AddressLine2 = new string('A', 35),
            AddressLine3 = new string('A', 35),
            AddressLine4 = new string('A', 35),
            AddressLine5 = new string('A', 35),
            Postcode = new string('A', 35),
            TelephoneNumber = new string('A', 32),
            MobileNumber = new string('A', 32),
            EmailAddress = new string('A', 90),
            ReferralFlag = false
        };

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        _handleException.Verify(i => i.CreateTransformExecutedExceptions(It.IsAny<CohortDistributionParticipant>(), It.IsAny<string>(), It.IsAny<int>(),null), times: Times.Exactly(13));
    }

    //TODO: This test needs fixing as it doesnt test anything.
    public void GetAddress_AddressFieldsBlankPostcodeNotNull_ReturnAddress()
    {
        // Arrange
        var participant = new CohortDistributionParticipant()
        {
            Postcode = "RG2 5TX"
        };

        var mockConnection = new Mock<SqlConnection>();
        var mockCommand = new Mock<SqlCommand>();
        var mockReader = new Mock<SqlDataReader>();

        mockReader.Setup(r => r.Read()).Returns(true);
        mockReader.Setup(r => r.GetString(0)).Returns("RG2 5TX");
        mockReader.Setup(r => r.GetString(1)).Returns("51 something av");

        mockCommand.Setup(c => c.ExecuteReader()).Returns(mockReader.Object);
        mockConnection.Setup(c => c.Open()).Verifiable();

        // Act
        var sut = new GetMissingAddress(participant, mockConnection.Object);
        var result = sut.GetAddress();

        // Assert
        var expectedResponse = new CohortDistributionParticipant
        {
            Postcode = "RG2 5TX",
            AddressLine1 = "51 something av"
        };

        Assert.AreEqual("51 something av", expectedResponse.AddressLine1);

    }

    [TestMethod]
    //[DataRow("John.,-()/='+:?!\"%&;<>*", "John.,-()/='+:?!\"%&;<>*")]
    [DataRow("abby{}", "abby()")]
    [DataRow("abc_", "abc-")]
    [DataRow("abc\\", "abc/")]
    [DataRow("{[Smith£$^`~#@_|\\]}", "((Smith   '   -:/))")]
    public async Task Run_InvalidCharsInParticipant_ReturnTransformedFields(string name, string transformedName)
    {
        // Arrange
        _requestBody.Participant.FamilyName = name;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);
        var expectedResponse = new CohortDistributionParticipant
        {
            NhsNumber = "1",
            FirstName = "John",
            FamilyName = transformedName,
            NamePrefix = "MR",
            Gender = Gender.Male,
            ReferralFlag = false
        };

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _handleException.Verify(i => i.CreateTransformExecutedExceptions(It.IsAny<CohortDistributionParticipant>(), "CharacterRules", 71,null), times: Times.Once);

    }


    [TestMethod]
    public async Task Run_RfrIsDeaAndDateOfDeathIsNull_SetDateOfDeathToRfrDate()
    {
        // Arrange
        _requestBody.Participant.ReasonForRemoval = "DEA";
        _requestBody.Participant.ReasonForRemovalEffectiveFromDate = "2/10/2024";

        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);
        var expectedResponse = new CohortDistributionParticipant
        {
            NhsNumber = "1",
            FirstName = "John",
            FamilyName = "Smith",
            NamePrefix = "MR",
            Gender = Gender.Male,
            ReferralFlag = false,
            ReasonForRemoval = "DEA",
            ReasonForRemovalEffectiveFromDate = "2/10/2024",
            DateOfDeath = "2/10/2024"
        };

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _handleException.Verify(i => i.CreateTransformExecutedExceptions(It.IsAny<CohortDistributionParticipant>(), "OtherDateOfDeathDoesNotExist", 3, null), times: Times.Once);

    }

    [TestMethod]
    public async Task Run_AmendRecordHasNullName_GetPreviousNameFromDb()
    {
        // Arrange
        _requestBody.Participant.RecordType = Actions.Amended;
        _requestBody.Participant.FirstName = null;

        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);
        var expectedResponse = new CohortDistributionParticipant
        {
            RecordType = Actions.Amended,
            NhsNumber = "1",
            FirstName = "John",
            FamilyName = "Smith",
            NamePrefix = "MR",
            Gender = Gender.Male,
            ReferralFlag = false
        };

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        _handleException.Verify(i => i.CreateTransformExecutedExceptions(It.IsAny<CohortDistributionParticipant>(), "OtherFirstNameDoesNotExist", 14,null), times: Times.Once);
    }

    [TestMethod]
    [DataRow("G82650", "1", Actions.New)]
    [DataRow("G82650", "1", Actions.Amended)]
    [DataRow("", "0", Actions.Removed)]
    public async Task Run_InvalidParticipantHasPrimaryCareProvider_TransformFields(string primaryCareProvider, string invalidFlag, string recordType)
    {
        // Arrange
        _requestBody.Participant.PrimaryCareProvider = primaryCareProvider;
        _requestBody.Participant.ReasonForRemovalEffectiveFromDate = DateTime.UtcNow.Date.ToString();
        _requestBody.Participant.InvalidFlag = invalidFlag;
        _requestBody.Participant.RecordType = recordType;

        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);
        var expectedResponse = new CohortDistributionParticipant
        {
            RecordType = recordType,
            NhsNumber = "1",
            FirstName = "John",
            FamilyName = "Smith",
            NamePrefix = "MR",
            Gender = Gender.Male,
            ReferralFlag = false,
            ReasonForRemoval = "ORR",
            ReasonForRemovalEffectiveFromDate = DateTime.UtcNow.Date.ToString("yyyyMMdd"),
            PrimaryCareProvider = "",
            InvalidFlag = invalidFlag
        };

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        _handleException.Verify(i => i.CreateTransformExecutedExceptions(It.IsAny<CohortDistributionParticipant>(), "OtherInvalidFlagTrueAndNoPrimaryCareProvider", 0,null), times: Times.Once);

    }

    [TestMethod]
    public async Task Run_DateOfDeathSuppliedAndReasonForRemovalIsNotDea_SetDateOfDeathToNull()
    {
        // Arrange
        _requestBody.Participant.ReasonForRemoval = "NOTDEA";
        _requestBody.Participant.DateOfDeath = "2024-01-01";

        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);
        var expectedResponse = new CohortDistributionParticipant
        {
            NhsNumber = "1",
            FirstName = "John",
            FamilyName = "Smith",
            NamePrefix = "MR",
            Gender = Gender.Male,
            ReferralFlag = false,
            ReasonForRemoval = "NOTDEA",
            DateOfDeath = null,
        };

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_DateOfDeathSuppliedAndReasonForRemovalIsDea_ShouldNotChangeDateOfDeath()
    {
        // Arrange
        _requestBody.Participant.ReasonForRemoval = "DEA";
        _requestBody.Participant.DateOfDeath = "2024-01-01";

        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);
        var expectedResponse = new CohortDistributionParticipant
        {
            NhsNumber = "1",
            FirstName = "John",
            FamilyName = "Smith",
            NamePrefix = "MR",
            Gender = Gender.Male,
            ReferralFlag = false,
            ReasonForRemoval = "DEA",
            DateOfDeath = "2024-01-01",
        };

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_SupersededNhsNumberNotNull_TransformAndRaiseException()
    {
        // Arrange
        _requestBody.Participant.SupersededByNhsNumber = "1234567890";
        _requestBody.Participant.RecordType = Actions.Amended;

        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);
        var expectedResponse = new CohortDistributionParticipant
        {
            RecordType = Actions.Amended,
            NhsNumber = "1",
            SupersededByNhsNumber = "1234567890",
            FirstName = "John",
            FamilyName = "Smith",
            NamePrefix = "MR",
            Gender = Gender.Male,
            ReferralFlag = false,
            PrimaryCareProvider = "",
            ReasonForRemoval = "ORR",
            ReasonForRemovalEffectiveFromDate = DateTime.UtcNow.Date.ToString("yyyyMMdd")
        };

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        _handleException
            .Verify(i => i.CreateTransformExecutedExceptions(It.IsAny<CohortDistributionParticipant>(), "OtherSupersededNhsNumber", 60,null),
            times: Times.Once);
    }

    [TestMethod]
    public async Task Run_DelRecord_TransformRfrAndRaiseException()
    {
        // Arrange
        CohortDistributionParticipant participant = new()
        {
            RecordType = Actions.Removed,
            NhsNumber = "1234567890",
            EligibilityFlag = "0",
            InvalidFlag = "1"
        };
        _requestBody.Participant = participant;

        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        StringAssert.Contains(responseBody, "ORR");
        _handleException
            .Verify(i => i.CreateTransformExecutedExceptions(It.IsAny<CohortDistributionParticipant>(), "RecordDeleted", 0, null),
            times: Times.Once);
    }

    private void SetUpRequestBody(string json)
    {
        var byteArray = Encoding.ASCII.GetBytes(json);
        var bodyStream = new MemoryStream(byteArray);

        _request.Setup(r => r.Body).Returns(bodyStream);
    }
}
