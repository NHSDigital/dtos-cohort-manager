namespace NHS.CohortManager.UnitTests.Tests.TransformDataServiceTests;

using System.Net;
using System.Text;
using System.Text.Json;
using NHS.CohortManager.CohortDistribution;
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
    private readonly Mock<IBsTransformationLookups> _transformationLookups = new();
    private readonly Mock<ITransformReasonForRemoval> _transformReasonForRemoval = new();

    public TransformDataServiceTests()
    {
        _request = new Mock<HttpRequestData>(_context.Object);

        _requestBody = new TransformDataRequestBody()
        {
            Participant = new CohortDistributionParticipant
            {
                NhsNumber = "1",
                FirstName = "John",
                FamilyName = "Smith",
                NamePrefix = "MR",
                Gender = Gender.Male
            },
            ServiceProvider = "1"
        };

        _transformationLookups.Setup(x => x.GetGivenName(It.IsAny<string>())).Returns("A first name");
        _transformationLookups.Setup(x => x.GetFamilyName(It.IsAny<string>())).Returns("A last name");

        _function = new TransformDataService(_createResponse.Object, _handleException.Object, _logger.Object, _transformationLookups.Object, _transformReasonForRemoval.Object);

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
    [DataRow("ADMIRAL", "ADM")]
    [DataRow("AIR MARSHAL", "A.ML")]
    [DataRow("HIS ROYAL HIGHNESS", "HRH")]
    [DataRow("BRIG", "BRIG")]
    public async Task Run_TransformNamePrefix_ReturnTransformedPrefix(string namePrefix, string expectedTransformedPrefix)
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
            Gender = Gender.Male
        };
        _transformReasonForRemoval.Setup(x => x.ReasonForRemovalTransformations(It.IsAny<CohortDistributionParticipant>())).Returns(Task.FromResult(expectedResponse));

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
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
            Gender = Gender.Male
        };
        _transformReasonForRemoval.Setup(x => x.ReasonForRemovalTransformations(It.IsAny<CohortDistributionParticipant>())).Returns(Task.FromResult(expectedResponse));

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
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
        };
        _transformReasonForRemoval.Setup(x => x.ReasonForRemovalTransformations(It.IsAny<CohortDistributionParticipant>())).Returns(Task.FromResult(expectedResponse));

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        var responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }


    [TestMethod]
    public async Task Run_StringFieldsTooLong_TruncateFields()
    {
        // Arrange
        _requestBody.Participant = new CohortDistributionParticipant
        {
            NamePrefix = new string('A', 36),
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
            EmailAddress = new string('A', 91)
        };
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);
        var expectedResponse = new CohortDistributionParticipant
        {
            NamePrefix = null,
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
            Gender = Gender.NotSpecified
        };
        _transformReasonForRemoval.Setup(x => x.ReasonForRemovalTransformations(It.IsAny<CohortDistributionParticipant>())).Returns(Task.FromResult(expectedResponse));

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_Should_Transform_Participant_Data_When_Gender_IsNot_0_1_2_or_9()
    {
        // Arrange
        _requestBody.Participant.Gender = (Gender)4;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);
        var expectedResponse = new CohortDistributionParticipant
        {
            NhsNumber = "1",
            FirstName = "John",
            FamilyName = "Smith",
            NamePrefix = "MR",
            Gender = Gender.NotSpecified,
        };
        _transformReasonForRemoval.Setup(x => x.ReasonForRemovalTransformations(It.IsAny<CohortDistributionParticipant>())).Returns(Task.FromResult(expectedResponse));

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        result.Body.Position = 0;
        var reader = new StreamReader(result.Body, Encoding.UTF8);
        var responseBody = await reader.ReadToEndAsync();
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_Should_Not_Transform_Participant_Gender_When_Gender_Is_0_1_2_or_9()
    {
        // Arrange
        _requestBody.Participant.Gender = Gender.Male;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);
        var expectedResponse = new CohortDistributionParticipant
        {
            NhsNumber = "1",
            FirstName = "John",
            FamilyName = "Smith",
            NamePrefix = "MR",
            Gender = Gender.Male,
        };
        _transformReasonForRemoval.Setup(x => x.ReasonForRemovalTransformations(It.IsAny<CohortDistributionParticipant>())).Returns(Task.FromResult(expectedResponse));

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

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
    [DataRow("John.,-()/='+:?!\"%&;<>*", "John.,-()/='+:?!\"%&;<>*")]
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
            Gender = Gender.Male
        };
        _transformReasonForRemoval.Setup(x => x.ReasonForRemovalTransformations(It.IsAny<CohortDistributionParticipant>())).Returns(Task.FromResult(expectedResponse));

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }


    [DataRow("Bob\\E\\")]
    [DataRow("Bob\\T\\")]
    [DataRow("Bob€")]
    public async Task Run_ExceptionalCharsInParticipant_RaisesException(string name)
    {
        // Arrange
        var sut = new TransformString();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => sut.CheckParticipantCharactersAsync(name));
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
            ReasonForRemoval = "DEA",
            ReasonForRemovalEffectiveFromDate = "2/10/2024",
            DateOfDeath = "2/10/2024"
        };
        _transformReasonForRemoval.Setup(x => x.ReasonForRemovalTransformations(It.IsAny<CohortDistributionParticipant>())).Returns(Task.FromResult(expectedResponse));

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
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
            FirstName = "A first name",
            FamilyName = "Smith",
            NamePrefix = "MR",
            Gender = Gender.Male
        };
        _transformReasonForRemoval.Setup(x => x.ReasonForRemovalTransformations(It.IsAny<CohortDistributionParticipant>())).Returns(Task.FromResult(expectedResponse));

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
    }

    [TestMethod]
    [DataRow("G82650", "1", Actions.New)]
    [DataRow("G82650", "1", Actions.Amended)]
    [DataRow("", "0", Actions.Removed)]
    public async Task Run_InvalidParticipantHasPrimaryCareProvider_TransformFields(string primaryCareProvider, string invalidFlag, string recordType)
    {
        // Arrange
        _requestBody.Participant.PrimaryCareProvider = primaryCareProvider;
        _requestBody.Participant.ReasonForRemovalEffectiveFromDate = DateTime.Today.ToString();
        _requestBody.Participant.InvalidFlag = invalidFlag;
        _requestBody.Participant.RecordType = recordType;

        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);
        var expectedResponse = new CohortDistributionParticipant
        {
            NhsNumber = "1",
            FirstName = "John",
            FamilyName = "Smith",
            NamePrefix = "MR",
            Gender = Gender.Male,
            ReasonForRemoval = "ORR",
            ReasonForRemovalEffectiveFromDate = DateTime.Today.ToString(),
            PrimaryCareProvider = "",
            InvalidFlag = "1"
        };
        _transformReasonForRemoval.Setup(x => x.ReasonForRemovalTransformations(It.IsAny<CohortDistributionParticipant>())).Returns(Task.FromResult(expectedResponse));

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
    }

    [TestMethod]
    [DataRow("RDR", "AL1 1BB")]
    [DataRow("RDI", "AL1 1BB")]
    [DataRow("RPR", "AL1 1BB")]
    public async Task Run_ReasonForRemovalRule1_TransformsMultipleFields(string reasonForRemoval, string postcode)
    {
        // Arrange
        var reasonForRemovalEffectiveFromDate = "2/10/2024";
        var addressLine = "address";
        var bsoCode = "ELD";

        _requestBody.Participant.ReasonForRemoval = reasonForRemoval;
        _requestBody.Participant.ReasonForRemovalEffectiveFromDate = reasonForRemovalEffectiveFromDate;
        _requestBody.Participant.Postcode = postcode;
        _requestBody.Participant.AddressLine1 = addressLine;
        _requestBody.Participant.AddressLine2 = addressLine;
        _requestBody.Participant.AddressLine3 = addressLine;
        _requestBody.Participant.AddressLine4 = addressLine;
        _requestBody.Participant.AddressLine5 = addressLine;

        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);
        var expectedResponse = new CohortDistributionParticipant
        {
            NhsNumber = "1",
            FirstName = "John",
            FamilyName = "Smith",
            NamePrefix = "MR",
            Gender = Gender.Male,
            AddressLine1 = addressLine,
            AddressLine2 = addressLine,
            AddressLine3 = addressLine,
            AddressLine4 = addressLine,
            AddressLine5 = addressLine,
            Postcode = postcode,
            PrimaryCareProvider = $"ZZZ{bsoCode}",
            PrimaryCareProviderEffectiveFromDate = reasonForRemovalEffectiveFromDate,
            ReasonForRemoval = null,
            ReasonForRemovalEffectiveFromDate = null,
        };
        _transformReasonForRemoval.Setup(x => x.ReasonForRemovalTransformations(It.IsAny<CohortDistributionParticipant>())).Returns(Task.FromResult(expectedResponse));

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    [DataRow("RDR", null)]
    [DataRow("RDI", "")]
    [DataRow("RPR", "InvalidPostcode")]
    public async Task Run_ReasonForRemovalRule2_TransformsMultipleFields(string reasonForRemoval, string postcode)
    {
        // Arrange
        var reasonForRemovalEffectiveFromDate = "2/10/2024";
        var addressLine = "address";
        var bsoCode = "ELD";

        _requestBody.Participant.PrimaryCareProvider = "Y00090";
        _requestBody.Participant.ReasonForRemoval = reasonForRemoval;
        _requestBody.Participant.ReasonForRemovalEffectiveFromDate = reasonForRemovalEffectiveFromDate;
        _requestBody.Participant.Postcode = postcode;
        _requestBody.Participant.AddressLine1 = addressLine;
        _requestBody.Participant.AddressLine2 = addressLine;
        _requestBody.Participant.AddressLine3 = addressLine;
        _requestBody.Participant.AddressLine4 = addressLine;
        _requestBody.Participant.AddressLine5 = addressLine;

        var expectedResponse = new CohortDistributionParticipant
        {
            NhsNumber = "1",
            FirstName = "John",
            FamilyName = "Smith",
            NamePrefix = "MR",
            Gender = Gender.Male,
            AddressLine1 = addressLine,
            AddressLine2 = addressLine,
            AddressLine3 = addressLine,
            AddressLine4 = addressLine,
            AddressLine5 = addressLine,
            Postcode = postcode,
            PrimaryCareProvider = $"ZZZ{bsoCode}",
            PrimaryCareProviderEffectiveFromDate = reasonForRemovalEffectiveFromDate,
            ReasonForRemoval = null,
            ReasonForRemovalEffectiveFromDate = null,
        };
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);
        _transformReasonForRemoval.Setup(x => x.ReasonForRemovalTransformations(It.IsAny<CohortDistributionParticipant>())).Returns(Task.FromResult(expectedResponse));

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    [DataRow("RDR", null)]
    [DataRow("RDI", "")]
    [DataRow("RPR", "InvalidPostcode")]
    public async Task Run_ReasonForRemovalRule_RaisesException(string reasonForRemoval, string postcode)
    {
        // Arrange
        var addressLine = "address";

        _requestBody.Participant.PrimaryCareProvider = "Y00090";
        _requestBody.Participant.ReasonForRemoval = reasonForRemoval;
        _requestBody.Participant.Postcode = postcode;
        _requestBody.Participant.AddressLine1 = addressLine;
        _requestBody.Participant.AddressLine2 = addressLine;
        _requestBody.Participant.AddressLine3 = addressLine;
        _requestBody.Participant.AddressLine4 = addressLine;
        _requestBody.Participant.AddressLine5 = addressLine;

        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);
        _transformReasonForRemoval.Setup(x => x.ReasonForRemovalTransformations(It.IsAny<CohortDistributionParticipant>())).Throws<TransformationException>();

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    private void SetUpRequestBody(string json)
    {
        var byteArray = Encoding.ASCII.GetBytes(json);
        var bodyStream = new MemoryStream(byteArray);

        _request.Setup(r => r.Body).Returns(bodyStream);
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
            ReasonForRemoval = "NOTDEA",
            DateOfDeath = null,
        };
        _transformReasonForRemoval.Setup(x => x.ReasonForRemovalTransformations(It.IsAny<CohortDistributionParticipant>())).Returns(Task.FromResult(expectedResponse));

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
            ReasonForRemoval = "DEA",
            DateOfDeath = "2024-01-01",
        };
        _transformReasonForRemoval.Setup(x => x.ReasonForRemovalTransformations(It.IsAny<CohortDistributionParticipant>())).Returns(Task.FromResult(expectedResponse));

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }
}
