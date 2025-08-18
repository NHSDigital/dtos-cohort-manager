namespace NHS.CohortManager.Tests.UnitTests.HttpClientFunctionTests;

using Common;
using Model;
using System.Net;
using System.Text.Json;
using Common.Interfaces;
using Moq;
using System.Runtime.CompilerServices;
using Castle.Core.Logging;
using Microsoft.Extensions.Logging;

[TestClass]
public class PdsHttpClientMockTests
{
    private PdsHttpClientMock _mockFunction = null!;

    private readonly Mock<IHttpClientFactory> _mockClientFactory = new();
    private readonly Mock<ILogger<HttpClientFunction>> _mockHttpLogger = new();
    private readonly Mock<ILogger<PdsHttpClientMock>> _mockLogger = new();


    [TestInitialize]
    public void Setup()
    {
        _mockFunction = new PdsHttpClientMock(_mockHttpLogger.Object, _mockClientFactory.Object, _mockLogger.Object);
    }

    [TestMethod]
    public async Task SendGet_WithParameters_ReturnsPdsDemographicJson()
    {
        // Arrange
        var url = "https://sandbox.api.service.nhs.uk/personal-demographics/FHIR/R4/Patient/9000000009";
        // Act
        var result = await _mockFunction.SendPdsGet(url,"token");

        // Assert
        Assert.IsNotNull(result);

        // Verify it's valid JSON and deserializes to PdsDemographic
        var resultString = await result.Content.ReadAsStringAsync();
        var pdsDemographic = JsonSerializer.Deserialize<ParticipantDemographic>(resultString);
        Assert.IsNotNull(pdsDemographic);

        //verify that is some default set
        Assert.AreEqual(pdsDemographic.ParticipantId, 0);
    }

    [TestMethod]
    public async Task GetResponseText_ReturnsContentAsString()
    {
        // Arrange
        var content = "test content";
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(content)
        };

        // Act
        var result = await _mockFunction.GetResponseText(response);

        // Assert
        Assert.AreEqual(content, result);
    }


    [TestMethod]
    public void PdsMockFunction_ImplementsIHttpClientFunction()
    {
        // Act & Assert
        Assert.IsInstanceOfType(_mockFunction, typeof(IHttpClientFunction));
    }
}
