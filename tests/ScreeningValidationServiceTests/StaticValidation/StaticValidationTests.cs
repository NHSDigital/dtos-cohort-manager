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
public class StaticValidationTests
{
    private readonly Mock<ILogger<StaticValidation>> loggerMock;
    private readonly ServiceCollection serviceCollection;
    private readonly Mock<FunctionContext> context;
    private readonly Mock<HttpRequestData> request;
    private readonly Participant participant = new();
    private readonly StaticValidation function;
    private readonly Mock<IValidationData> _validationDataService = new();

    public StaticValidationTests()
    {
        loggerMock = new Mock<ILogger<StaticValidation>>();
        context = new Mock<FunctionContext>();
        request = new Mock<HttpRequestData>(context.Object);

        serviceCollection = new ServiceCollection();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        context.SetupProperty(c => c.InstanceServices, serviceProvider);

        function = new StaticValidation(loggerMock.Object, _validationDataService.Object);
    }

    [TestMethod]
    [DataRow("0000000000")]
    [DataRow("9999999999")]
    public async Task Run_Should_Not_Return_Rule_Violation_When_Nhs_Number_Is_Ten_Digits(string nhsNumber)
    {
        // Arrange
        participant.NHSId = nhsNumber;
        var json = JsonSerializer.Serialize(participant);
        SetupRequest(json);

        // Act
        var result = await function.RunAsync(request.Object);

        // Assert
        string body = ReadStream(result.Body);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        Assert.IsTrue(!body.Contains("1.NhsNumberMustBeTenDigits"));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("0")]
    [DataRow("123456789")]      // 9 digits
    [DataRow("12.3456789")]     // 9 digits and 1 non-digit
    [DataRow("12.34567899")]    // 10 digits and 1 non-digit
    [DataRow("10000000000")]    // 11 digits
    public async Task Run_Should_Return_Rule_Violation_When_Nhs_Number_Is_Not_Ten_Digits(string nhsNumber)
    {
        // Arrange
        participant.NHSId = nhsNumber;
        var json = JsonSerializer.Serialize(participant);
        SetupRequest(json);

        // Act
        var result = await function.RunAsync(request.Object);

        // Assert
        string body = ReadStream(result.Body);
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.IsTrue(body.Contains("1.NhsNumberMustBeTenDigits"));
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
