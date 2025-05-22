namespace NHS.CohortManager.Tests.ScreeningDataServicesTests;

using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Common;
using Model.Enums;
using Model;
using NHS.CohortManager.Tests.TestUtils;
using System.Text.Json;
using DataServices.Client;
using Microsoft.Extensions.Options;
using NHS.Screening.CreateParticipant;

[TestClass]
public class CreateParticipantTests
{
    private readonly Mock<ILogger<ScreeningDataServices.CreateParticipant>> _mockLogger = new();
    private readonly Mock<ICreateResponse> _mockCreateResponse = new();
    private readonly Mock<IExceptionHandler> _handleException = new();
    private readonly Mock<IDataServiceClient<ParticipantManagement>> _participantManagementClient = new();
    private readonly Mock<IOptions<CreateParticipantConfig>> _config = new();

    private readonly Mock<IHttpClientFunction> _httpClientFunction = new();
    private readonly ScreeningDataServices.CreateParticipant _sut;
    private readonly ParticipantCsvRecord _requestRecord;

    public CreateParticipantTests()
    {
        _requestRecord = new()
        {
            Participant = new()
            {
                ParticipantId = "1",
                NhsNumber = "123456",
                SupersededByNhsNumber = "789012",
                PrimaryCareProvider = "ABC Clinic",
                NamePrefix = "Mr.",
                FirstName = "John",
                OtherGivenNames = "Middle",
                FamilyName = "Doe",
                DateOfBirth = "1990-01-01",
                Gender = Gender.Male,
                AddressLine1 = "123 Main Street",
                AddressLine2 = "Apt 101",
                AddressLine3 = "Suburb",
                AddressLine4 = "City",
                AddressLine5 = "State",
                Postcode = "12345",
                ReasonForRemoval = "Moved",
                ReasonForRemovalEffectiveFromDate = "2024-04-23",
                DateOfDeath = "2024-04-23",
                TelephoneNumber = "123-456-7890",
                MobileNumber = "987-654-3210",
                EmailAddress = "john.doe@example.com",
                PreferredLanguage = "English",
                IsInterpreterRequired = "0",
                RecordType = Actions.Amended,
                ScreeningId = "1"
            }
        };

        var testConfig = new CreateParticipantConfig
        {
            ParticipantManagementUrl = "test-url",
            LookupValidationURL = "test-url-2"
        };

        _config.Setup(c => c.Value).Returns(testConfig);

        ValidationExceptionLog validationResponse = new()
        {
            IsFatal = false,
            CreatedException = false
        };

        _participantManagementClient
            .Setup(data => data.Add(It.IsAny<ParticipantManagement>()))
            .ReturnsAsync(true);

        _httpClientFunction
            .Setup(x => x.GetResponseText(It.IsAny<HttpResponseMessage>()))
            .ReturnsAsync(JsonSerializer.Serialize(validationResponse));

        _sut = new(
            _mockLogger.Object,
            _mockCreateResponse.Object,
            _handleException.Object,
            _httpClientFunction.Object,

            _participantManagementClient.Object,
            _config.Object);
        _httpClientFunction.Setup(x => x.GetResponseText(It.IsAny<HttpResponseMessage>())).Returns(Task.FromResult<string>(
            JsonSerializer.Serialize<ValidationExceptionLog>(new ValidationExceptionLog()
            {
                IsFatal = false,
                CreatedException = false
            })));
        _participantManagementClient.Setup(data => data.Add(It.IsAny<ParticipantManagement>())).ReturnsAsync(true);

    }

    [TestMethod]
    public async Task Run_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_requestRecord);
        var mockRequest = MockHelpers.CreateMockHttpRequestData(json);

        // Act
        await _sut.Run(mockRequest);

        // Assert
        _mockCreateResponse.Verify(response => response.CreateHttpResponse(HttpStatusCode.OK, It.IsAny<HttpRequestData>(), ""), Times.Once);
        _mockCreateResponse.VerifyNoOtherCalls();
    }

    [TestMethod]
    [DataRow("2025")]
    [DataRow("202501")]
    public async Task Run_ParticipantHasPartialDates_TransformDatesAndAdd(string rfrDate)
    {
        // Arrange
        var expectedParticipant = _requestRecord.Participant.ToParticipantManagement();

        var json = JsonSerializer.Serialize(_requestRecord);
        var mockRequest = MockHelpers.CreateMockHttpRequestData(json);

        // Act
        var response = await _sut.Run(mockRequest);

        // Assert
        _mockCreateResponse.Verify(response => response.CreateHttpResponse(HttpStatusCode.OK, It.IsAny<HttpRequestData>(), ""), Times.Once);
        _participantManagementClient
            .Verify(x => x.Add(It.Is<ParticipantManagement>(x => x.ReasonForRemovalDate == expectedParticipant.ReasonForRemovalDate)),
                Times.Once());
    }

    [TestMethod]
    public async Task Run_InvalidRequest_Returns500()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_requestRecord);
        var mockRequest = MockHelpers.CreateMockHttpRequestData(json);
        _participantManagementClient
            .Setup(data => data.Add(It.IsAny<ParticipantManagement>()))
            .ReturnsAsync(false);


        // Act
        await _sut.Run(mockRequest);

        // Assert
        _mockCreateResponse.Verify(response => response.CreateHttpResponse(HttpStatusCode.InternalServerError, It.IsAny<HttpRequestData>(), ""), Times.Once);
        _mockCreateResponse.VerifyNoOtherCalls();
    }


    [TestMethod]
    public async Task Run_InvalidRequest_ReturnsCreated()
    {
        // Arrange
        ValidationExceptionLog validationResponse = new()
        {
            IsFatal = true,
            CreatedException = false
        };

        var testConfig = new CreateParticipantConfig
        {
            ParticipantManagementUrl = "test-url",
            LookupValidationURL = "test-url-2"
        };

        _config.Setup(c => c.Value).Returns(testConfig);

        new ScreeningDataServices.CreateParticipant(
            _mockLogger.Object,
            _mockCreateResponse.Object,
            _handleException.Object,
            _httpClientFunction.Object,
            _participantManagementClient.Object,
            _config.Object);
        _httpClientFunction.Setup(x => x.GetResponseText(It.IsAny<HttpResponseMessage>())).Returns(Task.FromResult<string>(
            JsonSerializer.Serialize<ValidationExceptionLog>(new ValidationExceptionLog()
            {
                IsFatal = true,
                CreatedException = false
            })));
        _participantManagementClient.Setup(data => data.Add(It.IsAny<ParticipantManagement>())).ReturnsAsync(true);

        _httpClientFunction
            .Setup(x => x.GetResponseText(It.IsAny<HttpResponseMessage>()))
            .ReturnsAsync(JsonSerializer.Serialize(validationResponse));

        var json = JsonSerializer.Serialize(_requestRecord);
        var mockRequest = MockHelpers.CreateMockHttpRequestData(json);


        // Act
        await _sut.Run(mockRequest);

        // Assert
        _mockCreateResponse.Verify(response => response.CreateHttpResponse(HttpStatusCode.Created, It.IsAny<HttpRequestData>(), ""), Times.Once);
        _mockCreateResponse.VerifyNoOtherCalls();
    }


    [TestMethod]
    public async Task Run_AddToDatabaseThrowsAnError_ReturnsInternalServerError()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_requestRecord);
        var mockRequest = MockHelpers.CreateMockHttpRequestData(json);

        var testConfig = new CreateParticipantConfig
        {
            ParticipantManagementUrl = "test-url",
            LookupValidationURL = "test-url-2"
        };

        _config.Setup(c => c.Value).Returns(testConfig);

        new ScreeningDataServices.CreateParticipant(
            _mockLogger.Object,
            _mockCreateResponse.Object,
            _handleException.Object,
            _httpClientFunction.Object,
            _participantManagementClient.Object,
            _config.Object);
        _httpClientFunction.Setup(x => x.GetResponseText(It.IsAny<HttpResponseMessage>())).Returns(Task.FromResult<string>(
            JsonSerializer.Serialize<ValidationExceptionLog>(new ValidationExceptionLog()
            {
                IsFatal = false,
                CreatedException = false
            })));

        _participantManagementClient.Setup(data => data.Add(It.IsAny<ParticipantManagement>())).Throws(new Exception("someError"));


        _participantManagementClient
            .Setup(data => data.Add(It.IsAny<ParticipantManagement>()))
            .Throws(new Exception("someError"));


        // Act
        await _sut.Run(mockRequest);

        // Assert
        _mockCreateResponse.Verify(response => response.CreateHttpResponse(HttpStatusCode.InternalServerError, It.IsAny<HttpRequestData>(), ""), Times.Once);
        _mockCreateResponse.VerifyNoOtherCalls();
    }

}
