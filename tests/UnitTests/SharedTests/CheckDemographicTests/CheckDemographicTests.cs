namespace NHS.CohortManager.Tests.UnitTests.CheckDemographicTests;

using Common;
using Microsoft.Identity.Client;
using Model.Enums;
using NHS.CohortManager.Tests.TestUtils;
using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Common;
using Model;
using RulesEngine.Models;
using NHS.CohortManager.Tests.TestUtils;
using Model.Enums;

using System.Text.Json;
using System.Threading.Tasks;

[TestClass]
public class CheckDemographicTests 
{
    private readonly Mock<ILogger<CheckDemographic>> _logger = new();
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly Mock<HttpClient> _httpClient = new();
    private readonly CheckDemographic _checkDemographic;

    public CheckDemographicTests()
    { 
        _checkDemographic = new CheckDemographic(_callFunction.Object, _logger.Object, _httpClient.Object);
    }

    [TestMethod]
    public async Task GetDemographicAsync_ValidInput_ReturnDemographic()
    {
        //Arrange
        var uri = "test-uri.com/get";
        var nhsNumber = "1234567890";

        var demographic = new Demographic
        {
            FirstName = "John",
            NhsNumber = nhsNumber
        };

        _callFunction.Setup(x => x.SendGet(It.IsAny<string>()))
            .ReturnsAsync(JsonSerializer.Serialize(demographic))
            .Verifiable();


        //Act
        var result = await _checkDemographic.GetDemographicAsync(uri, nhsNumber);

        //Assert
        Assert.AreEqual(nhsNumber, result.NhsNumber);
        Assert.AreEqual(demographic.FirstName, result.FirstName);
    }

    [TestMethod]
    public async Task PostDemographicDataAsync_NoParticipants_ReturnTrue()
    {
        // Arrange
        var participants = new List<ParticipantDemographic>();
        var uri = "test-uri.com/post";

        // Act
        var result = await _checkDemographic.PostDemographicDataAsync(participants, uri);

        // Assert
        Assert.IsTrue(result);
        _logger.Verify(x => x.Log(
            It.Is<Microsoft.Extensions.Logging.LogLevel>(l => l == Microsoft.Extensions.Logging.LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("There were no items to to send to the demographic durable function")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once());
    }

    [TestMethod]
    public async Task PostDemographicDataAsync_ParticipantsExist_ReturnTrue()
    {
        // Arrange
        var participants = new List<ParticipantDemographic>
        {
            new ParticipantDemographic { /* populate properties */ }
        };
        var uri = "test-uri.com/post";

        var response = MockHelpers.CreateMockHttpResponseData(HttpStatusCode.OK, "[]");

        _callFunction.Setup(x => x.SendGet(It.IsAny<string>()))
            .Returns(Task.FromResult("status"))
            .Verifiable();

        // Act
        var result = await _checkDemographic.PostDemographicDataAsync(participants, uri);

        // Assert
        Assert.IsTrue(result);
    }

    // [TestMethod]
    // public async Task GetStatus_ValidResponse_ReturnWorkflowStatus()
    // {
    //     // Arrange
    //     var uri = "test-uri.com/get-status";
    //     var participants = new List<ParticipantDemographic>
    //     {
    //         new ParticipantDemographic { /* populate properties */ }
    //     };
    //     var webhookResponse = new WebhookResponse { RuntimeStatus = "Completed" };
        
    //     Dictionary<string, string> headers = new Dictionary<string, string>();
    //     headers["Location"] = "TestLocation";
    //     var response = MockHelpers.CreateMockHttpResponseData(HttpStatusCode.OK, JsonSerializer.Serialize(webhookResponse), headers);

    //     _httpClient.Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
    //                  .ReturnsAsync(response);

    //     // Act
    //     var result = await _checkDemographic.PostDemographicDataAsync(participants, uri);

    //     // Assert
    //     Assert.AreEqual(true, result);
    //     _logger.Verify(x => x.Log(
    //         It.Is<Microsoft.Extensions.Logging.LogLevel>(l => l == Microsoft.Extensions.Logging.LogLevel.Warning),
    //         It.IsAny<EventId>(),
    //         It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Durable function completed")),
    //         null,
    //         It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
    //         Times.Once());
    // }

    // [TestMethod]
    // public async Task GetStatus_ResponseError_UnknownStatus()
    // {
    //     // Arrange
    //     var uri = "test-uri.com/get-status";
    //     var httpResponseMessage = new HttpResponseMessage
    //     {
    //         StatusCode = HttpStatusCode.BadRequest
    //     };
    //     _httpClient.SendAsync(Arg.Is<HttpRequestMessage>(m => m.Method == HttpMethod.Get)).Returns(Task.FromResult(httpResponseMessage));

    //     // Act
    //     var result = await _checkDemographic.GetStatus(uri);

    //     // Assert
    //     Assert.AreEqual(WorkFlowStatus.Unknown, result);
    //     _logger.Verify(x => x.LogWarning(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    // }
}
