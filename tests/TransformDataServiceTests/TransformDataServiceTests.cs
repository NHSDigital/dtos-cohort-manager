namespace NHS.CohortManager.Tests.TransformDataServiceTests;

using System.Net;
using System.Text;
using System.Text.Json;
using NHS.CohortManager.CohortDistribution;
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
        result.Body.Position = 0;
        var reader = new StreamReader(result.Body, Encoding.UTF8);
        var responseBody = await reader.ReadToEndAsync();
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
        };
        result.Body.Position = 0;
        var reader = new StreamReader(result.Body, Encoding.UTF8);
        var responseBody = await reader.ReadToEndAsync();
        Assert.AreEqual(JsonSerializer.Serialize(expectedResponse), responseBody);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }


    [TestMethod]
    public async Task Run_NamePrefixTooLong_TruncatePrefix()
    {
        // Arrange
        _requestBody.Participant.NamePrefix = "AAAAABBBBBCCCCCDDDDDEEEEEFFFFFGGGGGHHHHH";
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
            NamePrefix = "AAAAABBBBBCCCCCDDDDDEEEEEFFFFFGGGGG",
            Gender = Model.Enums.Gender.Male
        };
        result.Body.Position = 0;
        var reader = new StreamReader(result.Body, Encoding.UTF8);
        var responseBody = await reader.ReadToEndAsync();
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
            NamePrefix = "Mr",
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
            NamePrefix = "Mr",
            Gender = Model.Enums.Gender.Male,
        };

        result.Body.Position = 0;
        var reader = new StreamReader(result.Body, Encoding.UTF8);
        var responseBody = await reader.ReadToEndAsync();
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
