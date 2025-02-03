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
    private readonly Mock<HttpWebResponse> _webResponse = new();

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

        _webResponse
            .Setup(m => m.StatusCode)
            .Returns(HttpStatusCode.OK);

        _callFunctionMock
            .Setup(m => m.SendPost("LookupValidationURL", It.IsAny<string>()))
            .ReturnsAsync(_webResponse.Object);

    }

    [TestMethod]
    public async Task Run_ValidRequest_ReturnOk()
    {
        // Arrange
        _participantManagementClientMock
            .Setup(c => c.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
            .ReturnsAsync((ParticipantManagement)null);

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
        var sut = new UpdateParticipantDetails(_loggerMock.Object, _createResponseMock.Object, _exceptionHandlerMock.Object,
                                                _callFunctionMock.Object, _participantManagementClientMock.Object);

        string json = JsonSerializer.Serialize(_participantCsvRecord);
        var request = _setupRequest.Setup(json);

        // Act
        var response = await sut.Run(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    // [TestMethod]
    // public void Run_LookupValidationFails_ReturnInternalServerError()
    // {
    //     // Arrange
    //     _moqDataReader.SetupSequence(reader => reader.Read())
    //     .Returns(true)
    //     .Returns(false);


    //     _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.BadRequest);



    //     // Act
    //     var result = sut.UpdateParticipantDetails(_participantCsvRecord);
    //     // Assert
    //     Assert.IsFalse(result);
    //     _commandMock.Verify(command => command.ExecuteNonQuery(), Times.AtMost(2));
    //     //We still update the participant, but only set the Exception Flag.
    // }
}
