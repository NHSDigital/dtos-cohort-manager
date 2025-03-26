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
using Microsoft.Extensions.Options;
using NHS.Screening.CreateParticipant;

[TestClass]
public class CreateParticipantTests
{
    private readonly Mock<ILogger<ScreeningDataServices.CreateParticipant>> _mockLogger = new();
    private readonly Mock<ICreateResponse> _mockCreateResponse = new();
    private readonly Mock<IExceptionHandler> _handleException = new();
    private readonly Mock<IDataServiceClient<ParticipantManagement>> _participantManagementClient = new();
    private ParticipantCsvRecord _participantCsvRecord = new();
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly ScreeningDataServices.CreateParticipant _sut;
    private readonly Mock<IOptions<CreateParticipantConfig>> _config = new();

    public CreateParticipantTests()
    {
        _participantCsvRecord.Participant = new Participant
        {
            NhsNumber = "1234567890",
            ScreeningId = "1"
        };

        _callFunction
            .Setup(x => x.GetResponseText(It.IsAny<HttpWebResponse>()))
            .ReturnsAsync(JsonSerializer.Serialize(
                new ValidationExceptionLog { IsFatal = false, CreatedException = false }));

        var testConfig = new CreateParticipantConfig
        {
            ParticipantManagementUrl = "test-url",
            LookupValidationURL = "test-url-2"
        };

        _config.Setup(c => c.Value).Returns(testConfig);

        _participantManagementClient
            .Setup(data => data.Add(It.IsAny<ParticipantManagement>()))
            .ReturnsAsync(true);


        _sut = new(
            _mockLogger.Object,
            _mockCreateResponse.Object,
            _handleException.Object,
            _callFunction.Object,
            _participantManagementClient.Object,
            _config.Object);
    }


    [TestMethod]
    public async Task Run_ValidRequest_UpdateAndReturnOk()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        var mockRequest = MockHelpers.CreateMockHttpRequestData(json);

        // Act
        await _sut.Run(mockRequest);

        // Assert
        _participantManagementClient
            .Verify(x => x.Add(It.IsAny<ParticipantManagement>()),
            Times.Once);

        _mockCreateResponse
            .Verify(response => response.CreateHttpResponse(
                HttpStatusCode.OK, It.IsAny<HttpRequestData>(), ""),
            Times.Once);
    }


    [DataRow("2025")]
    [DataRow("202501")]
    public async Task Run_ParticipantHasPartialDates_TransformDatesAndAdd(string rfrDate)
    {
        // Arrange
        var expectedParticipant = _participantCsvRecord.Participant.ToParticipantManagement();

        var json = JsonSerializer.Serialize(_participantCsvRecord);
        var mockRequest = MockHelpers.CreateMockHttpRequestData(json);

        // Act
        var response = await _sut.Run(mockRequest);

        // Assert
        _mockCreateResponse
            .Verify(response => response.CreateHttpResponse(HttpStatusCode.OK, It.IsAny<HttpRequestData>(), ""),
                Times.Once());
        _participantManagementClient
            .Verify(x => x.Add(It.Is<ParticipantManagement>(x => x.ReasonForRemovalDate == expectedParticipant.ReasonForRemovalDate)),
                Times.Once());
    }

    [TestMethod]
    public async Task Run_AddFails_ReturnInternalServerError()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        var mockRequest = MockHelpers.CreateMockHttpRequestData(json);

        _participantManagementClient
            .Setup(data => data.Add(It.IsAny<ParticipantManagement>()))
            .ReturnsAsync(false);

        // Act
        await _sut.Run(mockRequest);

        // Assert
        _mockCreateResponse
            .Verify(response => response.CreateHttpResponse(
                HttpStatusCode.InternalServerError,
                It.IsAny<HttpRequestData>(), ""),
            Times.Once);
    }

    [TestMethod]
    public async Task Run_AddThrowsException_ReturnInternalServerError()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        var mockRequest = MockHelpers.CreateMockHttpRequestData(json);

        _participantManagementClient
            .Setup(data => data.Add(It.IsAny<ParticipantManagement>()))
            .Throws(new Exception("someError"));

        // Act
        await _sut.Run(mockRequest);

        // Assert
        _mockCreateResponse
            .Verify(response => response.CreateHttpResponse(
                HttpStatusCode.InternalServerError, It.IsAny<HttpRequestData>(), ""),
            Times.Once);
    }

    [TestMethod]
    public async Task Run_FatalRuleTriggered_DoNotUpdateAndReturnCreated()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        var mockRequest = MockHelpers.CreateMockHttpRequestData(json);

        _callFunction
            .Setup(x => x.GetResponseText(It.IsAny<HttpWebResponse>()))
            .ReturnsAsync(JsonSerializer.Serialize(
                new ValidationExceptionLog { IsFatal = true, CreatedException = false }
            ));

        // Act
        await _sut.Run(mockRequest);

        // Assert
        _participantManagementClient
            .Verify(x => x.Update(It.IsAny<ParticipantManagement>()), Times.Never());

        _mockCreateResponse
            .Verify(response => response.CreateHttpResponse(
                HttpStatusCode.Created, It.IsAny<HttpRequestData>(), ""),
            Times.Once);
    }

    [TestMethod]
    public async Task Run_NonFatalRuleTriggered_SetExceptionFlagAndReturnOk()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        var mockRequest = MockHelpers.CreateMockHttpRequestData(json);

        _callFunction
            .Setup(x => x.GetResponseText(It.IsAny<HttpWebResponse>()))
            .ReturnsAsync(JsonSerializer.Serialize(
                new ValidationExceptionLog { IsFatal = false, CreatedException = true }
            ));

        // Act
        await _sut.Run(mockRequest);

        // Assert
        _participantManagementClient
            .Verify(x => x.Add(It.Is<ParticipantManagement>(p => p.ExceptionFlag == 1)),
            Times.Once);

        _mockCreateResponse
            .Verify(response => response.CreateHttpResponse(
                HttpStatusCode.OK, It.IsAny<HttpRequestData>(), ""),
            Times.Once);
    }


}
