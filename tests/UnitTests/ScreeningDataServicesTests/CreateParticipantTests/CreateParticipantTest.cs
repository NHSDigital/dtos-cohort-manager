namespace NHS.CohortManager.Tests.ScreeningDataServicesTests;

using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Common;
using Data.Database;
using Model.Enums;
using Model;
using NHS.CohortManager.Tests.TestUtils;
using System.Text.Json;
using DataServices.Client;

[TestClass]
public class CreateParticipantTests
{
    private readonly Mock<ILogger<ScreeningDataServices.CreateParticipant>> _mockLogger = new();
    private readonly Mock<ICreateResponse> _mockCreateResponse = new();
    private readonly Mock<IExceptionHandler> _handleException = new();
    private readonly Mock<IDataServiceClient<ParticipantManagement>> _participantManagementClient = new();

    private readonly Mock<ICallFunction> _callFunction = new();

    [TestMethod]
    public async Task Run_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var participantCsvRecord = new ParticipantCsvRecord
        {
            Participant = new Participant
            {
                NhsNumber = "1234567890",
                ScreeningId = "1"
            }
        };
        var json = JsonSerializer.Serialize(participantCsvRecord);
        var mockRequest = MockHelpers.CreateMockHttpRequestData(json);

        var sut = new ScreeningDataServices.CreateParticipant(
            _mockLogger.Object,
            _mockCreateResponse.Object,
            _handleException.Object,
            _callFunction.Object,
            _participantManagementClient.Object);

        _callFunction.Setup(x => x.GetResponseText(It.IsAny<HttpWebResponse>())).Returns(Task.FromResult<string>(
            JsonSerializer.Serialize<ValidationExceptionLog>(new ValidationExceptionLog()
            {
                IsFatal = false,
                CreatedException = false
            })));
        _participantManagementClient.Setup(data => data.Add(It.IsAny<ParticipantManagement>())).ReturnsAsync(true);

        // Act
        await sut.Run(mockRequest);

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
        var participantCsvRecord = new ParticipantCsvRecord
        {
            Participant = new Participant
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
                ReasonForRemovalEffectiveFromDate = rfrDate,
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

        var sut = new ScreeningDataServices.CreateParticipant(
            _mockLogger.Object,
            _mockCreateResponse.Object,
            _handleException.Object,
            _callFunction.Object,
            _participantManagementClient.Object);

        var expectedParticipant = participantCsvRecord.Participant.ToParticipantManagement();

        var json = JsonSerializer.Serialize(participantCsvRecord);
        var mockRequest = MockHelpers.CreateMockHttpRequestData(json);

        // Act
        var response = await sut.Run(mockRequest);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        _participantManagementClient
            .Verify(x => x.Add(expectedParticipant), Times.Once());
    }

    [TestMethod]
    public async Task Run_InvalidRequest_Returns500()
    {
        // Arrange
        var participantCsvRecord = new ParticipantCsvRecord
        {
            Participant = new Participant
            {
                NhsNumber = "1234567890",
                ScreeningId = "1"
            }
        };
        var json = JsonSerializer.Serialize(participantCsvRecord);
        var mockRequest = MockHelpers.CreateMockHttpRequestData(json);

        var sut = new ScreeningDataServices.CreateParticipant(
            _mockLogger.Object,
            _mockCreateResponse.Object,
            _handleException.Object,
            _callFunction.Object,
            _participantManagementClient.Object);
        _callFunction.Setup(x => x.GetResponseText(It.IsAny<HttpWebResponse>())).Returns(Task.FromResult<string>(
            JsonSerializer.Serialize<ValidationExceptionLog>(new ValidationExceptionLog()
            {
                IsFatal = false,
                CreatedException = false
            })));
        _participantManagementClient.Setup(data => data.Add(It.IsAny<ParticipantManagement>())).ReturnsAsync(false);

        // Act
        await sut.Run(mockRequest);

        // Assert
        _mockCreateResponse.Verify(response => response.CreateHttpResponse(HttpStatusCode.InternalServerError, It.IsAny<HttpRequestData>(), ""), Times.Once);
        _mockCreateResponse.VerifyNoOtherCalls();
    }


    [TestMethod]
    public async Task Run_InValidRequest_ReturnsCreated()
    {
        // Arrange
        var participantCsvRecord = new ParticipantCsvRecord
        {
            Participant = new Participant
            {
                NhsNumber = "1234567890",
                ScreeningId = "1"
            }
        };
        var json = JsonSerializer.Serialize(participantCsvRecord);
        var mockRequest = MockHelpers.CreateMockHttpRequestData(json);

        var sut = new ScreeningDataServices.CreateParticipant(
            _mockLogger.Object,
            _mockCreateResponse.Object,
            _handleException.Object,
            _callFunction.Object,
            _participantManagementClient.Object);
        _callFunction.Setup(x => x.GetResponseText(It.IsAny<HttpWebResponse>())).Returns(Task.FromResult<string>(
            JsonSerializer.Serialize<ValidationExceptionLog>(new ValidationExceptionLog()
            {
                IsFatal = true,
                CreatedException = false
            })));
        _participantManagementClient.Setup(data => data.Add(It.IsAny<ParticipantManagement>())).ReturnsAsync(true);

        // Act
        await sut.Run(mockRequest);

        // Assert
        _mockCreateResponse.Verify(response => response.CreateHttpResponse(HttpStatusCode.Created, It.IsAny<HttpRequestData>(), ""), Times.Once);
        _mockCreateResponse.VerifyNoOtherCalls();
    }


    [TestMethod]
    public async Task Run_AddTodDatabaseThrowsAnError_ReturnsInternalServerError()
    {
        // Arrange
        var participantCsvRecord = new ParticipantCsvRecord
        {
            Participant = new Participant
            {
                NhsNumber = "1234567890",
                ScreeningId = "1"
            }
        };
        var json = JsonSerializer.Serialize(participantCsvRecord);
        var mockRequest = MockHelpers.CreateMockHttpRequestData(json);

        var sut = new ScreeningDataServices.CreateParticipant(
            _mockLogger.Object,
            _mockCreateResponse.Object,
            _handleException.Object,
            _callFunction.Object,
            _participantManagementClient.Object);
        _callFunction.Setup(x => x.GetResponseText(It.IsAny<HttpWebResponse>())).Returns(Task.FromResult<string>(
            JsonSerializer.Serialize<ValidationExceptionLog>(new ValidationExceptionLog()
            {
                IsFatal = false,
                CreatedException = false
            })));

        _participantManagementClient.Setup(data => data.Add(It.IsAny<ParticipantManagement>())).Throws(new Exception("someError"));


        // Act
        await sut.Run(mockRequest);

        // Assert
        _mockCreateResponse.Verify(response => response.CreateHttpResponse(HttpStatusCode.InternalServerError, It.IsAny<HttpRequestData>(), ""), Times.Once);
        _mockCreateResponse.VerifyNoOtherCalls();
    }

}
