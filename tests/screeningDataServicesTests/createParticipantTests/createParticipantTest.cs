namespace NHS.CohortManager.Tests.ScreeningDataServicesTests;

using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Common;
using Data.Database;
using Model;
using Model.Enums;
using screeningDataServices;

[TestClass]
public class CreateParticipantTests
{
    private readonly Mock<ILogger<screeningDataServices.CreateParticipant>> _mockLogger = new();
    private readonly Mock<ICreateResponse> _mockCreateResponse = new();
    private readonly Mock<ICreateParticipantData> _mockCreateParticipantData = new();
    private readonly Mock<FunctionContext> _mockContext = new();
    private Mock<HttpRequestData> _mockRequest;

    [TestMethod]
    public async Task Run_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        string requestBody = @"{
            ""nhsnumber"": ""1234567890"",
            ""supersededByNhsNumber"": ""0987654321""
            }";
        var mockRequest = MockHelpers.CreateMockHttpRequestData(requestBody);

        var sut = new screeningDataServices.CreateParticipant(_mockLogger.Object, _mockCreateResponse.Object, _mockCreateParticipantData.Object);
        _mockCreateParticipantData.Setup(data => data.CreateParticipantEntry(It.IsAny<ParticipantCsvRecord>())).Returns(true);

        // Act
        var response = await sut.Run(mockRequest);

        // Assert
        _mockCreateResponse.Verify(response => response.CreateHttpResponse(HttpStatusCode.OK, It.IsAny<HttpRequestData>(), ""), Times.Once);
        _mockCreateResponse.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_InvalidRequest_Returns404()
    {
        // Arrange
        _mockRequest = new Mock<HttpRequestData>(_mockContext.Object);
        var sut = new screeningDataServices.CreateParticipant(_mockLogger.Object, _mockCreateResponse.Object, _mockCreateParticipantData.Object);
        _mockCreateParticipantData.Setup(data => data.CreateParticipantEntry(It.IsAny<ParticipantCsvRecord>())).Returns(false);

        // Act
        var response = await sut.Run(_mockRequest.Object);

        // Assert
        _mockCreateResponse.Verify(response => response.CreateHttpResponse(HttpStatusCode.InternalServerError, It.IsAny<HttpRequestData>(), ""), Times.Once);
        _mockCreateResponse.VerifyNoOtherCalls();
    }

    private Mock<Participant> GenerateMockModelParticipantDetails()
    {
        var participantMock = new Mock<Participant>();
        participantMock.SetupAllProperties();

        participantMock.Object.NHSId = "1234567890";
        participantMock.Object.SupersededByNhsNumber = "0987654321";
        participantMock.Object.PrimaryCareProvider = "";
        participantMock.Object.NamePrefix = "";
        participantMock.Object.FirstName = "";
        participantMock.Object.OtherGivenNames = "";
        participantMock.Object.Surname = "";
        participantMock.Object.DateOfBirth = "";
        participantMock.Object.Gender = Gender.NotKnown;
        participantMock.Object.AddressLine1 = "";
        participantMock.Object.AddressLine2 = "";
        participantMock.Object.AddressLine3 = "";
        participantMock.Object.AddressLine4 = "";
        participantMock.Object.AddressLine5 = "";
        participantMock.Object.Postcode = "";
        participantMock.Object.ReasonForRemoval = "";
        participantMock.Object.ReasonForRemovalEffectiveFromDate = "";
        participantMock.Object.DateOfDeath = "";
        participantMock.Object.TelephoneNumber = "";
        participantMock.Object.MobileNumber = "";
        participantMock.Object.EmailAddress = "";
        participantMock.Object.PreferredLanguage = "";
        participantMock.Object.IsInterpreterRequired = "0";
        participantMock.Object.RecordType = "";

        return participantMock;
    }
}
