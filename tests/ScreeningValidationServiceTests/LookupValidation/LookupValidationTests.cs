namespace NHS.CohortManager.Tests.ScreeningValidationServiceTests;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Model;
using Model.Enums;
using Moq;
using NHS.CohortManager.ScreeningValidationService;
using RulesEngine.Models;

[TestClass]
public class LookupValidationTests
{
    private readonly Mock<FunctionContext> _context = new();
    private readonly Mock<HttpRequestData> _request;
    private readonly Mock<IExceptionHandler> _exceptionHandler = new();
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

        _exceptionHandler.Setup(x => x.CreateValidationExceptionLog(It.IsAny<IEnumerable<RuleResultTree>>(), It.IsAny<ParticipantCsvRecord>()))
            .Returns(Task.FromResult(true));

        _function = new LookupValidation(_createResponse, _exceptionHandler.Object);

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
        _exceptionHandler.Verify(handler => handler.CreateValidationExceptionLog(
            It.IsAny<IEnumerable<RuleResultTree>>(),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
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
        _exceptionHandler.Verify(handler => handler.CreateValidationExceptionLog(
            It.IsAny<IEnumerable<RuleResultTree>>(),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow(Actions.Amended, "")]
    [DataRow(Actions.Amended, null)]
    [DataRow(Actions.Amended, " ")]
    [DataRow(Actions.Removed, "")]
    [DataRow(Actions.Removed, null)]
    [DataRow(Actions.Removed, " ")]
    public async Task Run_Should_Return_Created_And_Create_Exception_When_ParticipantMustExist_Rule_Fails(string recordType, string nhsNumber)
    {
        // Arrange
        _requestBody.NewParticipant.RecordType = recordType;
        _requestBody.ExistingParticipant.NhsNumber = nhsNumber;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _exceptionHandler.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "22.ParticipantMustExist")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }

    [TestMethod]
    [DataRow(Actions.New, "")]
    [DataRow(Actions.New, null)]
    [DataRow(Actions.New, " ")]
    public async Task Run_Should_Not_Create_Exception_When_ParticipantMustExist_Rule_Passes(string recordType, string nhsNumber)
    {
        // Arrange
        _requestBody.NewParticipant.RecordType = recordType;
        _requestBody.ExistingParticipant.NhsNumber = nhsNumber;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _exceptionHandler.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "22.ParticipantMustExist")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow("0000000000")]
    [DataRow("9999999999")]
    public async Task Run_Should_Return_Created_And_Create_Exception_When_ParticipantMustNotExist_Rule_Fails(string nhsNumber)
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
        _exceptionHandler.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "ParticipantMustNotExist")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(null)]
    [DataRow(" ")]
    public async Task Run_Should_Not_Create_Exception_When_ParticipantMustNotExist_Rule_Passes(string nhsNumber)
    {
        // Arrange
        _requestBody.NewParticipant.RecordType = Actions.New;
        _requestBody.ExistingParticipant.NhsNumber = nhsNumber;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _exceptionHandler.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "ParticipantMustNotExist")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow("Smith", Gender.Female, "19700101", "Jones", Gender.Male, "19700101")]     // New Family Name & Gender
    [DataRow("Smith", Gender.Female, "19700101", "Jones", Gender.Female, "19700102")]   // New Family Name & Date of Birth
    [DataRow("Smith", Gender.Female, "19700101", "Smith", Gender.Male, "19700102")]     // New Gender & Date of Birth
    [DataRow("Smith", Gender.Female, "19700101", "Jones", Gender.Male, "19700102")]     // New Family Name, Gender & Date of Birth
    public async Task Run_Should_Return_Created_And_Create_Exception_When_Demographics_Rule_Fails(
        string existingFamilyName, Gender existingGender, string existingDateOfBirth, string newFamilyName, Gender newGender, string newDateOfBirth)
    {
        // Arrange
        _requestBody.ExistingParticipant.Surname = existingFamilyName;
        _requestBody.ExistingParticipant.Gender = existingGender;
        _requestBody.ExistingParticipant.DateOfBirth = existingDateOfBirth;
        _requestBody.NewParticipant.Surname = newFamilyName;
        _requestBody.NewParticipant.Gender = newGender;
        _requestBody.NewParticipant.DateOfBirth = newDateOfBirth;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _exceptionHandler.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "11.Demographics")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }


    [TestMethod]
    [DataRow("Smith", Gender.Female, "19700101", "Jones", Gender.Female, "19700101")]   // New Family Name Only
    [DataRow("Smith", Gender.Female, "19700101", "Smith", Gender.Male, "19700101")]     // New Gender Only
    [DataRow("Smith", Gender.Female, "19700101", "Smith", Gender.Female, "19700102")]   // New Date of Birth Only
    [DataRow("Smith", Gender.Female, "19700101", "Smith", Gender.Female, "19700101")]   // No Change
    public async Task Run_Should_Not_Create_Exception_When_Demographics_Rule_Passes(
        string existingFamilyName, Gender existingGender, string existingDateOfBirth, string newFamilyName, Gender newGender, string newDateOfBirth)
    {
        // Arrange
        _requestBody.ExistingParticipant.Surname = existingFamilyName;
        _requestBody.ExistingParticipant.Gender = existingGender;
        _requestBody.ExistingParticipant.DateOfBirth = existingDateOfBirth;
        _requestBody.NewParticipant.Surname = newFamilyName;
        _requestBody.NewParticipant.Gender = newGender;
        _requestBody.NewParticipant.DateOfBirth = newDateOfBirth;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _exceptionHandler.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "11.Demographics")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Never());
    }

    [TestMethod]
    [DataRow("221B Baker Street", "Flat 1", "Marylebone", "Westminster", "London", null, null, null, null, null)]
    [DataRow("221B Baker Street", "Flat 1", "Marylebone", "Westminster", "London", "", "", "", "", "")]
    [DataRow("221B Baker Street", null, null, null, null, "", "", "", "", "")]
    [DataRow(null, "Flat 1", null, null, null, "", "", "", "", "")]
    [DataRow(null, null, "Marylebone", null, null, "", "", "", "", "")]
    [DataRow(null, null, null, "Westminster", null, "", "", "", "", "")]
    [DataRow(null, null, null, null, "London", "", "", "", "", "")]
    public async Task Run_Should_Return_Created_And_Create_Exception_When_Address_Rule_Fails(
        string existingAddressLine1, string existingAddressLine2, string existingAddressLine3, string existingAddressLine4, string existingAddressLine5,
        string newAddressLine1, string newAddressLine2, string newAddressLine3, string newAddressLine4, string newAddressLine5)
    {
        // Arrange
        _requestBody.NewParticipant.RecordType = Actions.Amended;
        _requestBody.ExistingParticipant.AddressLine1 = existingAddressLine1;
        _requestBody.ExistingParticipant.AddressLine2 = existingAddressLine2;
        _requestBody.ExistingParticipant.AddressLine3 = existingAddressLine3;
        _requestBody.ExistingParticipant.AddressLine4 = existingAddressLine4;
        _requestBody.ExistingParticipant.AddressLine5 = existingAddressLine5;
        _requestBody.NewParticipant.AddressLine1 = newAddressLine1;
        _requestBody.NewParticipant.AddressLine2 = newAddressLine2;
        _requestBody.NewParticipant.AddressLine3 = newAddressLine3;
        _requestBody.NewParticipant.AddressLine4 = newAddressLine4;
        _requestBody.NewParticipant.AddressLine5 = newAddressLine5;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        _exceptionHandler.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "71.MustNotOverwriteAddressWithEmpty")),
            It.IsAny<ParticipantCsvRecord>()),
            Times.Once());
    }

    [TestMethod]
    [DataRow(Actions.Amended, null, null, null, null, null, "221B Baker Street", "Flat 2", "Marylebone", "Westminster", "London")]
    [DataRow(Actions.Amended, "221B Baker Street", "Flat 1", "Marylebone", "Westminster", "London", "221B Baker Street", "Flat 2", "Marylebone", "Westminster", "London")]
    [DataRow(Actions.Amended, "221B Baker Street", "Flat 1", "Marylebone", "Westminster", "London", "221B Baker Street", null, null, null, null)]
    [DataRow(Actions.Amended, "221B Baker Street", "Flat 1", "Marylebone", "Westminster", "London", null, "Flat 2", null, null, null)]
    [DataRow(Actions.Amended, "221B Baker Street", "Flat 1", "Marylebone", "Westminster", "London", null, null, "Marylebone", null, null)]
    [DataRow(Actions.Amended, "221B Baker Street", "Flat 1", "Marylebone", "Westminster", "London", null, null, null, "Westminster", null)]
    [DataRow(Actions.Amended, "221B Baker Street", "Flat 1", "Marylebone", "Westminster", "London", null, null, null, null, "London")]
    [DataRow(Actions.New, "221B Baker Street", "Flat 1", "Marylebone", "Westminster", "London", null, null, null, null, null)]
    public async Task Run_Should_Not_Create_Exception_When_Address_Rule_Passes(string recordType,
        string existingAddressLine1, string existingAddressLine2, string existingAddressLine3, string existingAddressLine4, string existingAddressLine5,
        string newAddressLine1, string newAddressLine2, string newAddressLine3, string newAddressLine4, string newAddressLine5)
    {
        // Arrange
        _requestBody.NewParticipant.RecordType = recordType;
        _requestBody.ExistingParticipant.AddressLine1 = existingAddressLine1;
        _requestBody.ExistingParticipant.AddressLine2 = existingAddressLine2;
        _requestBody.ExistingParticipant.AddressLine3 = existingAddressLine3;
        _requestBody.ExistingParticipant.AddressLine4 = existingAddressLine4;
        _requestBody.ExistingParticipant.AddressLine5 = existingAddressLine5;
        _requestBody.NewParticipant.AddressLine1 = newAddressLine1;
        _requestBody.NewParticipant.AddressLine2 = newAddressLine2;
        _requestBody.NewParticipant.AddressLine3 = newAddressLine3;
        _requestBody.NewParticipant.AddressLine4 = newAddressLine4;
        _requestBody.NewParticipant.AddressLine5 = newAddressLine5;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        await _function.RunAsync(_request.Object);

        // Assert
        _exceptionHandler.Verify(handleException => handleException.CreateValidationExceptionLog(
            It.Is<IEnumerable<RuleResultTree>>(r => r.Any(x => x.Rule.RuleName == "71.MustNotOverwriteAddressWithEmpty")),
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
