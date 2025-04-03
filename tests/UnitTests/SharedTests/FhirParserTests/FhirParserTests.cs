namespace FhirParserHelperTests;

using Common;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using System.Collections.Specialized;
using Model;

[TestClass]
public class FhirParserHelperTests
{
    private readonly Mock<ILogger<FhirParserHelper>> _logger = new();
    private readonly FhirParserHelper _fhirParserHelper;
    private readonly Mock<HttpRequestData> _request;
    private readonly Mock<FunctionContext> _context = new();

    public FhirParserHelperTests()
    {
        _fhirParserHelper = new FhirParserHelper(_logger.Object);

        _request = new Mock<HttpRequestData>(_context.Object);

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
    public void FhirParser_ValidJson_ReturnsDemographic()
    {
        // Arrange
        string json;
        using (StreamReader r = new StreamReader("../../../mock-fhir-response.json"))
        {
            json = r.ReadToEnd();
        }

        var expected = new Demographic()
        {
            NhsNumber = "9000000009",
            PrimaryCareProvider = "Y12345",
            PrimaryCareProviderEffectiveFromDate = "2020-01-01",
            NamePrefix = "Mrs"
        };

        // Act
        var result = _fhirParserHelper.ParseFhirJson(json);

        // Assert
        Assert.IsInstanceOfType(result, typeof(Demographic));
        Assert.AreEqual(expected.NhsNumber, result.NhsNumber);
        Assert.AreEqual(expected.PrimaryCareProvider, result.PrimaryCareProvider);
        Assert.AreEqual(expected.PrimaryCareProviderEffectiveFromDate, result.PrimaryCareProviderEffectiveFromDate);
        Assert.AreEqual(expected.NamePrefix, result.NamePrefix);
    }

    [TestMethod]
    public void FhirParser_InvalidJson_ThrowsException()
    {
        // Arrange
        var json = string.Empty;

        // Act & Assert
        Assert.ThrowsException<FormatException>(() => _fhirParserHelper.ParseFhirJson(json));
        _logger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to parse FHIR json")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
    }
}
