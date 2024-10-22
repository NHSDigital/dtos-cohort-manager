namespace NHS.CohortManager.Tests.TransformDataServiceTests;

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

        _function = new TransformDataService(_createResponse.Object, _handleException.Object, _logger.Object);

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
    [DataRow("HIS ROYAL HGHNESS", "HRH")]
    [DataRow("BRIG", "BRIG")]
    public async Task Run_TransformNamePrefix_ReturnTransformedPrefix(string namePrefix, string expectedTransformedPrefix)
    {
        // Arrange
        _requestBody.Participant.NamePrefix = namePrefix;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        var expectedResponse = new CohortDistributionParticipant
        {
            NhsNumber = "1",
            FirstName = "John",
            FamilyName = "Smith",
            NamePrefix = expectedTransformedPrefix,
            Gender = Gender.Male
        };

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

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        var expectedResponse = new CohortDistributionParticipant
        {
            NhsNumber = "1",
            FirstName = "John",
            FamilyName = "Smith",
            NamePrefix = null,
            Gender = Gender.Male
        };

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

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        var expectedResponse = new CohortDistributionParticipant
        {
            NhsNumber = "1",
            FirstName = "John",
            FamilyName = "Smith",
            NamePrefix = "DR",
            Gender = Gender.Male,
        };

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

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
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

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        var expectedResponse = new CohortDistributionParticipant
        {
            NhsNumber = "1",
            FirstName = "John",
            FamilyName = "Smith",
            NamePrefix = "MR",
            Gender = Gender.NotSpecified,
        };
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

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        var expectedResponse = new CohortDistributionParticipant
        {
            NhsNumber = "1",
            FirstName = "John",
            FamilyName = "Smith",
            NamePrefix = "MR",
            Gender = Gender.Male,
        };

        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    public async Task GetAddress_AddressFieldsBlankPostcodeNotNull_ReturnAddress()
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

    public async Task Run_InvalidCharsInParticipant_ReturnTransformedFields()
    {
        // Arrange
        _requestBody.Participant.FirstName = "John.,-()/='+:?!\"%&;<>*";
        _requestBody.Participant.FamilyName = "{[Smith£$^`~#@_|\\]}";
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        var expectedResponse = new CohortDistributionParticipant
        {
            NhsNumber = "1",
            FirstName = "John.,-()/='+:?!\"%&;<>*",
            FamilyName = "((Smith   '   -:/))",
            NamePrefix = "DR",
            Gender = Gender.Male
        };

        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_RfrIsDeaAndDateOfDeathIsNull_SetDateOfDeathToRfrDate()
    {
        // Arrange
        _requestBody.Participant.ReasonForRemoval = "DEA";
        _requestBody.Participant.ReasonForRemovalEffectiveFromDate = "2/10/2024";

        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
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

        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ReasonForRemovalRuleA_SetReasonForRemovalAsA()
    {
        // Arrange
        _requestBody.Participant.ReasonForRemoval = "A";

        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        var expectedResponse = new CohortDistributionParticipant
        {
            NhsNumber = "1",
            FirstName = "John",
            FamilyName = "Smith",
            NamePrefix = "MR",
            Gender = Gender.Male,
            ReasonForRemoval = "RULE_A_SUCCESS"
        };

        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ReasonForRemovalRuleB_SetReasonForRemovalAsB()
    {
        // Arrange
        _requestBody.Participant.ReasonForRemoval = "B";

        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        var expectedResponse = new CohortDistributionParticipant
        {
            NhsNumber = "1",
            FirstName = "John",
            FamilyName = "Smith",
            NamePrefix = "MR",
            Gender = Gender.Male,
            ReasonForRemoval = "RULE_B_SUCCESS"
        };

        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ReasonForRemovalRuleC_RaisesExceptionAndNoTransformation()
    {
        // Arrange
        _requestBody.Participant.FamilyName = "surname";

        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        var expectedResponse = new CohortDistributionParticipant
        {
            NhsNumber = "1",
            FirstName = "John",
            FamilyName = "surname",
            NamePrefix = "MR",
            Gender = Gender.Male
        };

        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateRecordValidationExceptionLog(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()),
            Times.Once());
    }

    [TestMethod]
    public async Task Run_ReasonForRemovalRuleD_RaisesExceptionAndNoTransformation()
    {
        // Arrange
        _requestBody.Participant.ReasonForRemoval = "D";

        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        var expectedResponse = new CohortDistributionParticipant
        {
            NhsNumber = "1",
            FirstName = "John",
            FamilyName = "Smith",
            NamePrefix = "MR",
            Gender = Gender.Male,
            ReasonForRemoval = "D"
        };

        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateRecordValidationExceptionLog(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()),
            Times.Once());
    }

    private void SetUpRequestBody(string json)
    {
        var byteArray = Encoding.ASCII.GetBytes(json);
        var bodyStream = new MemoryStream(byteArray);

        _request.Setup(r => r.Body).Returns(bodyStream);
    }
}
