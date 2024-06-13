namespace NHS.CohortManager.Tests.ScreeningValidationService;

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
using NHS.CohortManager.ScreeningValidationService;

[TestClass]
public class LookupValidationTests
{
    private readonly Mock<ILogger<LookupValidation>> _logger = new();
    private readonly Mock<IValidationData> _validationDataService = new();
    private readonly Mock<FunctionContext> _context = new();
    private readonly Mock<HttpRequestData> _request;
    private readonly ServiceCollection _serviceCollection = new();
    private readonly LookupValidationRequestBody _requestBody;
    private readonly LookupValidation _function;

    public LookupValidationTests()
    {
        _request = new Mock<HttpRequestData>(_context.Object);

        var serviceProvider = _serviceCollection.BuildServiceProvider();

        _context.SetupProperty(c => c.InstanceServices, serviceProvider);

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
        _requestBody = new LookupValidationRequestBody("UpdateParticipant", existingParticipant, newParticipant);

        _function = new LookupValidation(_logger.Object, _validationDataService.Object);

        _request.Setup(r => r.CreateResponse()).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(_context.Object);
            response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
            response.SetupProperty(r => r.StatusCode);
            response.SetupProperty(r => r.Body, new MemoryStream());
            return response.Object;
        });
    }

    [TestMethod]
    public async Task Run_Should_Return_BadRequest_When_Request_Body_Empty()
    {
        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _validationDataService.Verify(x => x.Create(It.IsAny<ValidationDataDto>()), Times.Never());
    }

    [TestMethod]
    public async Task Run_Should_Return_BadRequest_When_Request_Body_Invalid()
    {
        // Arrange
        SetUpRequestBody("Invalid request body");

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _validationDataService.Verify(x => x.Create(It.IsAny<ValidationDataDto>()), Times.Never());
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(null)]
    [DataRow(" ")]
    public async Task Run_Should_Return_Rule_Violation_When_Attempting_To_Update_Participant_That_Does_Not_Exist(string nhsNumber)
    {
        // Arrange
        _requestBody.ExistingParticipant.NHSId = nhsNumber;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _validationDataService.Verify(x => x.Create(It.IsAny<ValidationDataDto>()), Times.Once());
    }

    [TestMethod]
    [DataRow("0000000000")]
    [DataRow("9999999999")]
    public async Task Run_Should_Not_Return_Rule_Violation_When_Attempting_To_Update_Participant_That_Does_Exist(string nhsNumber)
    {
        // Arrange
        _requestBody.ExistingParticipant.NHSId = nhsNumber;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _validationDataService.Verify(x => x.Create(It.IsAny<ValidationDataDto>()), Times.Never());
    }

    [TestMethod]
    [DataRow("0000000000")]
    [DataRow("9999999999")]
    public async Task Run_Should_Return_Rule_Violation_When_Attempting_To_Add_Participant_That_Already_Exists(string nhsNumber)
    {
        // Arrange
        _requestBody.Workflow = "AddParticipant";
        _requestBody.ExistingParticipant.NHSId = nhsNumber;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _validationDataService.Verify(x => x.Create(It.IsAny<ValidationDataDto>()), Times.Once());
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(null)]
    [DataRow(" ")]
    public async Task Run_Should_Not_Return_Rule_Violation_When_Attempting_To_Add_Participant_That_Does_Not_Already_Exist(string nhsNumber)
    {
        // Arrange
        _requestBody.Workflow = "AddParticipant";
        _requestBody.ExistingParticipant.NHSId = nhsNumber;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _validationDataService.Verify(x => x.Create(It.IsAny<ValidationDataDto>()), Times.Never());
    }

    private void SetUpRequestBody(string json)
    {
        var byteArray = Encoding.ASCII.GetBytes(json);
        var bodyStream = new MemoryStream(byteArray);

        _request.Setup(r => r.Body).Returns(bodyStream);
    }
}
