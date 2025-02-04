namespace NHS.CohortManager.Tests.UnitTests.ScreeningDataServicesTests;

using System.Data;
using System.Net;
using Data.Database;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;
using Moq;
using updateParticipantDetails;
using DataServices.Client;
using Common;
using System.Linq.Expressions;
using NHS.CohortManager.Tests.TestUtils;
using Microsoft.Azure.Functions.Worker;
using System.Text.Json;

[TestClass]
public class UpdateParticipantDetailsTests
{
    private readonly ParticipantCsvRecord _participantCsvRecord;
    private Mock<IDataServiceClient<ParticipantManagement>> _participantManagementClientMock = new();
    private readonly Mock<ILogger<UpdateParticipantDetails>> _loggerMock = new();
    private readonly Mock<ICallFunction> _callFunctionMock = new();
    private readonly Mock<CreateResponse> _createResponseMock = new();
    private readonly Mock<IExceptionHandler> _exceptionHandlerMock = new();
    private readonly SetupRequest _setupRequest = new();
    private readonly Mock<HttpWebResponse> _LookupValidationWebResponse = new();
    private ValidationExceptionLog _lookupValidationResponseBody = new();

    public UpdateParticipantDetailsTests()
    {
        Environment.SetEnvironmentVariable("LookupValidationURL", "LookupValidationURL");

        _participantManagementClientMock
            .Setup(c => c.Update(It.IsAny<ParticipantManagement>()))
            .ReturnsAsync(true);

        _participantManagementClientMock
            .Setup(c => c.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
            .ReturnsAsync(new ParticipantManagement {NHSNumber = 1, ScreeningId = 1});

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

        _LookupValidationWebResponse
            .Setup(m => m.StatusCode)
            .Returns(HttpStatusCode.OK);

        _callFunctionMock
            .Setup(m => m.SendPost("LookupValidationURL", It.IsAny<string>()))
            .ReturnsAsync(_LookupValidationWebResponse.Object);

        _lookupValidationResponseBody.CreatedException = false;
        _lookupValidationResponseBody.IsFatal = false;
        string lookupResponseJson = JsonSerializer.Serialize(_lookupValidationResponseBody);

        _callFunctionMock
            .Setup(x => x.GetResponseText(It.IsAny<HttpWebResponse>()))
            .ReturnsAsync(lookupResponseJson);
    }

    [TestMethod]
    public async Task Run_ValidRequest_ReturnOk()
    {
        // Arrange
        var sut = new UpdateParticipantDetails(_loggerMock.Object, _createResponseMock.Object, _exceptionHandlerMock.Object,
                                                _callFunctionMock.Object, _participantManagementClientMock.Object);
        
        string json = JsonSerializer.Serialize(_participantCsvRecord);
        var request = _setupRequest.Setup(json);

        // Act
        var response = await sut.Run(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task Run_GetOldParticipantFails_ReturnInternalServerError()
    {
        // Arrange
        _participantManagementClientMock
            .Setup(c => c.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
            .Throws(new Exception());

        var sut = new UpdateParticipantDetails(_loggerMock.Object, _createResponseMock.Object, _exceptionHandlerMock.Object,
                                                _callFunctionMock.Object, _participantManagementClientMock.Object);

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
        _callFunctionMock
            .Setup(x => x.GetResponseText(It.IsAny<HttpWebResponse>()))
            .Throws(new Exception());

        var sut = new UpdateParticipantDetails(_loggerMock.Object, _createResponseMock.Object, _exceptionHandlerMock.Object,
                                                _callFunctionMock.Object, _participantManagementClientMock.Object);

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

        _callFunctionMock
            .Setup(x => x.GetResponseText(It.IsAny<HttpWebResponse>()))
            .ReturnsAsync(lookupResponseJson);

        var sut = new UpdateParticipantDetails(_loggerMock.Object, _createResponseMock.Object, _exceptionHandlerMock.Object,
                                                _callFunctionMock.Object, _participantManagementClientMock.Object);

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

        _callFunctionMock
            .Setup(x => x.GetResponseText(It.IsAny<HttpWebResponse>()))
            .ReturnsAsync(lookupResponseJson);

        var sut = new UpdateParticipantDetails(_loggerMock.Object, _createResponseMock.Object, _exceptionHandlerMock.Object,
                                                _callFunctionMock.Object, _participantManagementClientMock.Object);

        string json = JsonSerializer.Serialize(_participantCsvRecord);
        var request = _setupRequest.Setup(json);

        // Act
        var response = await sut.Run(request.Object);

        // Assert
        _participantManagementClientMock
            .Verify(x => x.Update(It.Is<ParticipantManagement>(p => p.ExceptionFlag == 1)), Times.Once());
    }
}
