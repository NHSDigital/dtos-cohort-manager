namespace ValidationDataServiceTests;

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
using ValidationDataService;

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
                FirstName = "John",
                Surname = "Smith"
            },
            new Participant
            {
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
    [DataRow("", "")]
    [DataRow(null, null)]
    [DataRow(" ", " ")]
    public async Task Run_Should_Return_FamilyNameProvidedRule_Violation_When_Rule_Validation_Fails(string existingSurname, string newSurname)
    {
        // Arrange
        participants[0].Surname = existingSurname;
        participants[1].Surname = newSurname;
        var json = JsonSerializer.Serialize(participants);
        SetupRequest(json);

        // Act
        var result = await function.RunAsync(request.Object);

        // Assert
        string body = ReadStream(result.Body);
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.IsTrue(body.Contains("39.FamilyNameProvidedRule"));
    }

    [TestMethod]
    [DataRow("Smith", "Smith")]
    [DataRow(null, "Smith")]
    [DataRow("Smith", null)]
    public async Task Run_Should_Not_Return_FamilyNameProvidedRule_Violation_When_Rule_Validation_Suceeds(string existingSurname, string newSurname)
    {
        // Arrange
        participants[0].Surname = existingSurname;
        participants[1].Surname = newSurname;
        var json = JsonSerializer.Serialize(participants);
        SetupRequest(json);

        // Act
        var result = await function.RunAsync(request.Object);

        // Assert
        string body = ReadStream(result.Body);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        Assert.IsFalse(body.Contains("39.FamilyNameProvidedRule"));
    }

    [TestMethod]
    [DataRow("", "")]
    [DataRow(null, null)]
    [DataRow(" ", " ")]
    public async Task Run_Should_Return_GivenNameProvidedRule_Violation_When_Rule_Validation_Fails(string existingFirstName, string newFirstName)
    {
        // Arrange
        participants[0].FirstName = existingFirstName;
        participants[1].FirstName = newFirstName;
        var json = JsonSerializer.Serialize(participants);
        SetupRequest(json);

        // Act
        var result = await function.RunAsync(request.Object);

        // Assert
        string body = ReadStream(result.Body);
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.IsTrue(body.Contains("40.GivenNameProvidedRule"));
    }

    [TestMethod]
    [DataRow("James", "James")]
    [DataRow(null, "James")]
    [DataRow("James", null)]
    public async Task Run_Should_Not_Return_GivenNameProvidedRule_Violation_When_Rule_Validation_Suceeds(string existingFirstName, string newFirstName)
    {
        // Arrange
        participants[0].FirstName = existingFirstName;
        participants[1].FirstName = newFirstName;
        var json = JsonSerializer.Serialize(participants);
        SetupRequest(json);

        // Act
        var result = await function.RunAsync(request.Object);

        // Assert
        string body = ReadStream(result.Body);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        Assert.IsFalse(body.Contains("40.GivenNameProvidedRule"));
    }

    [TestMethod]
    [DataRow("Smith", "Jones", "Male", "Female", "15/01/1970", "15/01/1970")]
    [DataRow("Smith", "Jones", "Male", "Male", "15/01/1970", "15/01/1971")]
    [DataRow("Smith", "Smith", "Male", "Female", "15/01/1970", "15/01/1971")]
    [DataRow("Smith", "Jones", "Male", "Female", "15/01/1970", "15/01/1971")]
    public async Task Run_Should_Return_TwoOfFamilyNameDateOfBirthGenderMustMatchRule_Violation_When_Rule_Validation_Fails(
        string existingSurname, string newSurname, string existingGender, string newGender, string existingDob, string newDob)
    {
        // Arrange
        participants[0].Surname = existingSurname;
        participants[0].Gender = existingGender;
        participants[0].DateOfBirth = existingDob;
        participants[1].Surname = newSurname;
        participants[1].Gender = newGender;
        participants[1].DateOfBirth = newDob;
        var json = JsonSerializer.Serialize(participants);
        SetupRequest(json);

        // Act
        var result = await function.RunAsync(request.Object);

        // Assert
        string body = ReadStream(result.Body);
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.IsTrue(body.Contains("35.TwoOfFamilyNameDateOfBirthGenderMustMatchRule"));
    }

    [TestMethod]
    [DataRow("Smith", "Smith", "Male", "Male", "15/01/1970", "15/01/1970")]
    [DataRow("Smith", "Jones", "Male", "Male", "15/01/1970", "15/01/1970")]
    [DataRow("Smith", "Smith", "Male", "Female", "15/01/1970", "15/01/1970")]
    [DataRow("Smith", "Smith", "Male", "Male", "15/01/1970", "15/01/1971")]
    public async Task Run_Should_Not_Return_TwoOfFamilyNameDateOfBirthGenderMustMatchRule_Violation_When_Rule_Validation_Suceeds(
        string existingSurname, string newSurname, string existingGender, string newGender, string existingDob, string newDob)
    {
        // Arrange
        participants[0].Surname = existingSurname;
        participants[0].Gender = existingGender;
        participants[0].DateOfBirth = existingDob;
        participants[1].Surname = newSurname;
        participants[1].Gender = newGender;
        participants[1].DateOfBirth = newDob;
        var json = JsonSerializer.Serialize(participants);
        SetupRequest(json);

        // Act
        var result = await function.RunAsync(request.Object);

        // Assert
        string body = ReadStream(result.Body);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        Assert.IsFalse(body.Contains("35.TwoOfFamilyNameDateOfBirthGenderMustMatchRule"));
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
