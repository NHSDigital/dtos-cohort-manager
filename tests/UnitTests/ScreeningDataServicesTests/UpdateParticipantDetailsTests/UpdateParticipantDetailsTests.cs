namespace NHS.CohortManager.Tests.UnitTests.ScreeningDataServicesTests;

using System.Net;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;
using Moq;
using updateParticipantDetails;
using DataServices.Client;
using Common;
using System.Linq.Expressions;
using NHS.CohortManager.Tests.TestUtils;
using System.Text.Json;
using NHS.Screening.UpdateParticipantDetails;
using Microsoft.Extensions.Options;

[TestClass]
public class UpdateParticipantDetailsTests
{
    private readonly ParticipantCsvRecord _participantCsvRecord;
    private readonly Mock<IDataServiceClient<ParticipantManagement>> _participantManagementClientMock = new();
    private readonly Mock<ILogger<UpdateParticipantDetails>> _loggerMock = new();
    private readonly Mock<IHttpClientFunction> _httpClientFunction = new();
    private readonly Mock<CreateResponse> _createResponseMock = new();
    private readonly Mock<IExceptionHandler> _exceptionHandlerMock = new();
    private readonly SetupRequest _setupRequest = new();
    private readonly Mock<IOptions<UpdateParticipantDetailsConfig>> _config = new();
    private readonly ValidationExceptionLog _lookupValidationResponseBody = new();

    public UpdateParticipantDetailsTests()
    {
        Environment.SetEnvironmentVariable("LookupValidationURL", "LookupValidationURL");

        _participantManagementClientMock
            .Setup(c => c.Update(It.IsAny<ParticipantManagement>()))
            .ReturnsAsync(true);

        _participantManagementClientMock
            .Setup(c => c.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
            .ReturnsAsync(new ParticipantManagement { NHSNumber = 1, ScreeningId = 1 });

        _participantCsvRecord = new ParticipantCsvRecord
        {
            FileName = "test.csv",
            Participant = new Participant()
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
                ReasonForRemovalEffectiveFromDate = "2023-01-01",
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

        _httpClientFunction
            .Setup(m => m.SendPost("LookupValidationURL", It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        _lookupValidationResponseBody.CreatedException = false;
        _lookupValidationResponseBody.IsFatal = false;
        string lookupResponseJson = JsonSerializer.Serialize(_lookupValidationResponseBody);

        var testConfig = new UpdateParticipantDetailsConfig
        {
            ParticipantManagementUrl = "test-storage",
            LookupValidationURL = "test-inbound"
        };

        _config.Setup(c => c.Value).Returns(testConfig);

        _httpClientFunction
            .Setup(x => x.GetResponseText(It.IsAny<HttpResponseMessage>()))
            .ReturnsAsync(lookupResponseJson);
    }

    [TestMethod]
    public async Task Run_ValidRequest_UpdateAndReturnOk()
    {
        // Arrange
        var sut = new UpdateParticipantDetails(_loggerMock.Object, _createResponseMock.Object, _exceptionHandlerMock.Object,

        _httpClientFunction.Object, _participantManagementClientMock.Object,
                                                _config.Object);


        string json = JsonSerializer.Serialize(_participantCsvRecord);
        var request = _setupRequest.Setup(json);

        // Act
        var response = await sut.Run(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        _participantManagementClientMock
            .Verify(x => x.Update(It.Is<ParticipantManagement>(x => x.RecordUpdateDateTime != null)),
                Times.Once());
    }

    [TestMethod]
    [DataRow("2025")]
    [DataRow("202501")]
    public async Task Run_ParticipantHasPartialDates_TransformDatesAndUpdate(string rfrDate)
    {
        // Arrange
        var sut = new UpdateParticipantDetails(_loggerMock.Object, _createResponseMock.Object, _exceptionHandlerMock.Object,
                                                _httpClientFunction.Object, _participantManagementClientMock.Object, _config.Object);

        _participantCsvRecord.Participant.ReasonForRemovalEffectiveFromDate = rfrDate;
        var expectedParticipant = _participantCsvRecord.Participant.ToParticipantManagement();
        string json = JsonSerializer.Serialize(_participantCsvRecord);
        var request = _setupRequest.Setup(json);

        // Act
        var response = await sut.Run(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        _participantManagementClientMock
            .Verify(x => x.Update(It.Is<ParticipantManagement>(x => x.ReasonForRemovalDate == expectedParticipant.ReasonForRemovalDate)),
                Times.Once());
    }

    [TestMethod]
    public async Task Run_GetOldParticipantFails_ReturnInternalServerError()
    {
        // Arrange
        _participantManagementClientMock
            .Setup(c => c.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
            .Throws(new Exception());

        var sut = new UpdateParticipantDetails(_loggerMock.Object, _createResponseMock.Object, _exceptionHandlerMock.Object,
                                                _httpClientFunction.Object, _participantManagementClientMock.Object,
                                                _config.Object);

        string json = JsonSerializer.Serialize(_participantCsvRecord);
        var request = _setupRequest.Setup(json);

        // Act
        var response = await sut.Run(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [TestMethod]
    public async Task Run_LookupValidationFails_ReturnInternalServerError()
    {
        // Arrange
        _httpClientFunction
            .Setup(x => x.GetResponseText(It.IsAny<HttpResponseMessage>()))
            .Throws(new Exception());

        var sut = new UpdateParticipantDetails(_loggerMock.Object, _createResponseMock.Object, _exceptionHandlerMock.Object,
                                                _httpClientFunction.Object, _participantManagementClientMock.Object,
                                                _config.Object);

        string json = JsonSerializer.Serialize(_participantCsvRecord);
        var request = _setupRequest.Setup(json);

        // Act
        var response = await sut.Run(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [TestMethod]
    public async Task Run_FatalValidationRuleTriggered_ReturnCreatedAndDoNotUpdate()
    {
        // Arrange
        _lookupValidationResponseBody.CreatedException = true;
        _lookupValidationResponseBody.IsFatal = true;
        string lookupResponseJson = JsonSerializer.Serialize(_lookupValidationResponseBody);

        _httpClientFunction
            .Setup(x => x.GetResponseText(It.IsAny<HttpResponseMessage>()))
            .ReturnsAsync(lookupResponseJson);

        var sut = new UpdateParticipantDetails(_loggerMock.Object, _createResponseMock.Object, _exceptionHandlerMock.Object,
                                                _httpClientFunction.Object, _participantManagementClientMock.Object,
                                                _config.Object);

        string json = JsonSerializer.Serialize(_participantCsvRecord);
        var request = _setupRequest.Setup(json);

        // Act
        var response = await sut.Run(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        _participantManagementClientMock
            .Verify(x => x.Update(It.IsAny<ParticipantManagement>()), Times.Never());
    }

    [TestMethod]
    public async Task Run_NonFatalValidationRuleTriggered_SetExceptionFlagAndUpdate()
    {
        // Arrange
        _lookupValidationResponseBody.CreatedException = true;
        _lookupValidationResponseBody.IsFatal = false;
        string lookupResponseJson = JsonSerializer.Serialize(_lookupValidationResponseBody);

        _httpClientFunction
            .Setup(x => x.GetResponseText(It.IsAny<HttpResponseMessage>()))
            .ReturnsAsync(lookupResponseJson);

        var sut = new UpdateParticipantDetails(_loggerMock.Object, _createResponseMock.Object, _exceptionHandlerMock.Object,
                                                _httpClientFunction.Object, _participantManagementClientMock.Object,
                                                _config.Object);

        string json = JsonSerializer.Serialize(_participantCsvRecord);
        var request = _setupRequest.Setup(json);

        // Act
        await sut.Run(request.Object);

        // Assert
        _participantManagementClientMock
            .Verify(x => x.Update(It.Is<ParticipantManagement>(p => p.ExceptionFlag == 1)), Times.Once());
    }
}
