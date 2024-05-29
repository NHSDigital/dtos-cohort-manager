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
    private readonly Mock<ILogger<LookupValidation>> loggerMock;
    private readonly ServiceCollection serviceCollection;
    private readonly Mock<FunctionContext> context;
    private readonly Mock<HttpRequestData> request;
    private readonly LookupValidationRequestBody requestBody;
    private readonly LookupValidation function;
    private readonly Mock<IValidationData> _validationDataService = new();

    public ValidationDataServiceTests()
    {
        loggerMock = new Mock<ILogger<LookupValidation>>();
        context = new Mock<FunctionContext>();
        request = new Mock<HttpRequestData>(context.Object);

        serviceCollection = new ServiceCollection();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        context.SetupProperty(c => c.InstanceServices, serviceProvider);

        var existingParticipant = new Participant
        {
            NHSId = "1",
            FirstName = "John",
            Surname = "Smith"
        };
        var newParticipant = new Participant
        {
            NHSId = "1",
            FirstName = "John",
            Surname = "Smith"
        };
        requestBody = new LookupValidationRequestBody("UpdateParticipant", existingParticipant, newParticipant);

        function = new LookupValidation(loggerMock.Object, _validationDataService.Object);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(null)]
    [DataRow(" ")]
    public async Task Run_Should_Return_Rule_Violation_When_Attempting_To_Update_Participant_That_Does_Not_Exist(string nhsNumber)
    {
        // Arrange
        requestBody.ExistingParticipant.NHSId = nhsNumber;
        var json = JsonSerializer.Serialize(requestBody);
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
        requestBody.ExistingParticipant.NHSId = nhsNumber;
        var json = JsonSerializer.Serialize(requestBody);
        SetupRequest(json);

        // Act
        var result = await function.RunAsync(request.Object);

        // Assert
        string body = ReadStream(result.Body);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        Assert.IsTrue(!body.Contains("ParticipantMustAlreadyExist"));
    }

    [TestMethod]
    [DataRow("0000000000")]
    [DataRow("9999999999")]
    public async Task Run_Should_Return_Rule_Violation_When_Attempting_To_Add_Participant_That_Already_Exists(string nhsNumber)
    {
        // Arrange
        requestBody.Workflow = "AddParticipant";
        requestBody.ExistingParticipant.NHSId = nhsNumber;
        var json = JsonSerializer.Serialize(requestBody);
        SetupRequest(json);

        // Act
        var result = await function.RunAsync(request.Object);

        // Assert
        string body = ReadStream(result.Body);
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.IsTrue(body.Contains("1.ParticipantMustNotAlreadyExist"));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(null)]
    [DataRow(" ")]
    public async Task Run_Should_Not_Return_Rule_Violation_When_Attempting_To_Add_Participant_That_Does_Not_Already_Exist(string nhsNumber)
    {
        // Arrange
        requestBody.Workflow = "AddParticipant";
        requestBody.ExistingParticipant.NHSId = nhsNumber;
        var json = JsonSerializer.Serialize(requestBody);
        SetupRequest(json);

        // Act
        var result = await function.RunAsync(request.Object);

        // Assert
        string body = ReadStream(result.Body);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        Assert.IsTrue(!body.Contains("1.ParticipantMustNotAlreadyExist"));
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
