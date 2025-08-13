namespace NHS.CohortManager.Tests.TransformDataServiceTests;

using System.Threading.Tasks;
using System.Xml.Linq;
using Castle.Core.Logging;
using DataServices.Client;
using FastExpressionCompiler;
using Hl7.Fhir.Utility;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using Moq;
using NHS.CohortManager.CohortDistributionService;



[TestClass]
public class TransformDataLookupFacadeTests
{
    private readonly TransformDataLookupFacade _sut;
    private Mock<IDataServiceClient<BsSelectOutCode>> _outcodeClientMock = new();
    private readonly Mock<IDataServiceClient<LanguageCode>> _languageCodeClientMock = new();
    private Mock<IDataServiceClient<BsSelectGpPractice>> _gpPracticeClientMock = new();

    private Mock<IDataServiceClient<ExcludedSMULookup>> _excludedSMUClient = new();

    private Mock<ILogger<TransformDataLookupFacade>> _logger = new();

    private Mock<IOptions<TransformDataServiceConfig>> _config = new();

    private Mock<IMemoryCache> _memoryCache = new();

    public TransformDataLookupFacadeTests()
    {
        var testConfig = new TransformDataServiceConfig
        {
            CacheTimeOutHours = 24
        };

        _config.Setup(c => c.Value).Returns(testConfig);
        _outcodeClientMock
            .Setup(x => x.GetSingle(It.IsAny<string>()))
            .ReturnsAsync(new BsSelectOutCode());

        _languageCodeClientMock
            .Setup(x => x.GetSingle(It.IsAny<string>()))
            .ReturnsAsync(new LanguageCode());

        object dummy = new HashSet<string>()
        {
            "A91151"
        };

        _memoryCache.Setup(m => m.TryGetValue(It.IsAny<object>(), out dummy)).Returns(true);
        _sut = new(_outcodeClientMock.Object, _gpPracticeClientMock.Object, _languageCodeClientMock.Object, _excludedSMUClient.Object, _logger.Object, _memoryCache.Object, _config.Object);
    }

    [TestMethod]
    [DataRow("ec1a1bb")]
    [DataRow("EC1A1BB")]
    [DataRow("ec1a 1bb")]
    [DataRow("EC1A 1BB")]
    [DataRow("W1A 0AX")]
    [DataRow("M1 1AE")]
    [DataRow("B33 8TH")]
    [DataRow("CR2 6XH")]
    [DataRow("LS10 1LT")]
    public void ValidateOutcode_ValidPostcode_ReturnTrue(string postcode)
    {
        // Act
        bool result = _sut.ValidateOutcode(postcode);

        // Assert
        Assert.IsTrue(result);
    }


    [TestMethod]
    public void ValidateOutcode_OutcodeNotFound_ReturnFalse()
    {
        // Arrange
        _outcodeClientMock
            .Setup(x => x.GetSingle(It.IsAny<string>()))
            .ReturnsAsync((BsSelectOutCode)null);

        string postcode = "LS10 1LT";

        // Act
        bool result = _sut.ValidateOutcode(postcode);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ValidateOutcode_OutCodeIsSpecial_ReturnTrue()
    {
        // Arrange
        _outcodeClientMock
            .Setup(x => x.GetSingle(It.IsAny<string>()))
            .ReturnsAsync((BsSelectOutCode)null);

        string postcode = "ZZZSECUR";

        // Act
        bool result = _sut.ValidateOutcode(postcode);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("ABC123")]
    [DataRow("ABC 123")]
    public void ValidateOutcode_InvalidPostcode_ThrowTransformationException(string postcode)
    {
        // Act & Assert
        Assert.ThrowsException<TransformationException>(() => _sut.ValidateOutcode(postcode));
    }

    [TestMethod]
    public async Task Get_ExcludedSMUList_ReturnsNonNullDictionary()
    {

        var excludedSMUList = new List<ExcludedSMULookup>() { new ExcludedSMULookup() { GpPracticeCode = "A91151" } };
        _excludedSMUClient.Setup(x => x.GetAll()).ReturnsAsync(excludedSMUList);


        var excludedSMUDictionary = await _sut.GetCachedExcludedSMUValues();


        Assert.IsNotNull(excludedSMUDictionary);
        Assert.AreEqual(excludedSMUDictionary.GetFirst(), excludedSMUList.FirstOrDefault()!.GpPracticeCode);
    }
}
