namespace NHS.CohortManager.Tests.ValidationDataService;

using System.Net;
using System.Text;
using System.Text.Json;
using Data.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Model;
using Moq;
using NHS.CohortManager.ValidationDataService;

[TestClass]
public class ValidationDataServiceTests
{
    private readonly Mock<ILogger<ValidationFunction>> loggerMock;
    private readonly ServiceCollection serviceCollection;
    private readonly Mock<FunctionContext> context;
    private readonly Mock<HttpRequestData> request;
    private readonly List<Participant> participants;
    private readonly ValidationFunction function;
    private readonly Mock<IValidationData> _validationDataService = new();

    public ValidationDataServiceTests()
    {
        loggerMock = new Mock<ILogger<ValidationFunction>>();
        context = new Mock<FunctionContext>();
        request = new Mock<HttpRequestData>(context.Object);

        serviceCollection = new ServiceCollection();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        context.SetupProperty(c => c.InstanceServices, serviceProvider);

        participants =
        [
            new Participant
            {
                NHSId = "1",
                FirstName = "John",
                Surname = "Smith"
            },
            new Participant
            {
                NHSId = "1",
                FirstName = "John",
                Surname = "Smith"
            }
        ];

        function = new ValidationFunction(loggerMock.Object, _validationDataService.Object);
    }

    [TestMethod]
    public async Task Run_Should_Return_BadRequest_When_Two_Participants_Not_Received()
    {
        // Arrange
        participants.RemoveAt(1);
        var json = JsonSerializer.Serialize(participants);
        SetupRequest(json);

        // Act
        var result = await function.RunAsync(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_Should_Return_Ok_When_All_Rules_Pass()
    {
        // Arrange
        var json = JsonSerializer.Serialize(participants);
        SetupRequest(json);

        // Act
        var result = await function.RunAsync(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        Assert.AreEqual(0, result.Body.Length);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(null)]
    [DataRow(" ")]
    public async Task Run_Should_Return_Rule_Violation_When_Attempting_To_Update_Participant_That_Does_Not_Exist(string nhsNumber)
    {
        // Arrange
        participants[0].NHSId = nhsNumber;
        var json = JsonSerializer.Serialize(participants);
        SetupRequest(json);

        // Act
        var result = await function.RunAsync(request.Object);

        // Assert
        string body = ReadStream(result.Body);
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.IsTrue(body.Contains("ParticipantMustAlreadyExist"));
    }

    [TestMethod]
    [DataRow("0000000000")]
    [DataRow("9999999999")]
    public async Task Run_Should_Not_Return_Rule_Violation_When_Attempting_To_Update_Participant_That_Does_Exist(string nhsNumber)
    {
        // Arrange
        participants[0].NHSId = nhsNumber;

        var json = JsonSerializer.Serialize(participants);
        SetupRequest(json);

        // Act
        var result = await function.RunAsync(request.Object);

        // Assert
        string body = ReadStream(result.Body);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        Assert.IsTrue(!body.Contains("ParticipantMustAlreadyExist"));
    }

    private void SetupRequest(string json)
    {
        var byteArray = Encoding.ASCII.GetBytes(json);
        var bodyStream = new MemoryStream(byteArray);

        request.Setup(r => r.Body).Returns(bodyStream);
        request.Setup(r => r.CreateResponse()).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(context.Object);
            response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
            response.SetupProperty(r => r.StatusCode);
            response.SetupProperty(r => r.Body, new MemoryStream());
            return response.Object;
        });
    }

    private static string ReadStream(Stream stream)
    {
        string str;
        using (var reader = new StreamReader(stream, Encoding.UTF8))
        {
            str = reader.ReadToEnd();
        }
        return str;
    }
}
