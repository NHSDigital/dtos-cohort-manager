namespace screeningDataServices;

using System.Net;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

using Moq;
using Common;
using Data.Database;
using Model;
using Model.Enums;

[TestClass]
public class CreateParticipantTests
{

    private Mock<ILogger<CreateParticipant>> mockLogger;
    private Mock<ICreateResponse> mockCreateResponse;
    private Mock<ICreateParticipantData> mockCreateParticipantData;
    private Mock<Participant> mockParticipantDetails;

    private readonly Mock<ICheckDemographic> CheckDemographic = new();

    Mock<FunctionContext> mockContext;
    Mock<HttpRequestData> mockRequest;

    [TestInitialize]
    public void Setup()
    {
        // Arrange
        var encoding = Encoding.UTF8;

        mockLogger = new Mock<ILogger<CreateParticipant>>();
        mockCreateResponse = new Mock<ICreateResponse>();
        mockCreateParticipantData = new Mock<ICreateParticipantData>();
        mockParticipantDetails = GenerateMockModelParticipantDetails();
        mockContext = new Mock<FunctionContext>();
    }

    [TestMethod]
    public async Task Run_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        string requestBody = @"{
            ""nhsnumber"": ""1234567890"",
            ""supersededByNhsNumber"": ""0987654321""
            }";
        var mockRequest = MockHelpers.CreateMockHttpRequestData(requestBody);

        var createParticipant = new CreateParticipant(mockLogger.Object, mockCreateResponse.Object, mockCreateParticipantData.Object);
        mockCreateParticipantData.Setup(data => data.CreateParticipantEntryAsync(It.IsAny<Participant>(), It.IsAny<string>())).Returns(true);

        // Act
        var response = await createParticipant.Run(mockRequest);

        // Assert
        mockCreateResponse.Verify(response => response.CreateHttpResponse(HttpStatusCode.OK, It.IsAny<HttpRequestData>(), ""), Times.Once);
        mockCreateResponse.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_InvalidRequest_Returns404()
    {
        // Arrange
        mockRequest = new Mock<HttpRequestData>(mockContext.Object);
        var createParticipant = new CreateParticipant(mockLogger.Object, mockCreateResponse.Object, mockCreateParticipantData.Object);
        mockCreateParticipantData.Setup(data => data.CreateParticipantEntryAsync(It.IsAny<Participant>(), It.IsAny<string>())).Returns(false);

        // Act
        var response = await createParticipant.Run(mockRequest.Object);

        // Assert
        mockCreateResponse.Verify(response => response.CreateHttpResponse(HttpStatusCode.InternalServerError, It.IsAny<HttpRequestData>(), ""), Times.Once);
        mockCreateResponse.VerifyNoOtherCalls();
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
