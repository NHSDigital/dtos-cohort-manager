namespace NHS.CohortManager.Tests.UnitTests.CheckDemographicTests;

using Common;
using Moq;
using Model;
using System.Text.Json;
using System.Threading.Tasks;

[TestClass]
public class CheckDemographicTests
{
    private readonly Mock<IHttpClientFunction> _httpClientFunction = new();
    private readonly CheckDemographic _checkDemographic;

    public CheckDemographicTests()
    {
        _checkDemographic = new CheckDemographic(_httpClientFunction.Object);
    }

    [TestMethod]
    public async Task GetDemographicAsync_ValidInput_ReturnDemographic()
    {
        // Arrange
        var uri = "test-uri.com/get";
        var nhsNumber = "1234567890";

        var demographic = new Demographic
        {
            FirstName = "John",
            NhsNumber = nhsNumber
        };

        _httpClientFunction.Setup(x => x.SendGet(It.IsAny<string>()))
            .ReturnsAsync(JsonSerializer.Serialize(demographic));

        // Act
        var result = await _checkDemographic.GetDemographicAsync(nhsNumber, uri);

        // Assert
        Assert.AreEqual(nhsNumber, result.NhsNumber);
        Assert.AreEqual(demographic.FirstName, result.FirstName);
    }

}
