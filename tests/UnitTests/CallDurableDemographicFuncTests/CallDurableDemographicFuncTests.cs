namespace NHS.CohortManager.Tests.UnitTests.CheckDemographicTests;

using Common;
using NHS.CohortManager.Tests.TestUtils;
using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Model;
using System.Text.Json;
using System.Threading.Tasks;
using Moq.Protected;
using NHS.Screening.ReceiveCaasFile;

[TestClass]
public class CallDurableDemographicFuncTests
{
    private readonly Mock<ILogger<CallDurableDemographicFunc>> _logger = new();
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly Mock<HttpClient> _httpClient = new();
    private readonly CallDurableDemographicFunc _checkDemographic;

    private readonly Mock<ICopyFailedBatchToBlob> _copyFailedBatchToBlob = new();

    public CallDurableDemographicFuncTests()
    {
        _checkDemographic = new CallDurableDemographicFunc(_callFunction.Object, _logger.Object, _httpClient.Object, _copyFailedBatchToBlob.Object);
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
        //Assert.IsTrue(result);
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Information),
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

    [TestMethod]
    public async Task GetStatus_ValidResponse_ReturnWorkflowStatus()
    {
        // Arrange
        var uri = "http://test-uri.com/get-status";
        var participants = new List<ParticipantDemographic>
        {
            new ParticipantDemographic { /* populate properties if needed */ }
        };

        // Create the HttpClient with the common helper
        var httpClient = CreateMockHttpClient(HttpStatusCode.OK);
        var checkDemographic = new CallDurableDemographicFunc(_callFunction.Object, _logger.Object, httpClient, _copyFailedBatchToBlob.Object);

        // Act
        var result = await checkDemographic.PostDemographicDataAsync(participants, uri);

        // Assert
        Assert.IsTrue(result);
        _logger.Verify(x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Durable function completed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once());
    }

    [TestMethod]
    public async Task GetStatus_ResponseError_UnknownStatus()
    {
        // Arrange
        var uri = "http://test-uri.com/get-status";
        var participants = new List<ParticipantDemographic>
        {
            new ParticipantDemographic { /* populate properties if needed */ }
        };

        // Create the HttpClient with the common helper
        var httpClient = CreateMockHttpClient(HttpStatusCode.BadRequest);
        var checkDemographic = new CallDurableDemographicFunc(_callFunction.Object, _logger.Object, httpClient, _copyFailedBatchToBlob.Object);

        // Act
        var result = await checkDemographic.PostDemographicDataAsync(participants, uri);

        // Assert
        Assert.IsTrue(result);
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("still sending records to queue")
                && v.ToString().Contains("Simulated exception")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        Times.Once());

    }

    // Missing test coverage for exception state - due to the retry logic and private const _maxNumberOfChecks it would add 150 seconds to test run
    // Tried to use reflection to manually change value but can't as it's const. 
    // Reccomend to refactor CheckDemographic.cs to make it more testable.

    private HttpClient CreateMockHttpClient(HttpStatusCode responseStatusCode)
    {
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        // Setup for the POST request
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, cancellationToken) =>
            {
                Console.WriteLine($"POST Request URL: {request.RequestUri}");
            })
            .Returns<HttpRequestMessage, CancellationToken>((request, cancellationToken) =>
            {
                // For error simulation, throw an exception if the responseStatusCode is BadRequest.
                if (responseStatusCode == HttpStatusCode.BadRequest)
                {
                    throw new Exception("Simulated exception");
                }
                var response = new HttpResponseMessage(responseStatusCode)
                {
                    Content = new StringContent("ignored")
                };
                // Set a valid Location header for the GET call in GetStatus
                response.Headers.Location = new Uri("http://test-uri.com/status");
                return Task.FromResult(response);
            });

        // Setup for the GET request
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, cancellationToken) =>
            {
                Console.WriteLine($"GET Request URL: {request.RequestUri}");
            })
            .ReturnsAsync(() =>
            {
                var webhookResponse = new WebhookResponse { RuntimeStatus = "Completed" };
                var content = JsonSerializer.Serialize(webhookResponse);
                return new HttpResponseMessage(responseStatusCode)
                {
                    Content = new StringContent(content)
                };
            });

        // Create and return an HttpClient configured with the mocked handler.
        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://test-uri.com")
        };
        return httpClient;
    }

}
