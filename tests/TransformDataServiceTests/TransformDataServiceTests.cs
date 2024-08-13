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
                Surname = "Smith",
                NamePrefix = "MR",
                Gender = Model.Enums.Gender.Male
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
    public async Task Run_TransformNamePrefix_ReturnTransformedPrefix(string namePrefix, string expectedTransformedPrefix)
    {
        // Arrange
        _requestBody.Participant.NamePrefix = namePrefix;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        var expectedResponse = new Participant
        {
            NhsNumber = "1",
            FirstName = "John",
            Surname = "Smith",
            NamePrefix = expectedTransformedPrefix,
            Gender = Model.Enums.Gender.Male
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
        var expectedResponse = new Participant
        {
            NhsNumber = "1",
            FirstName = "John",
            Surname = "Smith",
            NamePrefix = "DR",
            Gender = Model.Enums.Gender.Male
        };

        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }


    [TestMethod]
    public async Task Run_NamePrefixTooLong_TruncatePrefix()
    {
        // Arrange
        string actualNamePrefix = new string('A', 36);
        string expectedNamePrefix = new string('A', 35);
        _requestBody.Participant.NamePrefix = actualNamePrefix;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        var expectedResponse = new Participant
        {
            NhsNumber = "1",
            FirstName = "John",
            Surname = "Smith",
            NamePrefix = expectedNamePrefix,
            Gender = Model.Enums.Gender.Male
        };

        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_FirstNameTooLong_TruncatePrefix()
    {
        // Arrange
        string actualFirstName = new string('A', 36);
        string expectedFirstName = new string('A', 35);
        _requestBody.Participant.FirstName = actualFirstName;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        var expectedResponse = new Participant
        {
            NhsNumber = "1",
            FirstName = expectedFirstName,
            Surname = "Smith",
            NamePrefix = "MR",
            Gender = Model.Enums.Gender.Male
        };

        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_SurnameTooLong_TruncatePrefix()
    {
        // Arrange
        string actualSurname = new string('A', 36);
        string expectedSurname = new string('A', 35);
        _requestBody.Participant.Surname = actualSurname;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        var expectedResponse = new Participant
        {
            NhsNumber = "1",
            FirstName = "John",
            Surname = expectedSurname,
            NamePrefix = "MR",
            Gender = Model.Enums.Gender.Male
        };

        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_OtherGivenNamesTooLong_TruncatePrefix()
    {
        // Arrange
        string actualOtherGivenNames = new string('A', 105);
        string expectedOtherGivenNames = new string('A', 100);
        _requestBody.Participant.OtherGivenNames = actualOtherGivenNames;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        var expectedResponse = new Participant
        {
            NhsNumber = "1",
            FirstName = "John",
            Surname = "Smith",
            NamePrefix = "MR",
            OtherGivenNames = expectedOtherGivenNames,
            Gender = Model.Enums.Gender.Male
        };

        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_PreviousSurnameTooLong_TruncatePrefix()
    {
        // Arrange
        string actualPreviousSurname = new string('A', 36);
        string expectedPreviousSurname = new string('A', 35);
        _requestBody.Participant.PreviousSurname = actualPreviousSurname;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        var expectedResponse = new Participant
        {
            NhsNumber = "1",
            FirstName = "John",
            Surname = "Smith",
            NamePrefix = "MR",
            PreviousSurname = expectedPreviousSurname,
            Gender = Model.Enums.Gender.Male
        };

        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_AddressLine1TooLong_TruncatePrefix()
    {
        // Arrange
        string actualAddressLine1 = new string('A', 36);
        string expectedAddressLine1 = new string('A', 35);
        _requestBody.Participant.AddressLine1 = actualAddressLine1;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        var expectedResponse = new Participant
        {
            NhsNumber = "1",
            FirstName = "John",
            Surname = "Smith",
            NamePrefix = "MR",
            AddressLine1 = expectedAddressLine1,
            Gender = Model.Enums.Gender.Male
        };

        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_AddressLine2TooLong_TruncatePrefix()
    {
        // Arrange
        string actualAddressLine2 = new string('A', 36);
        string expectedAddressLine2 = new string('A', 35);
        _requestBody.Participant.AddressLine2 = actualAddressLine2;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        var expectedResponse = new Participant
        {
            NhsNumber = "1",
            FirstName = "John",
            Surname = "Smith",
            NamePrefix = "MR",
            AddressLine2 = expectedAddressLine2,
            Gender = Model.Enums.Gender.Male
        };

        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_AddressLine3TooLong_TruncatePrefix()
    {
        // Arrange
        string actualAddressLine3 = new string('A', 36);
        string expectedAddressLine3 = new string('A', 35);
        _requestBody.Participant.AddressLine3 = actualAddressLine3;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        var expectedResponse = new Participant
        {
            NhsNumber = "1",
            FirstName = "John",
            Surname = "Smith",
            NamePrefix = "MR",
            AddressLine3 = expectedAddressLine3,
            Gender = Model.Enums.Gender.Male
        };

        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_AddressLine4TooLong_TruncatePrefix()
    {
        // Arrange
        string actualAddressLine4 = new string('A', 36);
        string expectedAddressLine4 = new string('A', 35);
        _requestBody.Participant.AddressLine4 = actualAddressLine4;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        var expectedResponse = new Participant
        {
            NhsNumber = "1",
            FirstName = "John",
            Surname = "Smith",
            NamePrefix = "MR",
            AddressLine4 = expectedAddressLine4,
            Gender = Model.Enums.Gender.Male
        };

        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_AddressLine5TooLong_TruncatePrefix()
    {
        // Arrange
        string actualAddressLine5 = new string('A', 36);
        string expectedAddressLine5 = new string('A', 35);
        _requestBody.Participant.AddressLine5 = actualAddressLine5;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        var expectedResponse = new Participant
        {
            NhsNumber = "1",
            FirstName = "John",
            Surname = "Smith",
            NamePrefix = "MR",
            AddressLine5 = expectedAddressLine5,
            Gender = Model.Enums.Gender.Male
        };

        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_PostcodeTooLong_TruncatePrefix()
    {
        // Arrange
        string actualPostcode = new string('A', 36);
        string expectedPostcode = new string('A', 35);
        _requestBody.Participant.Postcode = actualPostcode;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        var expectedResponse = new Participant
        {
            NhsNumber = "1",
            FirstName = "John",
            Surname = "Smith",
            NamePrefix = "MR",
            Postcode = expectedPostcode,
            Gender = Model.Enums.Gender.Male
        };

        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_TelephoneNumberTooLong_TruncatePrefix()
    {
        // Arrange
        string actualTelephoneNumber = new string('A', 33);
        string expectedTelephoneNumber = new string('A', 32);
        _requestBody.Participant.TelephoneNumber = actualTelephoneNumber;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        var expectedResponse = new Participant
        {
            NhsNumber = "1",
            FirstName = "John",
            Surname = "Smith",
            NamePrefix = "MR",
            TelephoneNumber = expectedTelephoneNumber,
            Gender = Model.Enums.Gender.Male
        };

        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_MobileNumberTooLong_TruncatePrefix()
    {
        // Arrange
        string actualMobileNumber = new string('A', 33);
        string expectedMobileNumber = new string('A', 32);
        _requestBody.Participant.MobileNumber = actualMobileNumber;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        var expectedResponse = new Participant
        {
            NhsNumber = "1",
            FirstName = "John",
            Surname = "Smith",
            NamePrefix = "MR",
            MobileNumber = expectedMobileNumber,
            Gender = Model.Enums.Gender.Male
        };

        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_EmailAddressTooLong_TruncatePrefix()
    {
        // Arrange
        string actualEmailAddress = new string('A', 33);
        string expectedEmailAddress = new string('A', 32);
        _requestBody.Participant.EmailAddress = actualEmailAddress;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        var expectedResponse = new Participant
        {
            NhsNumber = "1",
            FirstName = "John",
            Surname = "Smith",
            NamePrefix = "MR",
            EmailAddress = expectedEmailAddress,
            Gender = Model.Enums.Gender.Male
        };

        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_Should_Transform_Participant_Data_When_Gender_IsNot_0_1_2_or_9()
    {
        // Arrange
        _requestBody.Participant.Gender = (Model.Enums.Gender)4;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        var expectedResponse = new Participant
        {
            NhsNumber = "1",
            FirstName = "John",
            Surname = "Smith",
            NamePrefix = "MR",
            Gender = Model.Enums.Gender.NotSpecified,
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
        _requestBody.Participant.Gender = Model.Enums.Gender.Male;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        var expectedResponse = new Participant
        {
            NhsNumber = "1",
            FirstName = "John",
            Surname = "Smith",
            NamePrefix = "MR",
            Gender = Model.Enums.Gender.Male,
        };

        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    public async Task Run_InvalidCharsInParticipant_ReturnTransformedFields()
    {
        // Arrange
        _requestBody.Participant.FirstName = "John.,-()/='+:?!\"%&;<>*";
        _requestBody.Participant.Surname = "{[Smith£$^`~#@_|\\]}";
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        var expectedResponse = new Participant
        {
            NhsNumber = "1",
            FirstName = "John.,-()/='+:?!\"%&;<>*",
            Surname = "((Smith   '   -:/))",
            NamePrefix = "DR",
            Gender = Model.Enums.Gender.Male
        };

        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    private void SetUpRequestBody(string json)
    {
        var byteArray = Encoding.ASCII.GetBytes(json);
        var bodyStream = new MemoryStream(byteArray);

        _request.Setup(r => r.Body).Returns(bodyStream);
    }
}
