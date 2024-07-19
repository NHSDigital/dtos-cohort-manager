namespace NHS.CohortManager.Tests.ScreeningValidationServiceTests;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Model;
using Moq;
using NHS.CohortManager.ScreeningValidationService;
using NHS.CohortManager.Tests.TestUtils;
using RulesEngine.Models;

[TestClass]
public class LookupValidationTests
{
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly Mock<FunctionContext> _context = new();
    private readonly Mock<HttpRequestData> _request;
    private readonly Mock<IExceptionHandler> _handleException = new();
    private readonly CreateResponse _createResponse = new();
    private readonly ServiceCollection _serviceCollection = new();
    private readonly LookupValidationRequestBody _requestBody;
    private readonly LookupValidation _function;


    public LookupValidationTests()
    {
        Environment.SetEnvironmentVariable("CreateValidationExceptionURL", "CreateValidationExceptionURL");

        _request = new Mock<HttpRequestData>(_context.Object);

        var serviceProvider = _serviceCollection.BuildServiceProvider();

        _context.SetupProperty(c => c.InstanceServices, serviceProvider);

        var existingParticipant = new Participant
        {
            NhsNumber = "1",
            FirstName = "John",
            Surname = "Smith"
        };
        var newParticipant = new Participant
        {
            NhsNumber = "1",
            FirstName = "John",
            Surname = "Smith"
        };
        _requestBody = new LookupValidationRequestBody(existingParticipant, newParticipant, "caas.csv");

        _handleException.Setup(x => x.CreateValidationExceptionLog(It.IsAny<IEnumerable<RuleResultTree>>(), It.IsAny<ParticipantCsvRecord>()))
            .Returns(Task.FromResult(true)).Verifiable();

        _function = new LookupValidation(_createResponse, _handleException.Object);

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
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
        _callFunction.Verify(call => call.SendPost(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    public async Task Run_Should_Return_BadRequest_When_Request_Body_Invalid()
    {
        // Arrange
        SetUpRequestBody("Invalid request body");

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
        _callFunction.Verify(call => call.SendPost(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(null)]
    [DataRow(" ")]
    public async Task Run_Should_Return_OK_And_Create_Exception_When_AmendedParticipantMustExist_Rule_Fails(string nhsNumber)
    {
        // Arrange
        _requestBody.NewParticipant.RecordType = Actions.Amended;
        _requestBody.ExistingParticipant.NhsNumber = nhsNumber;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.IsAny<IEnumerable<RuleResultTree>>(),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }

    [TestMethod]
    [DataRow("0000000000")]
    [DataRow("9999999999")]
    public async Task Run_Should_Not_Create_Exception_When_AmendedParticipantMustExist_Rule_Passes(string nhsNumber)
    {
        // Arrange
        _requestBody.NewParticipant.RecordType = Actions.Amended;
        _requestBody.ExistingParticipant.NhsNumber = nhsNumber;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.IsAny<IEnumerable<RuleResultTree>>(),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow("0000000000")]
    [DataRow("9999999999")]
    public async Task Run_Should_Return_OK_And_Create_Exception_When_NewParticipantMustNotAlreadyExist_Rule_Fails(string nhsNumber)
    {
        // Arrange
        _requestBody.NewParticipant.RecordType = Actions.New;
        _requestBody.ExistingParticipant.NhsNumber = nhsNumber;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.IsAny<IEnumerable<RuleResultTree>>(),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(null)]
    [DataRow(" ")]
    public async Task Run_Should_Not_Create_Exception_When_NewParticipantMustNotAlreadyExist_Rule_Passes(string nhsNumber)
    {
        // Arrange
        _requestBody.NewParticipant.RecordType = Actions.New;
        _requestBody.ExistingParticipant.NhsNumber = nhsNumber;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.IsAny<IEnumerable<RuleResultTree>>(),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(null)]
    [DataRow(" ")]
    public async Task Run_Should_Return_OK_And_Create_Exception_When_RemovedParticipantMustExist_Rule_Fails(string nhsNumber)
    {
        // Arrange
        _requestBody.NewParticipant.RecordType = Actions.Removed;
        _requestBody.ExistingParticipant.NhsNumber = nhsNumber;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.IsAny<IEnumerable<RuleResultTree>>(),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }

    [TestMethod]
    [DataRow("0000000000")]
    [DataRow("9999999999")]
    public async Task Run_Should_Not_Create_Exception_When_RemovedParticipantMustExist_Rule_Passes(string nhsNumber)
    {
        // Arrange
        _requestBody.NewParticipant.RecordType = Actions.Removed;
        _requestBody.ExistingParticipant.NhsNumber = nhsNumber;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.IsAny<IEnumerable<RuleResultTree>>(),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    private void SetUpRequestBody(string json)
    {
        var byteArray = Encoding.ASCII.GetBytes(json);
        var bodyStream = new MemoryStream(byteArray);

        _request.Setup(r => r.Body).Returns(bodyStream);
    }
}
